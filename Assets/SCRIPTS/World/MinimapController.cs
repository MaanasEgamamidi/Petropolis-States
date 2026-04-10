using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Keeps an orthographic minimap camera above the player, looking straight down (north-up).
/// Assign the camera's Target Texture to a RenderTexture and show it on a UI RawImage.
/// </summary>
[RequireComponent(typeof(Camera))]
public class MinimapController : MonoBehaviour
{
    [SerializeField] float heightAbovePlayer = 50f;
    [SerializeField] float orthographicSize = 25f;
    [SerializeField] string playerTag = "Player";

    [Tooltip("Optional; if null, finds GameObject with playerTag.")]
    [SerializeField] Transform followTarget;

    Camera _cam;

    void Awake()
    {
        _cam = GetComponent<Camera>();
        _cam.orthographic = true;
        _cam.orthographicSize = orthographicSize;
    }

    void Update()
    {
        if (!Application.isPlaying || MinimapSettings.Mode == MinimapMode.None)
            return;

#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null && Keyboard.current[Key.M].wasPressedThisFrame)
#elif ENABLE_LEGACY_INPUT_MANAGER
        if (Input.GetKeyDown(KeyCode.M))
#else
        if (false)
#endif
            MinimapSettings.RuntimeSuppressed = !MinimapSettings.RuntimeSuppressed;
    }

    void LateUpdate()
    {
        if (Application.isPlaying)
        {
            if (MinimapSettings.Mode == MinimapMode.None)
            {
                _cam.enabled = false;
                return;
            }

            // Keep rendering while fading out after M or while NPC chat hides the minimap (PresentationAlpha still > 0).
            bool minimapRequested = !MinimapSettings.RuntimeSuppressed && !NpcProximityChatOpener.IsWorldNpcChatOpen;
            bool needCamera = minimapRequested || MinimapSettings.PresentationAlpha > 0.001f;
            if (!needCamera)
            {
                _cam.enabled = false;
                return;
            }
        }

        _cam.enabled = true;

        if (followTarget == null)
        {
            var go = GameObject.FindGameObjectWithTag(playerTag);
            if (go == null)
                return;
            followTarget = go.transform;
        }

        Vector3 p = followTarget.position;
        transform.position = p + Vector3.up * heightAbovePlayer;
        transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        _cam.orthographicSize = orthographicSize;
    }
}
