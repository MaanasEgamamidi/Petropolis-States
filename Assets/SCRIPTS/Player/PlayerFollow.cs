using UnityEngine;

/// <summary>
/// Attach to the Dog GameObject. Hides dog until player returns from PetChoose with a chosen skin,
/// applies skin from GlobalData.StoredPetSkin, follows the player, and syncs animations (idle/walk/sprint).
/// </summary>
[RequireComponent(typeof(Animator))]
public class PlayerFollow : MonoBehaviour
{
    const string DogMeshName = "Dog_Mesh";

    [Header("Visibility (hidden until pet chosen in PetChoose)")]
    [Tooltip("The visual mesh to hide/show. Auto-finds 'Dog_Mesh' child if not assigned.")]
    [SerializeField] GameObject dogMeshVisual;

    [Header("Follow")]
    [Tooltip("The player to follow (assign in Inspector or auto-finds by Player tag).")]
    [SerializeField] Transform followTarget;

    [Tooltip("Offset from the player (e.g. behind and to the side). X=right, Z=behind.")]
    [SerializeField] Vector3 followOffset = new Vector3(0.8f, 0f, -1.2f);

    [Tooltip("How fast the dog moves toward the target. Should be slightly above player SprintSpeed to keep up.")]
    [SerializeField] float followSpeed = 8.5f;

    [Header("Ground")]
    [Tooltip("Layers to raycast for ground (roads, sidewalks, etc.). Should match player's GroundLayers.")]
    [SerializeField] LayerMask groundLayers = 1;

    [Tooltip("How far down to raycast for ground. Increase if you have tall steps or terrain.")]
    [SerializeField] float groundRaycastDistance = 5f;

    [Tooltip("Height offset from detected ground (e.g. dog's feet pivot). Adjust if dog sinks or floats.")]
    [SerializeField] float groundHeightOffset = 0f;

    [Header("Skin (from GlobalData.StoredPetSkin)")]
    [Tooltip("Dog materials in order: Dog_01, Dog_02, Dog_03, Dog_04, Dog_05, Dog_06.")]
    [SerializeField] Material[] dogMaterials = new Material[6];

    [Tooltip("The dog's SkinnedMeshRenderer (usually on Dog_Mesh child). Auto-finds if not assigned.")]
    [SerializeField] SkinnedMeshRenderer dogMeshRenderer;

    const string SpeedParam = "Speed";

    Animator _animator;
    CharacterController _playerController;
    StarterAssets.StarterAssetsInputs _playerInputs;
    float _playerMoveSpeed = 3.5f;
    float _playerSprintSpeed = 8f;

    [Tooltip("If farther than this from the follow slot (horizontal), snap beside the player (e.g. after PetChoose).")]
    [SerializeField] float snapIfBeyondHorizontal = 12f;

    [Tooltip("While idle, still walk toward the slot at least this planar speed if outside catchUpDistance.")]
    [SerializeField] float idleCatchUpSpeed = 3.5f;

    [Tooltip("Planar distance under which idle catch-up stops (meters).")]
    [SerializeField] float catchUpDistance = 2.5f;

    void Awake()
    {
        _animator = GetComponent<Animator>();

        if (dogMeshVisual == null)
        {
            var t = transform.Find(DogMeshName);
            if (t != null)
                dogMeshVisual = t.gameObject;
            else
                dogMeshVisual = GetComponentInChildren<SkinnedMeshRenderer>()?.gameObject;
        }

        if (dogMeshRenderer == null)
        {
            var sr = GetComponentInChildren<SkinnedMeshRenderer>();
            if (sr != null)
                dogMeshRenderer = sr;
        }

        ResolvePlayer();
    }

    void Start()
    {
        RefreshVisibilityAndSkin();
    }

    void OnEnable()
    {
        // When World unfreezes after PetChoose unloads, re-check visibility and skin
        RefreshVisibilityAndSkin();
    }

    void RefreshVisibilityAndSkin()
    {
        int skinIndex = GlobalData.StoredPetSkin;

        if (skinIndex <= 0)
        {
            if (dogMeshVisual != null)
                dogMeshVisual.SetActive(false);
            return;
        }

        if (dogMeshVisual != null)
            dogMeshVisual.SetActive(true);
        ApplySkinFromGlobalData();
        SnapBesidePlayerIfNeeded(force: true);
    }

