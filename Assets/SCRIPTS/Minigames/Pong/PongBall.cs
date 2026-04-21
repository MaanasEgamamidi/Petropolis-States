using System;
using UnityEngine;

namespace Petropolis.Minigames.Pong
{
    /// <summary>
    /// Ball physics + scoring events. Uses a dynamic <see cref="Rigidbody2D"/> with zero gravity —
    /// paddle hits produce a position-based reflection (classic Pong), and contact with any other
    /// collider is treated as a wall bounce: we look at the contact normal, force the velocity to
    /// head away from the wall with a minimum angle + a small jitter, and re-clamp the speed so
    /// rallies never settle into a flat skim along the top or bottom.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Collider2D))]
    public class PongBall : MonoBehaviour
    {
        [Header("Speed")]
        [SerializeField] float initialSpeed = 6f;
        [SerializeField] float minSpeed = 5f;
        [SerializeField] float maxSpeed = 14f;
        [SerializeField] float speedIncrement = 0.25f;

        [Header("Anti-Skim (angle clamps, in degrees from horizontal)")]
        [Tooltip("Minimum angle between the ball's direction and the horizontal axis after any bounce. Prevents the ball from skimming along the top/bottom wall.")]
        [SerializeField, Range(1f, 45f)] float minAngleFromHorizontal = 18f;
        [Tooltip("Maximum angle between the ball's direction and the horizontal axis. Keeps the ball progressing toward the paddles.")]
        [SerializeField, Range(45f, 89f)] float maxAngleFromHorizontal = 75f;
        [Tooltip("Random extra deflection (± degrees) applied to each wall bounce so the ball never settles into a repeating pattern.")]
        [SerializeField, Range(0f, 20f)] float wallBounceJitterDegrees = 8f;
        [Tooltip("Cooldown (seconds) between consecutive wall-bounce corrections from the same surface, so a single contact doesn't trigger a re-bounce every physics step.")]
        [SerializeField] float wallBounceCooldown = 0.08f;

        [Header("Bounds")]
        [Tooltip("X position past which the ball counts as 'player missed' (right side).")]
        [SerializeField] float playerMissX = 9.5f;
        [Tooltip("X position past which the ball counts as 'AI missed'.")]
        [SerializeField] float aiMissX = -9.5f;

        [Header("Tags")]
        [SerializeField] string playerPaddleTag = "PlayerPaddle";
        [SerializeField] string aiPaddleTag = "AIPaddle";

        public event Action PlayerHit;
        public event Action PlayerMissed;
        public event Action AIMissed;

        Rigidbody2D rb;
        float currentSpeed;
        float lastWallBounceTime = -999f;

        void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.linearDamping = 0f;
            rb.angularDamping = 0f;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            rb.freezeRotation = true;
        }

        void OnEnable() => ResetAndLaunch();

        void Update()
        {
            if (transform.position.x > playerMissX)
            {
                PlayerMissed?.Invoke();
                ResetAndLaunch(launchTowardPlayer: true);
            }
            else if (transform.position.x < aiMissX)
            {
                AIMissed?.Invoke();
                ResetAndLaunch(launchTowardPlayer: false);
            }
        }

        public void ResetAndLaunch(bool launchTowardPlayer = true)
        {
            transform.position = Vector3.zero;
            currentSpeed = Mathf.Max(initialSpeed, minSpeed);
            lastWallBounceTime = -999f;

            float angle = UnityEngine.Random.Range(-30f, 30f) * Mathf.Deg2Rad;
            float dirX = launchTowardPlayer ? 1f : -1f;
            Vector2 dir = new Vector2(Mathf.Cos(angle) * dirX, Mathf.Sin(angle)).normalized;
            rb.linearVelocity = ClampDirection(dir) * currentSpeed;
        }

        void OnCollisionEnter2D(Collision2D collision)
        {
            if (collision.collider.CompareTag(playerPaddleTag))
            {
                ReflectFromPaddle(collision.transform, movingRight: false);
                PlayerHit?.Invoke();
            }
            else if (collision.collider.CompareTag(aiPaddleTag))
            {
                ReflectFromPaddle(collision.transform, movingRight: true);
            }
            else
            {
                HandleWallBounce(collision);
            }
        }

        void OnCollisionStay2D(Collision2D collision)
        {
            // Sustained contact with a wall = we're sliding. Force the ball off so it can't skim.
            if (!collision.collider.CompareTag(playerPaddleTag) &&
                !collision.collider.CompareTag(aiPaddleTag))
            {
                HandleWallBounce(collision);
            }
        }

