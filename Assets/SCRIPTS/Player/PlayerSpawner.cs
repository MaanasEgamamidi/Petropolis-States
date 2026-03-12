using UnityEngine;

public class PlayerSpawner : MonoBehaviour
{
    [Header("Character Prefabs")]
    public GameObject malePrefab;
    public GameObject femalePrefab;

    [Header("Spawn")]
    public Transform spawnPoint;
    public Vector3 spawnPosition = new Vector3(0, 0, 0);

    [Header("Player Armature (Movement & Animator)")]
    [Tooltip("The PlayerArmature transform where character content is instantiated. Character meshes and rig will be parented here.")]
    public Transform playerArmature;

    [Header("Character Scale")]
    [Tooltip("Scale of the character mesh only (does not affect camera or movement). Use 2 for a larger character.")]
    public float characterVisualScale = 2.75f;

    [Header("Animation Setup")]
    [Tooltip("Starter Assets Third Person animator controller. Assign from Assets/StarterAssets/ThirdPersonController/Character/Animations/StarterAssetsThirdPerson.controller")]
    public RuntimeAnimatorController starterAssetsController;
    [Tooltip("Male avatar from Male_all.fbx. Expand Male_all in Project and drag the Avatar sub-asset.")]
    public Avatar maleAvatar;
    [Tooltip("Female avatar from Female_All.fbx. Expand Female_All in Project and drag the Avatar sub-asset.")]
    public Avatar femaleAvatar;

    [Header("Legacy (optional)")]
    [Tooltip("No longer used for spawning. Kept for compatibility.")]
    public Transform playerCharacterRoot;

    [Header("Existing Player (optional)")]
    [Tooltip("Disable this when spawning custom character (e.g. default PlayerArmature).")]
    public Transform defaultPlayerToDisable;

    const string DefaultArmatureMeshName = "Geometry";

    void Start()
    {
        string gender = GlobalData.StoredGender;
        int body = GlobalData.StoredBody;
        int head = GlobalData.StoredHead;

        if (string.IsNullOrEmpty(gender))
            return;

        GameObject prefab = (gender == "female") ? femalePrefab : malePrefab;
        if (prefab == null)
        {
            Debug.LogWarning("[PlayerSpawner] Prefab not assigned.");
            return;
        }

        if (playerArmature == null)
        {
            Debug.LogWarning("[PlayerSpawner] Player Armature not assigned. Assign it in the Inspector.");
            return;
        }

        // Create a scaled container for the character visual only (camera and movement stay unaffected)
        Transform container = CreateCharacterVisualContainer();

        Vector3 pos = spawnPoint != null ? spawnPoint.position : spawnPosition;
        GameObject instance = Instantiate(prefab, pos, Quaternion.identity);

        // Reparent all prefab children to the character visual container and destroy the prefab root
        while (instance.transform.childCount > 0)
        {
            Transform child = instance.transform.GetChild(0);
            child.SetParent(container, true);
        }
        Destroy(instance);

        // Collect and destroy unused body/head meshes so only the chosen pair remains
        System.Collections.Generic.List<GameObject> toDestroy = new System.Collections.Generic.List<GameObject>();
        foreach (Transform child in container.GetComponentsInChildren<Transform>(true))
        {
            string name = child.gameObject.name;
            if ((name.StartsWith("Body_") || name.StartsWith("Body-")) && name.Length > 5
                && int.TryParse(name.Substring(5), out int bodyNum))
            {
                if (bodyNum == body)
                    child.gameObject.SetActive(true);
                else
                    toDestroy.Add(child.gameObject);
            }
            else if (name.StartsWith("Head_") && name.Length > 5
                && int.TryParse(name.Substring(5), out int headNum))
            {
                if (headNum == head)
                    child.gameObject.SetActive(true);
                else
                    toDestroy.Add(child.gameObject);
            }
        }
        foreach (GameObject go in toDestroy)
            Destroy(go);

        // Hide the default Starter Assets Armature mesh (capsule man)
        HideDefaultArmatureMesh(playerArmature);

        // Swap Animator to use Starter Assets controller and correct avatar
        SetupCharacterAnimator(gender);
    }

    Transform CreateCharacterVisualContainer()
    {
        Transform existing = playerArmature.Find("CharacterVisual");
        if (existing != null)
            return existing;

        GameObject container = new GameObject("CharacterVisual");
        container.transform.SetParent(playerArmature, false);
        container.transform.localPosition = Vector3.zero;
        container.transform.localRotation = Quaternion.identity;
        container.transform.localScale = Vector3.one * characterVisualScale;
        return container.transform;
    }

    void HideDefaultArmatureMesh(Transform armatureRoot)
    {
        Transform geometry = armatureRoot.Find(DefaultArmatureMeshName);
        if (geometry != null)
            geometry.gameObject.SetActive(false);
    }

    void SetupCharacterAnimator(string gender)
    {
        Animator anim = playerArmature.GetComponent<Animator>();
        if (anim == null)
        {
            Debug.LogWarning("[PlayerSpawner] No Animator on PlayerArmature.");
            return;
        }

        if (starterAssetsController != null)
            anim.runtimeAnimatorController = starterAssetsController;
        else
            Debug.LogWarning("[PlayerSpawner] Starter Assets Controller not assigned. Character will not animate correctly.");

        Avatar avatar = (gender == "female") ? femaleAvatar : maleAvatar;
        if (avatar != null)
            anim.avatar = avatar;
        else
            Debug.LogWarning($"[PlayerSpawner] {(gender == "female" ? "Female" : "Male")} Avatar not assigned. Character may not animate correctly.");
    }
}
