using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;

public static class SetupNPCAnimators
{
    [MenuItem("Tools/Setup NPC Idle Animators")]
    public static void Setup()
    {
        CreateController(
            "Assets/Animations/NPCIdleMale.controller",
            "Assets/PolyArt/Characters/Male/FBX/Male_animations.fbx",
            "Arm_Char|_Idle_1",
            "Arm_Char|Idle_3"
        );

        CreateController(
            "Assets/Animations/NPCIdleFemale.controller",
            "Assets/PolyArt/Characters/Female/FBX/Female_Animations.fbx",
            "Female Idle_1",
            "Female Idle_3"
        );

        AssignControllersInScene();

        AssetDatabase.SaveAssets();
        Debug.Log("NPC Idle Animators created and assigned successfully.");
    }

    private static void CreateController(string path, string fbxPath, string idle1Name, string idle3Name)
    {
        if (!AssetDatabase.IsValidFolder("Assets/Animations"))
            AssetDatabase.CreateFolder("Assets", "Animations");

        AnimationClip idle1 = FindClip(fbxPath, idle1Name);
        AnimationClip idle3 = FindClip(fbxPath, idle3Name);

        if (idle1 == null || idle3 == null)
        {
            Debug.LogError($"Could not find clips '{idle1Name}' / '{idle3Name}' in {fbxPath}");
            return;
        }

        var controller = AnimatorController.CreateAnimatorControllerAtPath(path);
        controller.AddParameter("PlayerNearby", AnimatorControllerParameterType.Bool);

        var rootStateMachine = controller.layers[0].stateMachine;

        // Remove the default empty state that Unity auto-creates
        if (rootStateMachine.states.Length > 0)
        {
            foreach (var s in rootStateMachine.states)
                rootStateMachine.RemoveState(s.state);
        }

        var stateIdle1 = rootStateMachine.AddState("Idle1", new Vector3(350, 100, 0));
        stateIdle1.motion = idle1;

        var stateIdle3 = rootStateMachine.AddState("Idle3", new Vector3(350, 250, 0));
        stateIdle3.motion = idle3;

        rootStateMachine.defaultState = stateIdle1;

        // Idle1 -> Idle3 when PlayerNearby == true
        var toIdle3 = stateIdle1.AddTransition(stateIdle3);
        toIdle3.AddCondition(AnimatorConditionMode.If, 0, "PlayerNearby");
        toIdle3.hasExitTime = false;
        toIdle3.duration = 0.25f;

        // Idle3 -> Idle1 when PlayerNearby == false
        var toIdle1 = stateIdle3.AddTransition(stateIdle1);
        toIdle1.AddCondition(AnimatorConditionMode.IfNot, 0, "PlayerNearby");
        toIdle1.hasExitTime = false;
        toIdle1.duration = 0.25f;

        EditorUtility.SetDirty(controller);
    }

    private static AnimationClip FindClip(string fbxPath, string clipName)
    {
        var assets = AssetDatabase.LoadAllAssetsAtPath(fbxPath);
        foreach (var asset in assets)
        {
            if (asset is AnimationClip clip && clip.name == clipName)
                return clip;
        }
        return null;
    }

    private static void AssignControllersInScene()
    {
        var maleController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(
            "Assets/Animations/NPCIdleMale.controller");
        var femaleController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(
            "Assets/Animations/NPCIdleFemale.controller");

        if (maleController == null || femaleController == null)
        {
            Debug.LogError("Controllers not found after creation.");
            return;
        }

        var npcsRoot = GameObject.Find("NPCs");
        if (npcsRoot == null)
        {
            Debug.LogWarning("NPCs GameObject not found in scene. Open the World scene and run again.");
            return;
        }

        foreach (var animator in npcsRoot.GetComponentsInChildren<Animator>(true))
        {
            string goName = animator.gameObject.name;
            if (goName.StartsWith("Male"))
            {
                animator.runtimeAnimatorController = maleController;
                Debug.Log($"Assigned NPCIdleMale to {animator.gameObject.name}");
            }
            else if (goName.StartsWith("Female"))
            {
                animator.runtimeAnimatorController = femaleController;
                Debug.Log($"Assigned NPCIdleFemale to {animator.gameObject.name}");
            }
        }

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
    }
}
