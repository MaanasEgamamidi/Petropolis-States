using TMPro;
using UnityEngine;

namespace Petropolis.Minigames.Pong
{
    /// <summary>
    /// Wires the ball's score/miss events to a TMP text display. The "score" is the number of
    /// times the player has successfully hit the ball back — it resets to 0 on a miss so the
    /// player is always trying to beat their last streak.
    /// </summary>
    public class PongGameManager : MonoBehaviour
    {
        [SerializeField] PongBall ball;
        [SerializeField] TMP_Text scoreText;
        [SerializeField] TMP_Text bestText;

        int currentHits;
        int bestHits;

        void Awake()
        {
            if (ball == null) ball = FindFirstObjectByType<PongBall>();
            if (scoreText == null || bestText == null) AutoBindLabels();
            RefreshText();
        }

        void AutoBindLabels()
        {
            var labels = FindObjectsByType<TMP_Text>(FindObjectsSortMode.None);
            foreach (var label in labels)
            {
                if (scoreText == null && label.name == "ScoreText") scoreText = label;
                else if (bestText == null && label.name == "BestText") bestText = label;
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

        void OnPlayerHit()
        {
            currentHits++;
            if (currentHits > bestHits) bestHits = currentHits;
            RefreshText();
        }

        void OnPlayerMissed()
        {
            currentHits = 0;
            RefreshText();
        }

        void RefreshText()
        {
            if (scoreText != null) scoreText.text = $"HITS: {currentHits}";
            if (bestText != null) bestText.text = $"BEST: {bestHits}";
        }
    }
}