    void Update()
    {
        if (followTarget == null)
        {
            ResolvePlayer();
            if (followTarget == null) return;
        }

        if (GlobalData.StoredPetSkin <= 0) return;

        SnapBesidePlayerIfNeeded(force: false);

        float playerSpeed = GetPlayerHorizontalSpeed();
        bool playerSprinting = _playerInputs != null && _playerInputs.sprint;
        float targetSpeed = ComputeDogSpeed(playerSpeed, playerSprinting);

        if (_animator != null)
            _animator.SetFloat(SpeedParam, targetSpeed);

        Vector3 targetPos = GetFollowSlotWorldPosition();
        float moveSpeed = targetSpeed > 0.1f ? followSpeed * Mathf.Clamp01(targetSpeed / _playerSprintSpeed) : 0f;

        Vector3 planarToSlot = targetPos - transform.position;
        planarToSlot.y = 0f;
        float planarDist = planarToSlot.magnitude;
        bool needIdleCatchUp = moveSpeed <= 0.01f && planarDist > catchUpDistance;

        if (moveSpeed > 0.01f || needIdleCatchUp)
        {
            float effectiveSpeed = moveSpeed > 0.01f ? moveSpeed : idleCatchUpSpeed;
            Vector3 toTarget = targetPos - transform.position;
            toTarget.y = 0f;

            if (toTarget.sqrMagnitude > 0.0001f)
            {
                float step = effectiveSpeed * Time.deltaTime;
                Vector3 newPos = Vector3.MoveTowards(transform.position, targetPos, step);
                newPos.y = GetGroundHeightAt(newPos.x, newPos.z) + groundHeightOffset;
                transform.position = newPos;
                transform.rotation = Quaternion.Slerp(transform.rotation,
                    Quaternion.LookRotation(toTarget.normalized),
                    Time.deltaTime * 10f);
            }
        }
        else
        {
            float groundY = GetGroundHeightAt(transform.position.x, transform.position.z) + groundHeightOffset;
            if (Mathf.Abs(transform.position.y - groundY) > 0.01f)
            {
                Vector3 pos = transform.position;
                pos.y = Mathf.Lerp(pos.y, groundY, Time.deltaTime * 5f);
                transform.position = pos;
            }
        }
    }

    void ResolvePlayer()
    {
        if (followTarget != null && _playerController != null) return;
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            var root = player.transform.root;
            _playerController = root.GetComponentInChildren<CharacterController>();
            _playerInputs = root.GetComponentInChildren<StarterAssets.StarterAssetsInputs>();
            if (_playerController != null)
                followTarget = _playerController.transform;
            else if (followTarget == null)
                followTarget = root;

            // Read player speeds from ThirdPersonController so pet keeps up
            var tpc = root.GetComponentInChildren<ThirdPersonController>();
            if (tpc != null)
            {
                _playerMoveSpeed = tpc.MoveSpeed;
                _playerSprintSpeed = tpc.SprintSpeed;
            }
            else
            {
                var tpcSA = root.GetComponentInChildren<StarterAssets.ThirdPersonController>();
                if (tpcSA != null)
                {
                    _playerMoveSpeed = tpcSA.MoveSpeed;
                    _playerSprintSpeed = tpcSA.SprintSpeed;
                }
            }
        }
    }

    Vector3 GetFollowSlotWorldPosition()
    {
        Vector3 slot = followTarget.position + followTarget.TransformDirection(followOffset);
        slot.y = GetGroundHeightAt(slot.x, slot.z) + groundHeightOffset;
        return slot;
    }

    void SnapBesidePlayerIfNeeded(bool force)
    {
        if (followTarget == null) return;
        Vector3 slot = followTarget.position + followTarget.TransformDirection(followOffset);
        Vector3 planar = transform.position - slot;
        planar.y = 0f;
        if (!force && planar.sqrMagnitude < snapIfBeyondHorizontal * snapIfBeyondHorizontal) return;

        slot.y = GetGroundHeightAt(slot.x, slot.z) + groundHeightOffset;
        transform.position = slot;
        Vector3 look = followTarget.position - transform.position;
        look.y = 0f;
        if (look.sqrMagnitude > 0.0001f)
            transform.rotation = Quaternion.LookRotation(look.normalized);
    }

    float GetPlayerHorizontalSpeed()
    {
        if (_playerController == null) return 0f;
        var v = _playerController.velocity;
        float velSpeed = new Vector3(v.x, 0f, v.z).magnitude;
        if (velSpeed > 0.05f) return velSpeed;
        if (_playerInputs != null && _playerInputs.move.sqrMagnitude > 0.01f)
            return _playerInputs.sprint ? _playerSprintSpeed : _playerMoveSpeed;
        return 0f;
    }

    float ComputeDogSpeed(float playerSpeed, bool playerSprinting)
    {
        if (playerSpeed < 0.1f) return 0f;
        return playerSprinting ? _playerSprintSpeed : Mathf.Min(playerSpeed, _playerMoveSpeed);
    }

    /// <summary>
    /// Raycasts downward at (x, z) to find ground. Uses the player's foot height as the ray origin
    /// so we detect the same floor the player is on instead of snapping to a roof or stray collider
    /// when the pet was left at a wrong world position.
    /// </summary>
    float GetGroundHeightAt(float x, float z)
    {
        float probeY = GetPlayerFeetReferenceY() + 0.25f;
        Vector3 origin = new Vector3(x, probeY, z);
        float maxDist = Mathf.Max(groundRaycastDistance, 16f);
        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, maxDist, groundLayers, QueryTriggerInteraction.Ignore))
            return hit.point.y;
        return GetPlayerFeetReferenceY();
    }

    float GetPlayerFeetReferenceY()
    {
        if (_playerController != null)
            return _playerController.transform.position.y + _playerController.center.y - _playerController.height * 0.5f;
        return followTarget != null ? followTarget.position.y : transform.position.y;
    }

    void ApplySkinFromGlobalData()
    {
        int skinIndex = GlobalData.StoredPetSkin;
        if (skinIndex <= 0 || dogMeshRenderer == null || dogMaterials == null) return;

        int i = Mathf.Clamp(skinIndex, 1, 6) - 1;
        if (i >= 0 && i < dogMaterials.Length && dogMaterials[i] != null)
        {
            var mats = dogMeshRenderer.materials;
            if (mats.Length > 0)
            {
                mats[0] = dogMaterials[i];
                dogMeshRenderer.materials = mats;
            }
        }
    }
}
