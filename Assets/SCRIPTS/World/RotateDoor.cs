using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Door that opens when the player is within range and presses E.
/// Supports left or right pivot; opens outward 90 degrees. Press E again to close.
/// </summary>
[RequireComponent(typeof(Collider))]
[DisallowMultipleComponent]
public class RotateDoor : MonoBehaviour
{
    public enum PivotSide
    {
        Left,
        Right
    }

    [Header("Interaction")]
    [Tooltip("Distance within which the player can interact with the door.")]
    [Min(0.5f)]
    public float interactDistance = 2f;

    [Tooltip("Tag used to identify the player.")]
    public string playerTag = "Player";

    [Header("Door Pivot")]
    [Tooltip("Which side the hinge is on. Left = door swings counter-clockwise; Right = clockwise (viewed from above).")]
    public PivotSide pivotSide = PivotSide.Left;

    [Tooltip("Angle in degrees the door opens outward. Default 90.")]
    [Range(1f, 180f)]
    public float openAngle = 90f;

    [Header("Animation")]
    [Tooltip("How fast the door opens/closes.")]
    public float rotationSpeed = 120f;

    [Tooltip("Optional: Transform to rotate (e.g. door panel). If null, rotates this GameObject.")]
    public Transform doorTransform;

    float _closedAngle;
    float _openAngle;
    float _targetAngle;
    bool _isOpen;
    bool _isAnimating;
    Transform _player;
    Collider _collider;

    void Start()
    {
        _collider = GetComponent<Collider>();

        if (doorTransform == null)
            doorTransform = transform;

        _closedAngle = doorTransform.localEulerAngles.y;

        float sign = pivotSide == PivotSide.Left ? -1f : 1f;
        _openAngle = _closedAngle + sign * openAngle;
        _targetAngle = _closedAngle;

        _player = GameObject.FindGameObjectWithTag(playerTag)?.transform;
        if (_player == null)
            Debug.LogWarning($"[RotateDoor] No GameObject with tag \"{playerTag}\" found. Door will not respond to interaction.");
    }

    void Update()
    {
        if (_player == null) return;

        float dist = InteractionDistanceToPlayer();
        if (dist > interactDistance) return;

#if ENABLE_INPUT_SYSTEM
        bool interactPressed = Keyboard.current != null && Keyboard.current[Key.E].wasPressedThisFrame;
#else
        bool interactPressed = Input.GetKeyDown(KeyCode.E);
#endif

        if (!interactPressed) return;
        if (_isAnimating) return;

        _isOpen = !_isOpen;
        _targetAngle = _isOpen ? _openAngle : _closedAngle;
    }

    void LateUpdate()
    {
        if (!_isAnimating && Mathf.Abs(Mathf.DeltaAngle(doorTransform.localEulerAngles.y, _targetAngle)) > 0.1f)
            _isAnimating = true;

        if (!_isAnimating) return;

        float currentY = doorTransform.localEulerAngles.y;

        float newY = Mathf.MoveTowardsAngle(currentY, _targetAngle, rotationSpeed * Time.deltaTime);
        doorTransform.localRotation = Quaternion.Euler(doorTransform.localEulerAngles.x, newY, doorTransform.localEulerAngles.z);

        if (Mathf.Abs(Mathf.DeltaAngle(newY, _targetAngle)) < 0.1f)
            _isAnimating = false;
    }

    float InteractionDistanceToPlayer()
    {
        if (_collider == null)
            return Vector3.Distance(transform.position, _player.position);
        return Vector3.Distance(_collider.ClosestPoint(_player.position), _player.position);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0f, 1f, 0.5f, 0.4f);
        Gizmos.DrawWireSphere(transform.position, interactDistance);
    }
}
