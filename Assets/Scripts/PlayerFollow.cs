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

    [Tooltip("How fast the dog moves toward the target. Should roughly match player SprintSpeed (~5.35) to keep up.")]
    [SerializeField] float followSpeed = 5.5f;

    [Header("Skin (from GlobalData.StoredPetSkin)")]
    [Tooltip("Dog materials in order: Dog_01, Dog_02, Dog_03, Dog_04, Dog_05, Dog_06.")]
    [SerializeField] Material[] dogMaterials = new Material[6];

    [Tooltip("The dog's SkinnedMeshRenderer (usually on Dog_Mesh child). Auto-finds if not assigned.")]
    [SerializeField] SkinnedMeshRenderer dogMeshRenderer;

    const string SpeedParam = "Speed";

    Animator _animator;
    CharacterController _playerController;
    StarterAssets.StarterAssetsInputs _playerInputs;

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
    }

    void Update()
    {
        if (followTarget == null)
        {
            ResolvePlayer();
            if (followTarget == null) return;
        }

        float playerSpeed = GetPlayerHorizontalSpeed();
        bool playerSprinting = _playerInputs != null && _playerInputs.sprint;
        float targetSpeed = ComputeDogSpeed(playerSpeed, playerSprinting);

        if (_animator != null)
            _animator.SetFloat(SpeedParam, targetSpeed);

        Vector3 targetPos = followTarget.position + followTarget.TransformDirection(followOffset);
        targetPos.y = transform.position.y;
        float moveSpeed = targetSpeed > 0.1f ? followSpeed * Mathf.Clamp01(targetSpeed / 5.335f) : 0f;

        if (moveSpeed > 0.01f)
        {
            Vector3 toTarget = targetPos - transform.position;
            toTarget.y = 0f;

            if (toTarget.sqrMagnitude > 0.01f)
            {
                float step = moveSpeed * Time.deltaTime;
                transform.position = Vector3.MoveTowards(transform.position, targetPos, step);
                transform.rotation = Quaternion.Slerp(transform.rotation,
                    Quaternion.LookRotation(toTarget.normalized),
                    Time.deltaTime * 10f);
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
        }
    }

    float GetPlayerHorizontalSpeed()
    {
        if (_playerController == null) return 0f;
        var v = _playerController.velocity;
        float velSpeed = new Vector3(v.x, 0f, v.z).magnitude;
        if (velSpeed > 0.05f) return velSpeed;
        if (_playerInputs != null && _playerInputs.move.sqrMagnitude > 0.01f)
            return _playerInputs.sprint ? 5.335f : 2f;
        return 0f;
    }

    float ComputeDogSpeed(float playerSpeed, bool playerSprinting)
    {
        if (playerSpeed < 0.1f) return 0f;
        return playerSprinting ? 5.335f : Mathf.Min(playerSpeed, 2f);
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
