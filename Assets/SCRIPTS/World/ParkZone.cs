using System.Collections;
using UnityEngine;

/// <summary>
/// Attach to the "Park Zone" trigger collider in the World scene.
/// - While inside (with a pet): applies continuous stat drift each frame (happiness up, energy/hunger down); <see cref="GlobalData.PetHealth"/> is derived and recomputed.
/// - While inside: logs "Inside of park with [pet name]" (pet) or "Inside of park without a pet" every <see cref="logInterval"/>.
/// - On exit: logs "Left park".
/// </summary>
[RequireComponent(typeof(Collider))]
public class ParkZone : MonoBehaviour
{
    [Tooltip("Tag used to identify the player.")]
    public string playerTag = "Player";

    [Tooltip("How often (in seconds) to print the 'Inside of park' message.")]
    public float logInterval = 1f;

    [Header("Pet stats while in park (per real-time second, only if player has a pet)")]
    [Tooltip("PetHappiness change per second (positive = gain).")]
    [SerializeField] float happinessChangePerSecond = 4f;
    [Tooltip("PetEnergy lost per second (positive value; subtracted from energy).")]
    [SerializeField] [Min(0f)] float energyDrainPerSecond = 2.5f;
    [Tooltip("PetHunger lost per second (positive value; subtracted from hunger / fullness).")]
    [SerializeField] [Min(0f)] float hungerDrainPerSecond = 2.5f;

    bool _playerInZone;
    bool _hasEnteredOnce;
    Coroutine _logCoroutine;

    float _lastGameHourForParkTime = -1f;

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

    void Update()
    {
        if (!_playerInZone)
        {
            _lastGameHourForParkTime = -1f;
            return;
        }

        AccumulateParkGameHours();

        if (GlobalData.StoredPetSkin <= 0)
            return;

        if (NpcProximityChatOpener.IsWorldNpcChatOpen)
            return;

        float dt = Time.deltaTime;
        GlobalData.PetHappiness = Mathf.Clamp(GlobalData.PetHappiness + happinessChangePerSecond * dt, 0f, 100f);
        GlobalData.PetEnergy    = Mathf.Clamp(GlobalData.PetEnergy    - energyDrainPerSecond     * dt, 0f, 100f);
        GlobalData.PetHunger    = Mathf.Clamp(GlobalData.PetHunger    - hungerDrainPerSecond    * dt, 0f, 100f);
        // Health is derived from energy + hunger; never write PetHealth here or it fight PetNeedsSimulator.
        GlobalData.RecomputeHealth();
    }

    void AccumulateParkGameHours()
    {
        float current = GlobalData.CurrentTimeOfDay;
        if (_lastGameHourForParkTime < 0f)
        {
            _lastGameHourForParkTime = current;
            return;
        }

        float diff = current - _lastGameHourForParkTime;
        if (diff < 0f) diff += 24f; // wrapped past midnight
        if (diff > 0f && diff < 12f)
            BadgeManager.AddParkGameHours(diff);

        _lastGameHourForParkTime = current;
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
        _lastGameHourForParkTime = -1f;

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
