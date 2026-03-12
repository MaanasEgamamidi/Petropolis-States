using UnityEngine;
using UnityEditor;

public class AttachHUD : EditorWindow
{
    [MenuItem("Tools/Attach HUD Manager")]
    public static void Attach()
    {
        GameObject canvasObj = GameObject.Find("UI");
        if (canvasObj != null)
        {
            var hud = canvasObj.GetComponent("WorldHUDManager");
            if (hud == null)
            {
                // We use type finding to avoid missing reference if asmdefs get weird
                System.Type hudType = System.Type.GetType("WorldHUDManager, Assembly-CSharp");
                if (hudType != null)
                {
                    canvasObj.AddComponent(hudType);
                    UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
                    Debug.Log("HUD Manager Attached");
                }
                else
                {
                    Debug.LogError("Could not find type WorldHUDManager");
                }
            }
        }
    }
}
