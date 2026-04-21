using System.Collections.Generic;
using Petropolis.Minigames;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Trigger volume placed on a Game*NPCZone under <c>Zones</c>. While the player is inside, shows a
/// hint (via <see cref="NpcChatZone.CurrentInteractHint"/>) and pressing E (keyboard) or X / west
/// face button on an Xbox controller launches the assigned minigame additively. The World scene is
/// frozen by <see cref="MinigameSession"/>, the minigame runs on top, and on game-over the player
/// is returned here with whatever they earned credited to <see cref="GlobalData.PlayerMoney"/>.
/// </summary>
[RequireComponent(typeof(Collider))]
public class MinigameLauncherZone : MonoBehaviour
{
    [Tooltip("Exact name (no path, no extension) of the minigame scene to launch additively. Must be in Build Settings.")]
    [SerializeField] string minigameSceneName = "";

    [Tooltip("Friendly label used in the 'You earned $X from <label>!' notification.")]
    [SerializeField] string minigameLabel = "";

    [Tooltip("Shown while the player stands in the zone (handled by NpcProximityChatOpener's hint text).")]
    [SerializeField] string interactHint = "Press E / X to play";

    [SerializeField] string playerTag = "Player";

    static readonly List<MinigameLauncherZone> Stack = new List<MinigameLauncherZone>();

    /// <summary>Topmost hint string across all active launcher zones, or empty.</summary>
    public static string CurrentInteractHint { get; private set; } = string.Empty;

    bool _playerInside;

    void Awake()
    {
        if (gameObject.isStatic)
            gameObject.isStatic = false;

        Collider col = GetComponent<Collider>();
        if (col != null && !col.isTrigger)
            Debug.LogWarning($"[{nameof(MinigameLauncherZone)}] {name}: enable Is Trigger on the collider.");

        if (GetComponent<Rigidbody>() == null)
        {
            var rb = gameObject.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;
        }
    }

    bool IsPlayer(Collider other)
    {
        if (other == null) return false;
        if (other.CompareTag(playerTag)) return true;
        Transform root = other.transform.root;
        return root != other.transform && root.CompareTag(playerTag);
    }

    void OnTriggerEnter(Collider other)
    {
        if (!IsPlayer(other)) return;
        _playerInside = true;
        if (!Stack.Contains(this)) Stack.Add(this);
        RefreshHint();
    }

    void OnTriggerExit(Collider other)
    {
        if (!IsPlayer(other)) return;
        _playerInside = false;
        Stack.Remove(this);
        RefreshHint();
    }

    void OnDisable()
    {
        _playerInside = false;
        Stack.Remove(this);
        RefreshHint();
    }

    void Update()
    {
        if (!_playerInside) return;
        if (MinigameSession.IsActive) return;
        if (WorldPauseController.IsPaused) return;
        if (NpcProximityChatOpener.IsWorldNpcChatOpen || WorldPauseController.IsFocusedOnTextInput()) return;

        if (!InteractPressedThisFrame()) return;

        if (string.IsNullOrWhiteSpace(minigameSceneName))
        {
            Debug.LogWarning($"[{nameof(MinigameLauncherZone)}] {name}: no minigame scene assigned.");
            return;
        }

        MinigameSession.StartSession(minigameSceneName, minigameLabel);
    }

    static bool InteractPressedThisFrame()
    {
#if ENABLE_INPUT_SYSTEM
        // E on keyboard, or the X (west) button on an Xbox controller.
        if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame) return true;
        if (Gamepad.current != null && Gamepad.current.buttonWest.wasPressedThisFrame) return true;
        return false;
#else
        return Input.GetKeyDown(KeyCode.E);
#endif
    }

    static void RefreshHint()
    {
        CurrentInteractHint = string.Empty;
        for (int i = Stack.Count - 1; i >= 0; i--)
        {
            MinigameLauncherZone z = Stack[i];
            if (z == null || string.IsNullOrEmpty(z.interactHint)) continue;
            CurrentInteractHint = z.interactHint;
            return;
        }
    }
}
