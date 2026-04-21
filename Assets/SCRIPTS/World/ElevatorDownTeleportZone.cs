using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Trigger volume: while the player is inside, pressing R (keyboard) or Y / north face button (gamepad) moves them
/// downward in world space by <see cref="DescendMeters"/> from their current position.
/// Disables <see cref="CharacterController"/> briefly so the move applies cleanly.
/// Uses a physics overlap each frame so "in zone" matches actual overlap (CharacterController often does not fire OnTriggerExit reliably).
/// </summary>
[RequireComponent(typeof(Collider))]
public class ElevatorDownTeleportZone : MonoBehaviour
{
    const float DescendMeters = 5f;

    [Tooltip("Tag used to identify the player.")]
    public string playerTag = "Player";

    const int OverlapBufferSize = 16;

    Collider _zoneCollider;
    readonly Collider[] _overlapBuffer = new Collider[OverlapBufferSize];

    bool _playerInZone;
    Transform _playerRoot;

    void Awake()
    {
        _zoneCollider = GetComponent<Collider>();

        if (gameObject.isStatic)
            gameObject.isStatic = false;

        if (_zoneCollider != null && !_zoneCollider.isTrigger)
            Debug.LogWarning($"[{nameof(ElevatorDownTeleportZone)}] {name}: enable Is Trigger on the collider.");

        if (GetComponent<Rigidbody>() == null)
        {
            var rb = gameObject.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;
        }
    }

    bool IsPlayer(Collider other)
    {
        if (other == null || other == _zoneCollider) return false;
        if (other.CompareTag(playerTag)) return true;
        var root = other.transform.root;
        return root != other.transform && root.CompareTag(playerTag);
    }

    /// <summary>
    /// Recomputes whether the player overlaps this zone's volume (authoritative; avoids stuck state when OnTriggerExit does not fire for CharacterController).
    /// </summary>
    void RefreshPlayerInZoneFromOverlap()
    {
        _playerInZone = false;
        _playerRoot = null;

        if (_zoneCollider == null)
            return;

        int count;
        if (_zoneCollider is BoxCollider box)
        {
            Vector3 center = box.transform.TransformPoint(box.center);
            Quaternion rotation = box.transform.rotation;
            Vector3 lossy = box.transform.lossyScale;
            var halfExtents = Vector3.Scale(box.size * 0.5f, new Vector3(Mathf.Abs(lossy.x), Mathf.Abs(lossy.y), Mathf.Abs(lossy.z)));
            count = Physics.OverlapBoxNonAlloc(center, halfExtents, _overlapBuffer, rotation, ~0, QueryTriggerInteraction.Collide);
        }
        else
        {
            Bounds b = _zoneCollider.bounds;
            count = Physics.OverlapBoxNonAlloc(b.center, b.extents, _overlapBuffer, Quaternion.identity, ~0, QueryTriggerInteraction.Collide);
        }

        for (int i = 0; i < count; i++)
        {
            var c = _overlapBuffer[i];
            if (!IsPlayer(c))
                continue;
            _playerInZone = true;
            _playerRoot = c.transform.root;
            return;
        }
    }

    void Update()
    {
        RefreshPlayerInZoneFromOverlap();

        if (!_playerInZone || _playerRoot == null)
            return;
        if (WorldPauseController.IsPaused)
            return;

#if ENABLE_INPUT_SYSTEM
        bool keyboardR = Keyboard.current != null && Keyboard.current[Key.R].wasPressedThisFrame;
        bool gamepadY = Gamepad.current != null && Gamepad.current.buttonNorth.wasPressedThisFrame;
        bool pressed = keyboardR || gamepadY;
#else
        bool pressed = Input.GetKeyDown(KeyCode.R) || Input.GetKeyDown(KeyCode.Y);
#endif
        if (!pressed)
            return;

        if (NpcProximityChatOpener.IsWorldNpcChatOpen || WorldPauseController.IsFocusedOnTextInput())
            return;

        ElevatorSlide.CloseAll();

        var cc = _playerRoot.GetComponentInChildren<CharacterController>();
        Vector3 delta = Vector3.down * DescendMeters;
        if (cc != null)
        {
            cc.enabled = false;
            cc.transform.position += delta;
            cc.enabled = true;
        }
        else
        {
            _playerRoot.position += delta;
        }
    }
}
