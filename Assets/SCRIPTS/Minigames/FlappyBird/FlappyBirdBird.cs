using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Petropolis.Minigames.FlappyBird
{
    /// <summary>
    /// 2D bird controller. Uses a dynamic <see cref="Rigidbody2D"/> with gravity — space/click
    /// snaps vertical velocity to <see cref="flapVelocity"/> so every flap feels identical, no
    /// matter how fast the bird was falling. Any collision kills the bird and raises <see cref="Died"/>.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Collider2D))]
    public class FlappyBirdBird : MonoBehaviour
    {
        [SerializeField] float flapVelocity = 7f;
        [SerializeField] float gravityScale = 2.8f;
        [SerializeField] float maxFallSpeed = -12f;
        [Tooltip("Max tilt up when rising (degrees).")]
        [SerializeField] float tiltUp = 25f;
        [Tooltip("Max tilt down when falling (degrees).")]
        [SerializeField] float tiltDown = -70f;
        [Tooltip("Y position past which the bird instantly dies (ground / ceiling).")]
        [SerializeField] float lethalMinY = -5.5f;
        [SerializeField] float lethalMaxY = 5.5f;

        public event Action Died;

        Rigidbody2D rb;
        bool alive = true;
        bool inputEnabled;

        public bool IsAlive => alive;

        void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            rb.gravityScale = gravityScale;
            rb.freezeRotation = true;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            var sr = GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                if (sr.sprite == null) sr.sprite = FlappyBirdSpriteCache.WhiteSquare;
                sr.color = new Color(1f, 0.85f, 0.15f, 1f);
                sr.sortingOrder = 5;
            }
        }

        public void BeginPlay()
        {
            alive = true;
            inputEnabled = true;
            rb.linearVelocity = Vector2.zero;
            rb.gravityScale = gravityScale;
            transform.rotation = Quaternion.identity;
        }

        public void FreezeForGameOver()
        {
            alive = false;
            inputEnabled = false;
            rb.linearVelocity = Vector2.zero;
            rb.gravityScale = 0f;
        }

        public void ResetTo(Vector3 position)
        {
            transform.position = position;
            transform.rotation = Quaternion.identity;
            rb.linearVelocity = Vector2.zero;
            rb.gravityScale = gravityScale;
            alive = true;
            inputEnabled = true;
        }

        void Update()
        {
            if (!alive || !inputEnabled) return;

            if (ShouldFlap())
            {
                Vector2 v = rb.linearVelocity;
                v.y = flapVelocity;
                rb.linearVelocity = v;
            }

            if (rb.linearVelocity.y < maxFallSpeed)
            {
                Vector2 v = rb.linearVelocity;
                v.y = maxFallSpeed;
                rb.linearVelocity = v;
            }

            float t = Mathf.InverseLerp(maxFallSpeed, flapVelocity, rb.linearVelocity.y);
            float angle = Mathf.Lerp(tiltDown, tiltUp, t);
            transform.rotation = Quaternion.Euler(0f, 0f, angle);

            float y = transform.position.y;
            if (y < lethalMinY || y > lethalMaxY)
            {
                Die();
            }
        }

        static bool ShouldFlap()
        {
            var kb = Keyboard.current;
            if (kb != null && (kb.spaceKey.wasPressedThisFrame || kb.upArrowKey.wasPressedThisFrame || kb.wKey.wasPressedThisFrame))
                return true;
            var mouse = Mouse.current;
            if (mouse != null && mouse.leftButton.wasPressedThisFrame)
                return true;
            var gp = Gamepad.current;
            if (gp != null && gp.buttonSouth.wasPressedThisFrame)
                return true;
            var touch = Touchscreen.current;
            if (touch != null && touch.primaryTouch.press.wasPressedThisFrame)
                return true;
            return false;
        }

        void OnCollisionEnter2D(Collision2D _) => Die();
        void OnTriggerEnter2D(Collider2D other)
        {
            if (other.CompareTag("FlappyBirdObstacle"))
            {
                Die();
            }
        }

        void Die()
        {
            if (!alive) return;
            alive = false;
            inputEnabled = false;
            Died?.Invoke();
        }
    }
}
