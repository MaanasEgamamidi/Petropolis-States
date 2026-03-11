using UnityEngine;
using UnityEditor;
using System.Linq;

public class SetupDoorsEditor : EditorWindow
{
    [MenuItem("Tools/Setup Doors")]
    public static void SetupDoors()
    {
        string[] searchFolders = new[] { "Assets/JC_LP_MegaCity/Prefabs" };
        string[] guids = AssetDatabase.FindAssets("t:Prefab", searchFolders);

        int updatedCount = 0;

        try
        {
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                
                EditorUtility.DisplayProgressBar("Updating Doors", $"Processing {path}", (float)i / guids.Length);

                GameObject prefabContents = PrefabUtility.LoadPrefabContents(path);
                if (prefabContents == null) continue;
                
                bool modified = false;

                Transform[] allTransforms = prefabContents.GetComponentsInChildren<Transform>(true);
                foreach (Transform t in allTransforms)
                {
                    if (t.name.ToLower().Contains("door"))
                    {
                        // Add collider first since RotateDoor requires one
                        Collider col = t.GetComponent<Collider>();
                        if (col == null)
                        {
                            t.gameObject.AddComponent<BoxCollider>();
                            modified = true;
                        }

                        // Add script if missing
                        RotateDoor doorScript = t.GetComponent<RotateDoor>();
                        if (doorScript == null)
                        {
                            doorScript = t.gameObject.AddComponent<RotateDoor>();
                            modified = true;
                        }

                        // Determine pivot based on mesh center
                        MeshFilter mf = t.GetComponent<MeshFilter>();
                        if (mf != null && mf.sharedMesh != null)
                        {
                            float centerX = mf.sharedMesh.bounds.center.x;
                            RotateDoor.PivotSide expectedPivot = centerX > 0f ? RotateDoor.PivotSide.Left : RotateDoor.PivotSide.Right;
                            
                            if (doorScript != null && doorScript.pivotSide != expectedPivot)
                            {
                                doorScript.pivotSide = expectedPivot;
                                modified = true;
                            }
                            
                        }
                    }
                }

                if (modified)
                {
                    PrefabUtility.SaveAsPrefabAsset(prefabContents, path);
                    updatedCount++;
                }
                
                PrefabUtility.UnloadPrefabContents(prefabContents);
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }

        Debug.Log($"[SetupDoors] Processed {guids.Length} prefabs, updated {updatedCount} prefabs with door scripts.");
    }
}