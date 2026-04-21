using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
using UnityEngine.UI;

namespace Petropolis.Minigames
{
    /// <summary>
    /// Three-attempt reaction-time test. Click / Space / Xbox A to advance. When launched from a
    /// <see cref="MinigameLauncherZone"/>, the total payout is scored proportionally to how fast
    /// the player reacts across all attempts (faster reactions = more dollars) and awarded when
    /// they finish the third trial.
    /// </summary>
    public class ReactionTimeGame : MonoBehaviour
    {
        private enum State
        {
            Idle,
            Waiting,
            Ready,
            TooSoon,
            Finished
        }

        [Header("References")]
        [SerializeField] private Image lightImage;
        [SerializeField] private TMP_Text instructionText;
        [SerializeField] private TMP_Text resultText;
        [SerializeField] private TMP_Text attemptText;

        [Header("Colors")]
        [SerializeField] private Color redColor = new Color(0.92f, 0.15f, 0.15f, 1f);
        [SerializeField] private Color greenColor = new Color(0.15f, 0.85f, 0.25f, 1f);
        [SerializeField] private Color badColor = new Color(0.95f, 0.65f, 0.1f, 1f);
        [SerializeField] private Color finishedColor = new Color(0.2f, 0.45f, 0.9f, 1f);

        [Header("Timing (seconds)")]
        [SerializeField] private float minWait = 1.2f;
        [SerializeField] private float maxWait = 3.5f;
        [SerializeField] private int totalAttempts = 3;

        [Header("Economy")]
        [Tooltip("Flat bonus added per completed attempt.")]
        [SerializeField] private float baseDollarsPerAttempt = 20f;

        [Tooltip("Dollars per millisecond under 600ms. Faster reactions stack this up; anything at/above 600ms gives only the base bonus.")]
        [SerializeField] private float dollarsPerMsSaved = 0.25f;

        [Tooltip("Milliseconds cutoff: reactions at or above this earn only the flat base bonus.")]
        [SerializeField] private float slowReactionCutoffMs = 600f;

        [Tooltip("Seconds to hold the final results before banking and returning to the World.")]
        [SerializeField] private float finishedHoldSeconds = 3.5f;

        private State state = State.Idle;
        private float greenShownAt;
        private readonly List<float> reactionTimes = new List<float>();
        private Coroutine waitRoutine;
        private Coroutine returnRoutine;
        private bool payoutAwarded;

        private void Awake()
        {
            AutoResolveReferences();
        }

        private void Start()
        {
            ResetGame();
        }

        private void AutoResolveReferences()
        {
            if (lightImage == null)
            {
                foreach (Image img in FindObjectsByType<Image>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                {
                    if (img.gameObject.name == "Light") { lightImage = img; break; }
                }
            }

            foreach (TMP_Text txt in FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                string n = txt.gameObject.name;
                if (instructionText == null && n == "InstructionText") instructionText = txt;
                else if (resultText == null && n == "ResultText") resultText = txt;
                else if (attemptText == null && n == "AttemptText") attemptText = txt;
            }

            if (lightImage == null)
                Debug.LogWarning("[ReactionTimeGame] Could not find 'Light' Image in scene.");
        }

        private void Update()
        {
            if (QuitPressedThisFrame())
            {
                BeginReturnToWorld();
                return;
            }

            if (!RespondPressedThisFrame()) return;

            switch (state)
            {
                case State.Idle:
                case State.TooSoon:
                    StartNextAttempt();
                    break;
                case State.Finished:
                    // In-session: finishing already triggers return; outside a session, restart.
                    if (!MinigameSession.IsActive) StartNextAttempt();
                    break;
                case State.Waiting:
                    FailTooSoon();
                    break;
                case State.Ready:
                    RegisterReaction();
                    break;
            }
        }

        static bool RespondPressedThisFrame()
        {
#if ENABLE_INPUT_SYSTEM
            Keyboard kb = Keyboard.current;
            if (kb != null && (kb.spaceKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame)) return true;
            Mouse mouse = Mouse.current;
            if (mouse != null && mouse.leftButton.wasPressedThisFrame) return true;
            Gamepad gp = Gamepad.current;
            if (gp != null && (gp.buttonSouth.wasPressedThisFrame || gp.buttonWest.wasPressedThisFrame)) return true;
            return false;
#else
            return Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return);
#endif
        }

        static bool QuitPressedThisFrame()
        {
#if ENABLE_INPUT_SYSTEM
            Keyboard kb = Keyboard.current;
            if (kb != null && kb.escapeKey.wasPressedThisFrame) return true;
            Gamepad gp = Gamepad.current;
            if (gp != null && (gp.startButton.wasPressedThisFrame || gp.selectButton.wasPressedThisFrame)) return true;
            return false;
#else
            return Input.GetKeyDown(KeyCode.Escape);
#endif
        }

        private void ResetGame()
        {
            StopWaitRoutine();
            reactionTimes.Clear();
            state = State.Idle;
            SetLight(redColor);
            if (instructionText != null)
                instructionText.text = "Reaction Time Test\nClick / Space / A to start";
            if (resultText != null) resultText.text = string.Empty;
            UpdateAttemptText();
        }

