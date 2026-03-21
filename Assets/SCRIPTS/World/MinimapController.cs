using UnityEngine;

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

    void LateUpdate()
    {
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
