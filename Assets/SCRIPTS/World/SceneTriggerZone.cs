using UnityEngine;
using UnityEngine.SceneManagement;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Attach to a trigger collider (e.g. PetShopZone under Zones in World). When the player enters the zone and presses the interact key,
/// loads the target scene. Leave <see cref="targetSceneName"/> empty until a shop/interior scene is in Build Settings.
/// World → PetChoose additive loading only runs when <c>targetSceneName</c> is <c>PetChoose</c> (not used for the pet shop zone).
/// </summary>
[RequireComponent(typeof(Collider))]
public class SceneTriggerZone : MonoBehaviour
{
    [Tooltip("Scene to load when player is in zone and presses the interact key (must be in Build Settings).")]
    public string targetSceneName = "Map";

    [Header("Fade Transition (optional)")]
    [Tooltip("Full-screen black image with CanvasGroup for fade-to-black. If set, uses fade before loading; otherwise loads immediately.")]
    public GameObject fadeToBlackPanel;
    [Tooltip("Total fade duration (fade out + load + fade in). Only used when fadeToBlackPanel is assigned.")]
    public float fadeDuration = 0.85f;

    [Tooltip("Key to press while in zone to load the target scene.")]
#if ENABLE_INPUT_SYSTEM
    public Key interactKey = Key.E;
#else
    public KeyCode interactKey = KeyCode.E;
#endif

    [Tooltip("Tag used to identify the player. Default is \"Player\".")]
    public string playerTag = "Player";

    [Tooltip("Enable debug logs in Console to help troubleshoot.")]
    public bool debugLogs = true;

    [Tooltip("If true, this zone can only be used once per play session. After use, it won't trigger again when returning to this scene.")]
    public bool oneTimeUse = true;

    static System.Collections.Generic.HashSet<string> _usedZoneIds = new System.Collections.Generic.HashSet<string>();

    bool _playerInZone;

    string ZoneId => $"{gameObject.scene.name}_{gameObject.name}";

    void Awake()
    {
        // Static objects often don't detect CharacterController - must be non-static
        if (gameObject.isStatic)
        {
            gameObject.isStatic = false;
            if (debugLogs) Debug.Log("[SceneTriggerZone] Cleared Static flag - triggers need non-static for detection.");
        }

        Collider col = GetComponent<Collider>();
        if (col != null && !col.isTrigger)
        {
            Debug.LogWarning($"[SceneTriggerZone] Collider on {gameObject.name} should have Is Trigger enabled for zone detection.");
        }
        // Trigger needs Rigidbody (kinematic) to detect CharacterController
        if (GetComponent<Rigidbody>() == null)
        {
            var rb = gameObject.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;
            if (debugLogs) Debug.Log("[SceneTriggerZone] Added kinematic Rigidbody for trigger detection.");
        }
    }

    bool IsPlayer(Collider other)
    {
        if (other.CompareTag(playerTag)) return true;
        // Also check root (collider might be on child like PlayerArmature)
        var root = other.transform.root;
        return root != other.transform && root.CompareTag(playerTag);
    }

    void OnTriggerEnter(Collider other)
    {
        if (debugLogs) Debug.Log($"[SceneTriggerZone] Something entered: {other.gameObject.name} (tag: {other.tag}, root: {other.transform.root.name})");
        if (IsPlayer(other))
        {
            _playerInZone = true;
            if (debugLogs) Debug.Log("[SceneTriggerZone] Player is now in zone.");
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (IsPlayer(other))
        {
            _playerInZone = false;
            if (debugLogs) Debug.Log("[SceneTriggerZone] Player left zone.");
        }
    }

    void Update()
    {
        if (!_playerInZone)
            return;
        if (WorldPauseController.IsPaused)
            return;

#if ENABLE_INPUT_SYSTEM
        bool keyboardInteract = Keyboard.current != null && Keyboard.current[interactKey].wasPressedThisFrame;
        bool gamepadInteract = Gamepad.current != null && Gamepad.current.buttonWest.wasPressedThisFrame;
        if (!keyboardInteract && !gamepadInteract)
#else
        if (!Input.GetKeyDown(interactKey))
#endif
        {
            return;
        }

        if (NpcProximityChatOpener.IsWorldNpcChatOpen || WorldPauseController.IsFocusedOnTextInput())
        {
            if (debugLogs)
                Debug.Log("[SceneTriggerZone] Ignoring interact — NPC chat is open or a text field is focused.");
            return;
        }

        bool petChooseFromWorld = targetSceneName == "PetChoose" && gameObject.scene.name == "World";
        if (petChooseFromWorld && GlobalData.StoredPetSkin > 0)
        {
            if (debugLogs)
                Debug.Log("[SceneTriggerZone] Pet already chosen; not opening PetChoose again.");
            return;
        }

        if (oneTimeUse && _usedZoneIds.Contains(ZoneId))
        {
            if (debugLogs) Debug.Log("[SceneTriggerZone] Zone already used this session; ignoring.");
            return;
        }

        if (string.IsNullOrEmpty(targetSceneName))
        {
            if (debugLogs)
                Debug.LogWarning($"[SceneTriggerZone] No target scene on '{gameObject.name}'. Assign Target Scene Name when the interior (e.g. pet shop) scene is ready.");
            return;
        }

        if (debugLogs) Debug.Log($"[SceneTriggerZone] Loading scene: {targetSceneName}");

        if (oneTimeUse)
            _usedZoneIds.Add(ZoneId);

        // When leaving World to PetChoose, load additively so World stays in memory
        if (petChooseFromWorld)
        {
            GlobalData.PetChooseOpenedFromWorld = true;
            GlobalData.MarkPetChooseFlowStartedFromWorld();
            AdditiveSceneHelper.FreezeWorldBeforePetChooseLoad(); // Freeze before load to avoid "only one EventSystem" warning
            var panel = fadeToBlackPanel;
            if (panel != null && panel.GetComponent<CanvasGroup>() != null)
            {
                FadeTransitionRunner.RunFadeThenLoadSceneAdditive(panel, fadeDuration, targetSceneName);
            }
            else
            {
                SceneManager.LoadScene(targetSceneName, LoadSceneMode.Additive);
            }
        }
        else
        {
            var panel = fadeToBlackPanel;
            if (panel != null && panel.GetComponent<CanvasGroup>() != null)
            {
                FadeTransitionRunner.RunFadeThenLoadScene(panel, fadeDuration, targetSceneName);
            }
            else
            {
                SceneManager.LoadScene(targetSceneName);
            }
        }
    }
}
