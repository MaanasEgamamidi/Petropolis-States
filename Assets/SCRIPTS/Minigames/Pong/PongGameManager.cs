using System.Collections;
using TMPro;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Petropolis.Minigames.Pong
{
    /// <summary>
    /// Wires the ball's score/miss events to a TMP text display. The "score" is the number of
    /// times the player has successfully hit the ball back — every hit is worth
    /// <see cref="dollarsPerHit"/> and accumulates into <see cref="MinigameSession.PendingEarnings"/>.
    /// A single miss ends the session: it banks what the player earned, unloads this scene, and
    /// returns them to the World where they entered.
    /// </summary>
    public class PongGameManager : MonoBehaviour
    {
        [SerializeField] PongBall ball;
        [SerializeField] TMP_Text scoreText;
        [SerializeField] TMP_Text bestText;
        [SerializeField] TMP_Text statusText;

        [Header("Economy")]
        [Tooltip("Dollars credited to the player per successful paddle hit.")]
        [SerializeField] float dollarsPerHit = 10f;

        [Tooltip("Seconds the GAME OVER screen stays up before returning to the World.")]
        [SerializeField] float gameOverHoldSeconds = 2.25f;

        int currentHits;
        int bestHits;
        bool ended;

        void Awake()
        {
            if (ball == null) ball = FindFirstObjectByType<PongBall>();
            if (scoreText == null || bestText == null || statusText == null) AutoBindLabels();
            RefreshText(string.Empty);
        }

        void AutoBindLabels()
        {
            TMP_Text[] labels = FindObjectsByType<TMP_Text>(FindObjectsSortMode.None);
            foreach (TMP_Text label in labels)
            {
                if (scoreText == null && label.name == "ScoreText") scoreText = label;
                else if (bestText == null && label.name == "BestText") bestText = label;
                else if (statusText == null && label.name == "StatusText") statusText = label;
            }
        }

        void OnEnable()
        {
            if (ball == null) return;
            ball.PlayerHit += OnPlayerHit;
            ball.PlayerMissed += OnPlayerMissed;
        }

        void OnDisable()
        {
            if (ball == null) return;
            ball.PlayerHit -= OnPlayerHit;
            ball.PlayerMissed -= OnPlayerMissed;
        }

        void Update()
        {
            if (!ended && QuitPressedThisFrame())
                EndAndReturn("You left early.");
        }

        void OnPlayerHit()
        {
            if (ended) return;
            currentHits++;
            if (currentHits > bestHits) bestHits = currentHits;
            MinigameSession.AddPayout(dollarsPerHit);
            RefreshText(string.Empty);
        }

        void OnPlayerMissed()
        {
            if (ended) return;
            // Pong's "loss" = first miss. Bank the earnings and return to the World.
            EndAndReturn("GAME OVER");
        }

        void EndAndReturn(string status)
        {
            ended = true;
            RefreshText(status);
            StartCoroutine(DelayedReturn());
        }

        IEnumerator DelayedReturn()
        {
            yield return new WaitForSecondsRealtime(gameOverHoldSeconds);
            MinigameSession.EndSession();
        }

        void RefreshText(string status)
        {
            float earned = MinigameSession.IsActive ? MinigameSession.PendingEarnings : currentHits * dollarsPerHit;
            if (scoreText != null) scoreText.text = $"HITS: {currentHits}  (${earned:0})";
            if (bestText != null) bestText.text = $"BEST: {bestHits}";
            if (statusText != null) statusText.text = status;
        }

        static bool QuitPressedThisFrame()
        {
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) return true;
            if (Gamepad.current != null && (Gamepad.current.startButton.wasPressedThisFrame || Gamepad.current.selectButton.wasPressedThisFrame)) return true;
            return false;
#else
            return Input.GetKeyDown(KeyCode.Escape);
#endif
        }
    }
}
