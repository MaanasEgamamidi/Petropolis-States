using System;
using UnityEngine;

namespace Petropolis.Minigames.FlappyBird
{
    /// <summary>
    /// A "beam" = the top + bottom obstacle pair separated by a gap. Scrolls left at a fixed
    /// speed and self-destructs once fully off-screen. The invisible scoring trigger in the gap
    /// raises <see cref="Passed"/> the moment the bird's collider exits it — that's exactly one
    /// score increment per beam, no matter how the bird weaves through.
    /// </summary>
    public class FlappyBirdBeam : MonoBehaviour
    {
        public event Action<FlappyBirdBeam> Passed;

        float scrollSpeed;
        float destroyAtX;
        bool alreadyScored;
        bool moving = true;

        public void Configure(float scrollSpeed, float destroyAtX)
        {
            this.scrollSpeed = scrollSpeed;
            this.destroyAtX = destroyAtX;
        }

        public void SetMoving(bool value) => moving = value;

        void Update()
        {
            if (!moving) return;
            transform.position += Vector3.left * (scrollSpeed * Time.deltaTime);
            if (transform.position.x < destroyAtX)
            {
                Destroy(gameObject);
            }
        }

        public void NotifyBirdPassed()
        {
            if (alreadyScored) return;
            alreadyScored = true;
            Passed?.Invoke(this);
        }
    }

    /// <summary>
    /// Attached to the invisible trigger in the middle of a beam gap. When the bird's collider
    /// leaves the trigger it means the bird successfully cleared the beam — at that point we
    /// tell the parent <see cref="FlappyBirdBeam"/> to raise its "passed" event.
    /// </summary>
    public class FlappyBirdBeamScoreTrigger : MonoBehaviour
    {
        FlappyBirdBeam owner;

        void Awake()
        {
            owner = GetComponentInParent<FlappyBirdBeam>();
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (owner == null) return;
            if (other.GetComponentInParent<FlappyBirdBird>() != null)
            {
                owner.NotifyBirdPassed();
            }
        }
    }
}
