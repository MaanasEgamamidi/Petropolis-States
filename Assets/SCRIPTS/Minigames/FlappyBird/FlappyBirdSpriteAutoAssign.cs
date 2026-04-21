using UnityEngine;

namespace Petropolis.Minigames.FlappyBird
{
    /// <summary>
    /// Bird / ground / ceiling use plain solid-color quads. Instead of shipping a texture asset
    /// and binding it in the inspector, this component assigns a shared 1x1 white sprite on
    /// Awake so the <see cref="SpriteRenderer"/> actually renders. The sprite's color comes from
    /// the <see cref="SpriteRenderer"/>'s own color field.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class FlappyBirdSpriteAutoAssign : MonoBehaviour
    {
        [SerializeField] Color color = Color.white;
        [SerializeField] int sortingOrder;

        void Awake()
        {
            var sr = GetComponent<SpriteRenderer>();
            if (sr.sprite == null) sr.sprite = FlappyBirdSpriteCache.WhiteSquare;
            sr.color = color;
            sr.sortingOrder = sortingOrder;
        }
    }
}
