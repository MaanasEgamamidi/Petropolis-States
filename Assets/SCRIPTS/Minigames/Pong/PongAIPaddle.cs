using UnityEngine;

namespace Petropolis.Minigames.Pong
{
    /// <summary>
    /// Dumb-but-passable opponent paddle: tracks the ball's Y position with a capped speed and
    /// a small deadzone so it doesn't jitter. Kept intentionally beatable.
    /// </summary>
    public class PongAIPaddle : MonoBehaviour
    {
        [SerializeField] Transform ball;
        [SerializeField] float speed = 6f;
        [SerializeField] float minY = -4f;
        [SerializeField] float maxY = 4f;
        [SerializeField] float deadzone = 0.15f;

        public void SetBall(Transform t) => ball = t;

        void Awake()
        {
            if (ball == null)
            {
                var b = FindFirstObjectByType<PongBall>();
                if (b != null) ball = b.transform;
            }
        }

        void Update()
        {
            if (ball == null) return;

            float diff = ball.position.y - transform.position.y;
            if (Mathf.Abs(diff) < deadzone) return;

            float step = Mathf.Sign(diff) * speed * Time.deltaTime;
            if (Mathf.Abs(step) > Mathf.Abs(diff)) step = diff;

            Vector3 pos = transform.position;
            pos.y = Mathf.Clamp(pos.y + step, minY, maxY);
            transform.position = pos;
        }
    }
}
