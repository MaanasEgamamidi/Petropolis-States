using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Elevator panel that slides left or right when the player is in range and presses Interact (E).
/// Press again to return. Similar flow to RotateDoor, but translates along local X instead of rotating.
/// </summary>
[RequireComponent(typeof(Collider))]
[DisallowMultipleComponent]
public class ElevatorSlide : MonoBehaviour
{
    public enum ElevatorSide
    {
        Left,
        Right
    }

    [Header("Interaction")]
    [Tooltip("Distance within which the player can interact.")]
    [Min(0.5f)]
    public float interactDistance = 2f;

    [Tooltip("Tag used to identify the player.")]
    public string playerTag = "Player";

    [Header("Slide")]
    [Tooltip("Left half slides along local -X; Right half slides along local +X (face the elevator so local X matches your scene).")]
    public ElevatorSide elevatorSide = ElevatorSide.Left;

    [Tooltip("How far the panel moves when open, in local space.")]
    [Min(0.01f)]
    public float slideDistance = 1f;

    [Tooltip("Slide speed in units per second.")]
    [Min(0.01f)]
    public float slideSpeed = 2f;

    [Tooltip("Optional: Transform to move (e.g. door mesh). If null, moves this GameObject.")]
    public Transform slideTransform;

    Vector3 _closedLocalPos;
    Vector3 _openLocalPos;
    Vector3 _targetLocalPos;
    bool _isOpen;
    bool _isAnimating;
    Transform _player;
    Collider _collider;
    bool _started;

    void Start()
    {
        _collider = GetComponent<Collider>();

        if (slideTransform == null)
            slideTransform = transform;

        RebuildSlideEndpoints();
        _targetLocalPos = _closedLocalPos;
        _started = true;

        _player = GameObject.FindGameObjectWithTag(playerTag)?.transform;
        if (_player == null)
            Debug.LogWarning($"[ElevatorSlide] No GameObject with tag \"{playerTag}\" found. Elevator will not respond to interaction.");
    }

    void OnValidate()
    {
        if (!_started || slideTransform == null) return;
        Vector3 dir = elevatorSide == ElevatorSide.Left ? -Vector3.right : Vector3.right;
        _openLocalPos = _closedLocalPos + dir * slideDistance;
    }

    void RebuildSlideEndpoints()
    {
        _closedLocalPos = slideTransform.localPosition;
        Vector3 dir = elevatorSide == ElevatorSide.Left ? -Vector3.right : Vector3.right;
        _openLocalPos = _closedLocalPos + dir * slideDistance;
    }

    void Update()
    {
        if (_player == null) return;

        float dist = InteractionDistanceToPlayer();
        if (dist > interactDistance) return;

#if ENABLE_INPUT_SYSTEM
        bool interactPressed = (Keyboard.current != null && Keyboard.current[Key.E].wasPressedThisFrame)
                               || (Gamepad.current != null && Gamepad.current.buttonWest.wasPressedThisFrame);
#else
        bool interactPressed = Input.GetKeyDown(KeyCode.E);
#endif

        if (!interactPressed) return;
        if (_isAnimating) return;

        _isOpen = !_isOpen;
        _targetLocalPos = _isOpen ? _openLocalPos : _closedLocalPos;
    }

    void LateUpdate()
    {
        if (slideTransform == null) return;

        if (!_isAnimating && Vector3.Distance(slideTransform.localPosition, _targetLocalPos) > 0.001f)
            _isAnimating = true;

        if (!_isAnimating) return;

        Vector3 current = slideTransform.localPosition;
        Vector3 newPos = Vector3.MoveTowards(current, _targetLocalPos, slideSpeed * Time.deltaTime);
        slideTransform.localPosition = newPos;

        if (Vector3.Distance(newPos, _targetLocalPos) < 0.001f)
            _isAnimating = false;
    }

    float InteractionDistanceToPlayer()
    {
        if (_collider == null)
            return Vector3.Distance(transform.position, _player.position);
        return ColliderInteractionDistance.DistanceToPoint(_collider, _player.position);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0f, 0.55f, 1f, 0.35f);
        Gizmos.DrawWireSphere(transform.position, interactDistance);

        Transform t = slideTransform != null ? slideTransform : transform;
        Vector3 worldDir = t.TransformDirection(elevatorSide == ElevatorSide.Left ? -Vector3.right : Vector3.right);
        Gizmos.color = new Color(0f, 0.85f, 1f, 0.9f);
        Gizmos.DrawRay(t.position, worldDir * Mathf.Min(slideDistance, 3f));
    }
}
