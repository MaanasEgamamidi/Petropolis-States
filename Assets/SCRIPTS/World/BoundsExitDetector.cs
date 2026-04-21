using UnityEngine;

/// <summary>
/// Attach to the "Bounds" GameObject in the World scene. The Bounds collider is a trigger volume
/// surrounding the playable area; when the player exits it, the player has gone out of bounds and
/// the "Off the Leash" badge is awarded via <see cref="BadgeManager"/>.
///
/// Detection runs two ways so the badge still fires if the trigger callbacks miss the crossing
/// (e.g. the player was spawned outside the volume, teleported out, or the CharacterController
/// exits on a frame where the collision system already culled it):
///   1. <see cref="OnTriggerExit"/> — fires the instant the player's collider leaves the trigger.
///   2. Polling in <see cref="Update"/> — every <see cref="pollInterval"/> seconds we check whether
///      the player's transform is still inside the collider's world-space bounds.
/// </summary>
[RequireComponent(typeof(Collider))]
public class BoundsExitDetector : MonoBehaviour
{
    [Tooltip("Tag used to identify the player.")]
    [SerializeField] string playerTag = "Player";

    [Tooltip("How often (seconds) to poll the player position as a fallback for the trigger event.")]
    [SerializeField, Min(0.05f)] float pollInterval = 0.25f;

    [Tooltip("Log to the Console when the player goes out of bounds (useful while testing).")]
    [SerializeField] bool verboseLogging = false;

    Collider _col;
    Transform _player;
    float _nextPollTime;
    bool _firedThisSession;

    void Awake()
    {
        _col = GetComponent<Collider>();
        if (_col != null && !_col.isTrigger)
            Debug.LogWarning("[BoundsExitDetector] Collider is not set as a Trigger. Enable 'Is Trigger' on the collider for the Bounds GameObject.");

        // CharacterController-only triggers require the trigger side to carry a (kinematic)
        // Rigidbody, matching the pattern used by ParkZone/HomeZone.
        if (GetComponent<Rigidbody>() == null)
        {
            var rb = gameObject.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity  = false;
        }

        if (gameObject.isStatic)
            gameObject.isStatic = false;
    }

    bool IsPlayer(Collider other)
    {
        if (other.CompareTag(playerTag)) return true;
        var root = other.transform.root;
        return root != other.transform && root.CompareTag(playerTag);
    }

    void OnTriggerExit(Collider other)
    {
        if (!IsPlayer(other)) return;
        Award("OnTriggerExit");
    }

    void Update()
    {
        if (_firedThisSession) return;
        if (Time.unscaledTime < _nextPollTime) return;
        _nextPollTime = Time.unscaledTime + pollInterval;

        if (_col == null || !_col.enabled) return;

        Transform p = GetPlayerTransform();
        if (p == null) return;

        // Collider.bounds is axis-aligned world-space AABB — fine for the Bounds box which is
        // authored without rotation. Use Contains so "outside on any axis" fires the badge.
        if (!_col.bounds.Contains(p.position))
            Award("PositionPoll");
    }

    Transform GetPlayerTransform()
    {
        if (_player != null) return _player;

        var go = GameObject.FindGameObjectWithTag(playerTag);
        if (go != null)
            _player = go.transform;

        return _player;
    }

    void Award(string source)
    {
        if (_firedThisSession) return;
        _firedThisSession = true;

        if (verboseLogging)
            Debug.Log($"[BoundsExitDetector] Player left bounds ({source}) — awarding Off the Leash.");

        BadgeManager.MarkOutOfBounds();
    }
}
