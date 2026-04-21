using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Petropolis.Minigames
{
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

        private State state = State.Idle;
        private float greenShownAt;
        private readonly List<float> reactionTimes = new List<float>();
        private Coroutine waitRoutine;

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
            // If references weren't wired in the inspector, locate UI pieces by name.
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
            // Accept mouse click, space, or any touch as "respond" input.
            bool pressed = Input.GetMouseButtonDown(0)
                           || Input.GetKeyDown(KeyCode.Space)
                           || Input.GetKeyDown(KeyCode.Return);

            if (!pressed) return;

            switch (state)
            {
                case State.Idle:
                case State.TooSoon:
                case State.Finished:
                    StartNextAttempt();
                    break;
                case State.Waiting:
                    // Clicked while still red = too soon.
                    FailTooSoon();
                    break;
                case State.Ready:
                    RegisterReaction();
                    break;
            }
        }

        private void ResetGame()
        {
            StopWaitRoutine();
            reactionTimes.Clear();
            state = State.Idle;
            SetLight(redColor);
            if (instructionText != null)
                instructionText.text = "Reaction Time Test\nClick / Space to start";
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
            yield return new WaitForSeconds(delay);

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

            if (instructionText != null)
                instructionText.text = "Done!\nClick to play again";

            if (resultText != null)
            {
                var lines = new System.Text.StringBuilder();
                for (int i = 0; i < reactionTimes.Count; i++)
                {
                    lines.AppendLine($"Try {i + 1}: {Mathf.RoundToInt(reactionTimes[i] * 1000f)} ms");
                }
                lines.AppendLine();
                lines.AppendLine($"Best: {Mathf.RoundToInt(best * 1000f)} ms");
                lines.Append($"Avg:  {Mathf.RoundToInt(avg * 1000f)} ms");
                resultText.text = lines.ToString();
            }

            UpdateAttemptText();
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
