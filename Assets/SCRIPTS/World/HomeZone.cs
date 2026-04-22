using UnityEngine;

/// <summary>
/// Attach to the HomeZone trigger collider in the World scene.
/// While the player is inside (and owns a pet), <see cref="GlobalData.PetEnergy"/> recovers over time.
/// </summary>
[RequireComponent(typeof(Collider))]
public class HomeZone : MonoBehaviour
{
    [Tooltip("Tag used to identify the player.")]
    public string playerTag = "Player";

    [Header("Pet stats while at home (per real-time second, only if player has a pet)")]
    [Tooltip("PetEnergy gained per second (positive = rest / recover).")]
    [SerializeField] [Min(0f)] float energyGainPerSecond = 3f;

    bool _playerInZone;

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
        if (col != null)
        {
            if (!col.isTrigger)
                col.isTrigger = true;
        }
        else
            Debug.LogWarning("[HomeZone] No Collider on this GameObject.");
    }

    void Update()
    {
        if (!_playerInZone)
            return;

        if (GlobalData.StoredPetSkin <= 0)
            return;

        float dt = Time.deltaTime;
        GlobalData.PetEnergy = Mathf.Clamp(GlobalData.PetEnergy + energyGainPerSecond * dt, 0f, 100f);
        GlobalData.RecomputeHealth();
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
        _playerInZone = true;
    }

    void OnTriggerExit(Collider other)
    {
        if (!IsPlayer(other)) return;
        _playerInZone = false;
    }
}
