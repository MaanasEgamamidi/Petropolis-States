using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace Petropolis.Minigames.FlappyBird
{
    /// <summary>
    /// Orchestrates the FlappyBird minigame: ties the bird's death to game-over, counts every
    /// beam cleared, and restarts on space/click. Score is the number of beams the player has
    /// made it past this run; <see cref="bestScore"/> is the best across the session.
    ///
    /// On <see cref="Awake"/> this component also "bootstraps" any missing scene dependencies
    /// (bird, ground/ceiling walls, beam spawner, UI canvas + score texts). That way the scene
    /// only needs this GameManager + a Camera to work — if inspector references are missing we
    /// just create the pieces at runtime and wire them up.
    /// </summary>
    public class FlappyBirdGameManager : MonoBehaviour
    {
        enum State { WaitingToStart, Playing, GameOver }

        [Header("Refs (auto-filled if empty)")]
        [SerializeField] FlappyBirdBird bird;
        [SerializeField] FlappyBirdBeamSpawner spawner;
        [SerializeField] TMP_Text scoreText;
        [SerializeField] TMP_Text bestText;
        [SerializeField] TMP_Text statusText;

        [Header("Config")]
        [SerializeField] Vector3 birdStartPosition = new Vector3(-3f, 0f, 0f);
        [SerializeField] Color groundColor = new Color(0.4f, 0.26f, 0.12f, 1f);
        [SerializeField] Color uiColor = Color.white;

        [Header("Economy")]
        [Tooltip("Dollars credited to the player per beam cleared.")]
        [SerializeField] float dollarsPerBeam = 10f;

        [Tooltip("Seconds the GAME OVER screen stays up before returning to the World (only when launched from a MinigameLauncherZone).")]
        [SerializeField] float gameOverHoldSeconds = 2.25f;

        State state = State.WaitingToStart;
        int score;
        int bestScore;
        bool returningToWorld;

        void Awake()
        {
            BootstrapScene();
        }

        void OnEnable()
        {
            if (bird != null) bird.Died += OnBirdDied;
            if (spawner != null) spawner.BeamPassed += OnBeamPassed;
            EnterWaiting();
        }

        void OnDisable()
        {
            if (bird != null) bird.Died -= OnBirdDied;
            if (spawner != null) spawner.BeamPassed -= OnBeamPassed;
        }

        void Update()
        {
            if (!returningToWorld && QuitPressedThisFrame())
            {
                BeginReturnToWorld("You left early.");
                return;
            }

            if (returningToWorld) return;

            if (state != State.Playing && ConsumeStartInput())
            {
                BeginRun();
            }
        }

        static bool QuitPressedThisFrame()
        {
            Keyboard kb = Keyboard.current;
            if (kb != null && kb.escapeKey.wasPressedThisFrame) return true;
            Gamepad gp = Gamepad.current;
            if (gp != null && (gp.startButton.wasPressedThisFrame || gp.selectButton.wasPressedThisFrame)) return true;
            return false;
        }

        static bool ConsumeStartInput()
        {
            var kb = Keyboard.current;
            if (kb != null && (kb.spaceKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame || kb.upArrowKey.wasPressedThisFrame))
                return true;
            var mouse = Mouse.current;
            if (mouse != null && mouse.leftButton.wasPressedThisFrame)
                return true;
            var gp = Gamepad.current;
            if (gp != null && (gp.buttonSouth.wasPressedThisFrame || gp.startButton.wasPressedThisFrame))
                return true;
            return false;
        }

        void EnterWaiting()
        {
            state = State.WaitingToStart;
            score = 0;
            if (spawner != null) spawner.ClearAll();
            if (bird != null)
            {
                bird.ResetTo(birdStartPosition);
                bird.FreezeForGameOver();
            }
            RefreshUI("TAP / SPACE to start");
        }

        void BeginRun()
        {
            state = State.Playing;
            score = 0;
            if (bird != null)
            {
                bird.ResetTo(birdStartPosition);
                bird.BeginPlay();
            }
            if (spawner != null) spawner.BeginSpawning();
            RefreshUI(string.Empty);
        }

        void OnBeamPassed(FlappyBirdBeam _)
        {
            if (state != State.Playing) return;
            score++;
            if (score > bestScore) bestScore = score;
            MinigameSession.AddPayout(dollarsPerBeam);
            RefreshUI(string.Empty);
        }

        void OnBirdDied()
        {
            if (state == State.GameOver) return;
            state = State.GameOver;
            if (spawner != null) spawner.StopSpawning();
            if (bird != null) bird.FreezeForGameOver();
            if (score > bestScore) bestScore = score;

            // When launched from a MinigameLauncherZone, the "loss" banks earnings and sends the
            // player back to the World. In standalone play-mode there's no session active, so we
            // fall back to the existing retry-on-tap behaviour.
            if (MinigameSession.IsActive)
                BeginReturnToWorld("GAME OVER");
            else
                RefreshUI("GAME OVER - tap to retry");
        }

        void BeginReturnToWorld(string status)
        {
            if (returningToWorld) return;
            returningToWorld = true;
            state = State.GameOver;
            if (spawner != null) spawner.StopSpawning();
            if (bird != null) bird.FreezeForGameOver();
            RefreshUI(status);
            StartCoroutine(ReturnToWorldAfterDelay());
        }

        IEnumerator ReturnToWorldAfterDelay()
        {
            yield return new WaitForSecondsRealtime(gameOverHoldSeconds);
            MinigameSession.EndSession();
        }

        void RefreshUI(string status)
        {
            float earned = MinigameSession.IsActive ? MinigameSession.PendingEarnings : score * dollarsPerBeam;
            if (scoreText != null) scoreText.text = $"SCORE: {score}  (${earned:0})";
            if (bestText != null) bestText.text = $"BEST: {bestScore}";
            if (statusText != null) statusText.text = status;
        }

        void BootstrapScene()
        {
            EnsureCamera();
            if (bird == null) bird = FindFirstObjectByType<FlappyBirdBird>();
            if (bird == null) bird = CreateBird();
            EnsureBirdVisual(bird);

            if (spawner == null) spawner = FindFirstObjectByType<FlappyBirdBeamSpawner>();
            if (spawner == null) spawner = CreateSpawner();

            EnsureGroundAndCeiling();
            EnsureEventSystem();
            EnsureUI();
        }

        static void EnsureCamera()
        {
            var cam = Camera.main;
            if (cam == null) return;
            cam.orthographic = true;
            if (cam.orthographicSize < 0.1f) cam.orthographicSize = 5.5f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            if (cam.backgroundColor.a < 0.01f)
            {
                cam.backgroundColor = new Color(0.46f, 0.78f, 0.95f, 1f);
            }
            if (cam.transform.position.z >= 0f)
            {
                var p = cam.transform.position;
                p.z = -10f;
                cam.transform.position = p;
            }
        }

        FlappyBirdBird CreateBird()
        {
            var go = new GameObject("Bird");
            go.transform.position = birdStartPosition;
            go.transform.localScale = new Vector3(0.7f, 0.7f, 1f);
            go.AddComponent<SpriteRenderer>();
            var rb = go.AddComponent<Rigidbody2D>();
            rb.gravityScale = 2.8f;
            rb.freezeRotation = true;
            var col = go.AddComponent<CircleCollider2D>();
            col.radius = 0.5f;
            return go.AddComponent<FlappyBirdBird>();
        }

        static void EnsureBirdVisual(FlappyBirdBird b)
        {
            if (b == null) return;
            var sr = b.GetComponent<SpriteRenderer>();
            if (sr == null) sr = b.gameObject.AddComponent<SpriteRenderer>();
            if (sr.sprite == null) sr.sprite = FlappyBirdSpriteCache.WhiteSquare;
            sr.color = new Color(1f, 0.85f, 0.15f, 1f);
            sr.sortingOrder = 5;
        }

        FlappyBirdBeamSpawner CreateSpawner()
        {
            var go = new GameObject("BeamSpawner");
            return go.AddComponent<FlappyBirdBeamSpawner>();
        }

        void EnsureGroundAndCeiling()
        {
            EnsureWall("Ground", new Vector3(0f, -5.6f, 0f));
            EnsureWall("Ceiling", new Vector3(0f, 5.6f, 0f));
        }

        void EnsureWall(string name, Vector3 position)
        {
            var go = GameObject.Find(name);
            if (go == null)
            {
                go = new GameObject(name);
                go.transform.position = position;
                go.transform.localScale = new Vector3(30f, 1f, 1f);
            }
            var sr = go.GetComponent<SpriteRenderer>();
            if (sr == null) sr = go.AddComponent<SpriteRenderer>();
            if (sr.sprite == null) sr.sprite = FlappyBirdSpriteCache.WhiteSquare;
            sr.color = groundColor;
            sr.sortingOrder = 2;

            var col = go.GetComponent<BoxCollider2D>();
            if (col == null) col = go.AddComponent<BoxCollider2D>();
            col.size = new Vector2(1f, 1f);
        }

        static void EnsureEventSystem()
        {
            if (FindFirstObjectByType<EventSystem>() != null) return;
            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            go.AddComponent<InputSystemUIInputModule>();
        }

        void EnsureUI()
        {
            Canvas canvas = null;
            foreach (var c in FindObjectsByType<Canvas>(FindObjectsSortMode.None))
            {
                if (c != null && c.isRootCanvas) { canvas = c; break; }
            }
            if (canvas == null)
            {
                var go = new GameObject("Canvas");
                canvas = go.AddComponent<Canvas>();
                go.AddComponent<CanvasScaler>();
                go.AddComponent<GraphicRaycaster>();
            }
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            var scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler != null)
            {
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920f, 1080f);
                scaler.matchWidthOrHeight = 0.5f;
            }

            if (scoreText == null) scoreText = EnsureUIText(canvas, "ScoreText", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(60f, -80f), new Vector2(520f, 80f), 64, TextAlignmentOptions.Left);
            if (bestText == null) bestText = EnsureUIText(canvas, "BestText", new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-60f, -80f), new Vector2(520f, 80f), 48, TextAlignmentOptions.Right);
            if (statusText == null) statusText = EnsureUIText(canvas, "StatusText", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 0f), new Vector2(900f, 200f), 72, TextAlignmentOptions.Center);
        }

        TMP_Text EnsureUIText(Canvas canvas, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPos, Vector2 size, float fontSize, TextAlignmentOptions alignment)
        {
            var existing = canvas.transform.Find(name);
            GameObject go;
            if (existing != null)
            {
                go = existing.gameObject;
            }
            else
            {
                go = new GameObject(name);
                go.transform.SetParent(canvas.transform, worldPositionStays: false);
            }

            var rt = go.GetComponent<RectTransform>();
            if (rt == null) rt = go.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = new Vector2(
                Mathf.Approximately(anchorMin.x, 1f) ? 1f : Mathf.Approximately(anchorMin.x, 0.5f) ? 0.5f : 0f,
                Mathf.Approximately(anchorMin.y, 0f) ? 0f : Mathf.Approximately(anchorMin.y, 0.5f) ? 0.5f : 1f);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;

            var tmp = go.GetComponent<TextMeshProUGUI>();
            if (tmp == null) tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.fontSize = fontSize;
            tmp.color = uiColor;
            tmp.alignment = alignment;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            tmp.raycastTarget = false;
            tmp.fontStyle = FontStyles.Bold;
            return tmp;
        }
    }
}
