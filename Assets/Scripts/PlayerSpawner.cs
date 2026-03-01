using UnityEngine;

public class PlayerSpawner : MonoBehaviour
{
    [Header("Character Prefabs")]
    public GameObject malePrefab;
    public GameObject femalePrefab;

    [Header("Spawn")]
    public Transform spawnPoint;
    public Vector3 spawnPosition = new Vector3(0, 0, 0);

    [Header("Character Container")]
    [Tooltip("The child 'Player' transform where character content is instantiated. Create an empty child named 'Player' if not present.")]
    public Transform playerCharacterRoot;

    [Header("Existing Player (optional)")]
    [Tooltip("Disable this when spawning custom character (e.g. default PlayerArmature).")]
    public Transform defaultPlayerToDisable;

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

        // Keep movement object (PlayerArmature) always enabled - do not disable defaultPlayerToDisable
        Transform container = playerCharacterRoot != null ? playerCharacterRoot : transform;

        Vector3 pos = spawnPoint != null ? spawnPoint.position : spawnPosition;
        GameObject instance = Instantiate(prefab, pos, Quaternion.identity);

        // Reparent all prefab children to the child Player (playerCharacterRoot) and destroy the prefab root
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
    }
}
