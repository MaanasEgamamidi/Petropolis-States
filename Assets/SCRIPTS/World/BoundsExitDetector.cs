using UnityEngine;

/// <summary>
/// Attach to the "Bounds" GameObject in the World scene. The Bounds collider is a trigger volume
/// surrounding the playable area; when the player exits it, the player has gone out of bounds and
/// the "Off the Leash" badge is awarded via <see cref="BadgeManager"/>.
/// </summary>
[RequireComponent(typeof(Collider))]
public class BoundsExitDetector : MonoBehaviour
{
    [Tooltip("Tag used to identify the player.")]
    [SerializeField] string playerTag = "Player";

    void Awake()
    {
        Collider col = GetComponent<Collider>();
        if (col != null && !col.isTrigger)
            Debug.LogWarning("[BoundsExitDetector] Collider is not set as a Trigger. Enable 'Is Trigger' on the collider for the Bounds GameObject.");
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
        BadgeManager.MarkOutOfBounds();
    }
}
