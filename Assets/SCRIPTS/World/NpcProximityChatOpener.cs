using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// World scene: while inside an <see cref="NpcChatZone"/> that has a chat panel, press <b>C</b> to open it under Chats.
/// Press C again to close. (Pet shop also uses <b>E</b> on <see cref="SceneTriggerZone"/> for PetChoose.)
/// Optional <see cref="interactHintText"/> shows <see cref="NpcChatZone.CurrentInteractHint"/>.
/// </summary>
public class NpcProximityChatOpener : MonoBehaviour
{
    /// <summary>True while a World NPC chat panel is open — used to freeze look/move like pause.</summary>
    public static bool IsWorldNpcChatOpen { get; private set; }

    const string WorldSceneName = "World";

    [Header("Optional UI")]
    [SerializeField] TMP_Text interactHintText;

    readonly Dictionary<string, GameObject> _chatByName = new();

    GameObject _openChatRoot;

    void Awake()
    {
        foreach (Transform child in transform)
            _chatByName[child.name] = child.gameObject;
    }

    void OnDestroy()
    {
        IsWorldNpcChatOpen = false;
    }

    void LateUpdate()
    {
        if (SceneManager.GetActiveScene().name != WorldSceneName)
            return;

        if (interactHintText != null)
        {
            if (WorldPauseController.IsPaused || IsWorldNpcChatOpen)
                interactHintText.text = string.Empty;
            else
                interactHintText.text = NpcChatZone.CurrentInteractHint ?? string.Empty;
        }

        if (WorldPauseController.IsPaused)
            return;
        if (_openChatRoot == null || !_openChatRoot.activeSelf)
            return;
        SetGameUiCursorForChat(true);
    }

    void Update()
    {
        if (SceneManager.GetActiveScene().name != WorldSceneName)
            return;
        if (WorldPauseController.IsPaused)
            return;
        if (!ChatKeyPressedThisFrame())
            return;

        if (_openChatRoot != null && _openChatRoot.activeSelf)
        {
            CloseChat();
            return;
        }

        string panelName = NpcChatZone.GetActiveChatWindowName();
        if (string.IsNullOrEmpty(panelName))
            return;

        if (_chatByName.TryGetValue(panelName, out GameObject chatGo) && chatGo != null)
            OpenChat(chatGo);
        else
            Debug.LogWarning($"[NpcProximityChatOpener] Missing chat window '{panelName}' under {gameObject.name}.");
    }

    void OpenChat(GameObject chatRoot)
    {
        foreach (KeyValuePair<string, GameObject> kv in _chatByName)
        {
            if (kv.Value != null)
                kv.Value.SetActive(false);
        }

        chatRoot.SetActive(true);
        _openChatRoot = chatRoot;
        IsWorldNpcChatOpen = true;
        SetGameUiCursorForChat(true);

        ChatInputField chatInput = chatRoot.GetComponentInChildren<ChatInputField>(true);
        if (chatInput != null && EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(chatInput.InputField.gameObject);
            chatInput.InputField.ActivateInputField();
        }
    }

    void CloseChat()
    {
        if (_openChatRoot != null)
            _openChatRoot.SetActive(false);
        _openChatRoot = null;

        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);

        IsWorldNpcChatOpen = false;
        SetGameUiCursorForChat(false);
    }

    void SetGameUiCursorForChat(bool chatOpen)
    {
#if ENABLE_INPUT_SYSTEM
        foreach (StarterAssets.StarterAssetsInputs sai in Object.FindObjectsByType<StarterAssets.StarterAssetsInputs>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            sai.SetUiMenuOpen(chatOpen);
#else
        if (chatOpen)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
#endif
    }

    static bool ChatKeyPressedThisFrame()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null && Keyboard.current.cKey.wasPressedThisFrame)
            return true;
        if (Gamepad.current != null && Gamepad.current.buttonNorth.wasPressedThisFrame)
            return true;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetKeyDown(KeyCode.C);
#else
        return false;
#endif
    }
}