        private void StartNextAttempt()
        {
            if (reactionTimes.Count >= totalAttempts)
            {
                ResetGame();
                return;
            }

            state = State.Waiting;
            SetLight(redColor);
            if (instructionText != null)
                instructionText.text = "Wait for GREEN...";
            if (resultText != null) resultText.text = string.Empty;
            UpdateAttemptText();

            StopWaitRoutine();
            waitRoutine = StartCoroutine(WaitThenGo());
        }

        private IEnumerator WaitThenGo()
        {
            float delay = Random.Range(minWait, maxWait);
            yield return new WaitForSecondsRealtime(delay);

            state = State.Ready;
            SetLight(greenColor);
            if (instructionText != null) instructionText.text = "CLICK!";
            greenShownAt = Time.unscaledTime;
        }

        private void RegisterReaction()
        {
            float reaction = Time.unscaledTime - greenShownAt;
            reactionTimes.Add(reaction);

            if (reactionTimes.Count >= totalAttempts)
            {
                ShowFinalResults();
            }
            else
            {
                state = State.Idle;
                SetLight(redColor);
                if (instructionText != null)
                    instructionText.text = $"{Mathf.RoundToInt(reaction * 1000f)} ms\nClick to continue";
                if (resultText != null) resultText.text = string.Empty;
                UpdateAttemptText();
            }
        }

        private void FailTooSoon()
        {
            StopWaitRoutine();
            state = State.TooSoon;
            SetLight(badColor);
            if (instructionText != null)
                instructionText.text = "Too soon!\nClick to retry this attempt";
        }

        private void ShowFinalResults()
        {
            state = State.Finished;
            SetLight(finishedColor);

            float total = 0f;
            float best = float.MaxValue;
            foreach (float t in reactionTimes)
            {
                total += t;
                if (t < best) best = t;
            }
            float avg = total / reactionTimes.Count;

            float earned = ComputePayoutForRun();
            AwardPayoutOnce(earned);

            if (instructionText != null)
            {
                instructionText.text = MinigameSession.IsActive
                    ? $"Done! You earned ${earned:0}"
                    : "Done!\nClick to play again";
            }

            if (resultText != null)
            {
                System.Text.StringBuilder lines = new System.Text.StringBuilder();
                for (int i = 0; i < reactionTimes.Count; i++)
                    lines.AppendLine($"Try {i + 1}: {Mathf.RoundToInt(reactionTimes[i] * 1000f)} ms");
                lines.AppendLine();
                lines.AppendLine($"Best: {Mathf.RoundToInt(best * 1000f)} ms");
                lines.Append($"Avg:  {Mathf.RoundToInt(avg * 1000f)} ms");
                resultText.text = lines.ToString();
            }

            UpdateAttemptText();

            if (MinigameSession.IsActive && returnRoutine == null)
                returnRoutine = StartCoroutine(ReturnAfterDelay());
        }

        private float ComputePayoutForRun()
        {
            float total = 0f;
            foreach (float t in reactionTimes)
                total += PayoutForAttempt(t);
            return total;
        }

        /// <summary>
        /// Payout scale for a single attempt. Faster = more. At ≥<see cref="slowReactionCutoffMs"/>
        /// only <see cref="baseDollarsPerAttempt"/> is awarded; every ms saved below that adds
        /// <see cref="dollarsPerMsSaved"/> dollars.
        /// </summary>
        private float PayoutForAttempt(float reactionSeconds)
        {
            float ms = reactionSeconds * 1000f;
            float msSaved = Mathf.Max(0f, slowReactionCutoffMs - ms);
            return baseDollarsPerAttempt + msSaved * dollarsPerMsSaved;
        }

        private void AwardPayoutOnce(float earned)
        {
            if (payoutAwarded) return;
            if (!MinigameSession.IsActive) return;
            MinigameSession.AddPayout(earned);
            payoutAwarded = true;
        }

        private IEnumerator ReturnAfterDelay()
        {
            yield return new WaitForSecondsRealtime(finishedHoldSeconds);
            MinigameSession.EndSession();
        }

        private void BeginReturnToWorld()
        {
            if (!MinigameSession.IsActive) return;
            if (returnRoutine != null) return;
            // Early exit: still award whatever attempts they already completed.
            if (!payoutAwarded)
                AwardPayoutOnce(ComputePayoutForRun());
            returnRoutine = StartCoroutine(ReturnAfterDelay_Immediate());
        }

        private IEnumerator ReturnAfterDelay_Immediate()
        {
            yield return null;
            MinigameSession.EndSession();
        }

        private void UpdateAttemptText()
        {
            if (attemptText == null) return;
            int done = Mathf.Min(reactionTimes.Count, totalAttempts);
            attemptText.text = $"Attempt {Mathf.Min(done + (state == State.Finished ? 0 : 1), totalAttempts)} / {totalAttempts}";
        }

        private void SetLight(Color c)
        {
            if (lightImage != null) lightImage.color = c;
        }

        private void StopWaitRoutine()
        {
            if (waitRoutine != null)
            {
                StopCoroutine(waitRoutine);
                waitRoutine = null;
            }
        }
    }
}
