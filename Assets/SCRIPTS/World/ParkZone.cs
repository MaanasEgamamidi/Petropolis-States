using System.Collections;
using UnityEngine;

/// <summary>
/// Attach to the "Park Zone" trigger collider in the World scene.
/// - Before the player first enters: silent.
/// - While inside: logs "Inside of park with [pet name]" (pet) or "Inside of park without a pet" every second.
/// - On exit: logs "Left park".
/// </summary>
[RequireComponent(typeof(Collider))]
public class ParkZone : MonoBehaviour
{
    [Tooltip("Tag used to identify the player.")]
    public string playerTag = "Player";

    [Tooltip("How often (in seconds) to print the 'Inside of park' message.")]
    public float logInterval = 1f;

    bool _playerInZone;
    bool _hasEnteredOnce;
    Coroutine _logCoroutine;

    void Awake()
    {
        // Triggers need a non-static object and a Rigidbody to detect CharacterController.
        if (gameObject.isStatic)
            gameObject.isStatic = false;

        if (GetComponent<Rigidbody>() == null)
        {
            var rb = gameObject.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity  = false;
        }

        Collider col = GetComponent<Collider>();
        if (col != null && !col.isTrigger)
            Debug.LogWarning("[ParkZone] Collider is not set as a Trigger. Enable 'Is Trigger' on the collider.");
    }

    bool IsPlayer(Collider other)
    {
        if (other.CompareTag(playerTag)) return true;
        var root = other.transform.root;
        return root != other.transform && root.CompareTag(playerTag);
    }

    void OnTriggerEnter(Collider other)
    {
        if (!IsPlayer(other)) return;

        _playerInZone   = true;
        _hasEnteredOnce = true;

        if (_logCoroutine != null)
            StopCoroutine(_logCoroutine);
        _logCoroutine = StartCoroutine(LogInsidePark());
    }

    void OnTriggerExit(Collider other)
    {
        if (!IsPlayer(other)) return;

        _playerInZone = false;

        if (_logCoroutine != null)
        {
            StopCoroutine(_logCoroutine);
            _logCoroutine = null;
        }

        // Only log "Left park" after the player has actually entered at least once.
        if (_hasEnteredOnce)
            Debug.Log("Left park");
    }

    IEnumerator LogInsidePark()
    {
        while (_playerInZone)
        {
            bool hasPet = GlobalData.StoredPetSkin > 0;
            if (hasPet)
                Debug.Log($"Inside of park with {GlobalData.PetName}");
            else
                Debug.Log("Inside of park without a pet");

            yield return new WaitForSeconds(logInterval);
        }
    }
}
