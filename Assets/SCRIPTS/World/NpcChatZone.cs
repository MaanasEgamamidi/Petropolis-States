using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Trigger volume under <c>Zones</c>: when the player is inside, <see cref="NpcProximityChatOpener"/> can open
/// the named chat panel with the interact key (E). Overlapping zones use a stack (last entered wins on exit).
/// </summary>
[RequireComponent(typeof(Collider))]
public class NpcChatZone : MonoBehaviour
{
    [Tooltip("Child name under the Chats canvas root (e.g. ChatWindow_PetStore). Leave empty for hint-only zones (e.g. park).")]
    [SerializeField] string chatWindowName = "ChatWindow_PetStore";

    [SerializeField] string playerTag = "Player";

    [Tooltip("Shown by NpcProximityChatOpener when this zone is active (optional UI text). Use C for chat; E only if you add SceneTriggerZone (e.g. pet shop).")]
    [SerializeField] string interactHintWhenInside = "Press C to chat";

    static readonly List<NpcChatZone> Stack = new();

    /// <summary>Hint for the topmost chat zone the player occupies, or empty.</summary>
    public static string CurrentInteractHint { get; private set; }

    public static string GetActiveChatWindowName()
    {
        for (int i = Stack.Count - 1; i >= 0; i--)
        {
            if (Stack[i] != null && !string.IsNullOrEmpty(Stack[i].chatWindowName))
                return Stack[i].chatWindowName;
        }

        return null;
    }

    void Awake()
    {
        if (gameObject.isStatic)
            gameObject.isStatic = false;

        if (GetComponent<Rigidbody>() == null)
        {
            var rb = gameObject.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        Collider col = GetComponent<Collider>();
        if (col != null && !col.isTrigger)
            Debug.LogWarning($"[{nameof(NpcChatZone)}] {name}: enable Is Trigger on the collider.");
    }

    bool IsPlayer(Collider other)
    {
        if (other.CompareTag(playerTag))
            return true;
        Transform root = other.transform.root;
        return root != other.transform && root.CompareTag(playerTag);
    }

    void OnTriggerEnter(Collider other)
    {
        if (!IsPlayer(other))
            return;

        Stack.Add(this);
        RefreshHint();
    }

    void OnTriggerExit(Collider other)
    {
        if (!IsPlayer(other))
            return;

        Stack.Remove(this);
        RefreshHint();
    }

    void OnDisable()
    {
        Stack.Remove(this);
        RefreshHint();
    }

    static void RefreshHint()
    {
        CurrentInteractHint = string.Empty;
        for (int i = Stack.Count - 1; i >= 0; i--)
        {
            NpcChatZone z = Stack[i];
            if (z == null || string.IsNullOrEmpty(z.interactHintWhenInside))
                continue;
            CurrentInteractHint = z.interactHintWhenInside;
            return;
        }
    }
}