        void ReflectFromPaddle(Transform paddle, bool movingRight)
        {
            float paddleHeight = 2f;
            var col = paddle.GetComponent<Collider2D>();
            if (col != null) paddleHeight = col.bounds.size.y;

            float offset = Mathf.Clamp((transform.position.y - paddle.position.y) / (paddleHeight * 0.5f), -1f, 1f);
            float bounceAngle = offset * 55f * Mathf.Deg2Rad;
            bounceAngle += UnityEngine.Random.Range(-5f, 5f) * Mathf.Deg2Rad;

            currentSpeed = Mathf.Clamp(currentSpeed + speedIncrement, minSpeed, maxSpeed);
            float x = Mathf.Cos(bounceAngle) * (movingRight ? 1f : -1f);
            float y = Mathf.Sin(bounceAngle);
            rb.linearVelocity = ClampDirection(new Vector2(x, y).normalized) * currentSpeed;

            lastWallBounceTime = -999f;
        }

        void HandleWallBounce(Collision2D collision)
        {
            if (Time.time - lastWallBounceTime < wallBounceCooldown) return;

            Vector2 normal = collision.GetContact(0).normal;
            Vector2 v = rb.linearVelocity;
            float speed = Mathf.Max(v.magnitude, minSpeed);

            // Horizontal wall (top/bottom): normal points mostly up/down.
            // Force vertical velocity to head AWAY from the surface.
            if (Mathf.Abs(normal.y) > Mathf.Abs(normal.x))
            {
                float ySign = Mathf.Sign(normal.y == 0f ? 1f : normal.y);
                v.y = Mathf.Abs(v.y) * ySign;
                // If vy was tiny (skim case), boost it to the minimum angle.
                float minY = speed * Mathf.Sin(minAngleFromHorizontal * Mathf.Deg2Rad);
                if (Mathf.Abs(v.y) < minY) v.y = minY * ySign;
                // Preserve X sign but don't let it be 0.
                if (Mathf.Abs(v.x) < 0.01f) v.x = (UnityEngine.Random.value < 0.5f ? -1f : 1f) * speed * 0.5f;
            }
            else
            {
                // Side wall fallback (shouldn't normally happen — those are the miss zones).
                float xSign = Mathf.Sign(normal.x == 0f ? 1f : normal.x);
                v.x = Mathf.Abs(v.x) * xSign;
            }

            // Jitter so patterns don't repeat.
            float jitter = UnityEngine.Random.Range(-wallBounceJitterDegrees, wallBounceJitterDegrees) * Mathf.Deg2Rad;
            float cos = Mathf.Cos(jitter);
            float sin = Mathf.Sin(jitter);
            Vector2 jittered = new Vector2(v.x * cos - v.y * sin, v.x * sin + v.y * cos);

            Vector2 dir = jittered.normalized;
            rb.linearVelocity = ClampDirection(dir) * Mathf.Clamp(speed, minSpeed, maxSpeed);
            lastWallBounceTime = Time.time;
        }

        /// <summary>
        /// Rotates <paramref name="dir"/> so its angle from the horizontal axis lies within
        /// [<see cref="minAngleFromHorizontal"/>, <see cref="maxAngleFromHorizontal"/>]. Preserves
        /// the original X and Y signs (the ball keeps heading the same general direction).
        /// </summary>
        Vector2 ClampDirection(Vector2 dir)
        {
            dir = dir.normalized;
            if (dir.sqrMagnitude < 0.0001f) return new Vector2(1f, 0f);

            float sx = dir.x >= 0f ? 1f : -1f;
            float sy = dir.y >= 0f ? 1f : -1f;
            if (Mathf.Approximately(dir.y, 0f)) sy = UnityEngine.Random.value < 0.5f ? -1f : 1f;
            if (Mathf.Approximately(dir.x, 0f)) sx = UnityEngine.Random.value < 0.5f ? -1f : 1f;

            float angle = Mathf.Atan2(Mathf.Abs(dir.y), Mathf.Abs(dir.x)) * Mathf.Rad2Deg;
            angle = Mathf.Clamp(angle, minAngleFromHorizontal, maxAngleFromHorizontal);

            float rad = angle * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(rad) * sx, Mathf.Sin(rad) * sy);
        }
    }
}
