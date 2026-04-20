using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// World: press <b>F</b> to feed the pet using <see cref="GlobalData.Inventory"/> dog food
/// (<see cref="GlobalData.NormalDogFoodItemName"/> / <see cref="GlobalData.EnhancedDogFoodItemName"/>).
/// Hunger increases per <c>NPC_Personalities/supermarket_owner.md</c>. If there is no food, plays the
/// <c>OutOfFood</c> <see cref="NotificationController"/> (slide down / hold / up).
/// </summary>
public class PetFeedController : MonoBehaviour
{
    const string WorldSceneName = "World";

    [Tooltip("If null, resolved from a GameObject named OutOfFood in the active scene.")]
    [SerializeField] NotificationController outOfFoodNotification;

    void Awake()
    {
        if (outOfFoodNotification == null)
        {
            var go = FindNamedObjectInActiveScene("OutOfFood");
            if (go != null)
                outOfFoodNotification = go.GetComponent<NotificationController>();
        }
    }

    static GameObject FindNamedObjectInActiveScene(string objectName)
    {
        var scene = SceneManager.GetActiveScene();
        foreach (var go in Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (go.name != objectName || go.scene != scene)
                continue;
            return go;
        }

        return null;
    }

    void LateUpdate()
    {
        if (!Application.isPlaying) return;
        if (SceneManager.GetActiveScene().name != WorldSceneName) return;
        if (WorldPauseController.IsPaused) return;
        if (NpcProximityChatOpener.IsWorldNpcChatOpen) return;
        if (IsTypingInFocusedInput()) return;

        if (!FeedKeyPressedThisFrame()) return;

        if (GlobalData.TryFeedDogFromInventory())
            return;

        if (outOfFoodNotification != null)
            outOfFoodNotification.Show();
    }

    static bool IsTypingInFocusedInput()
    {
        if (EventSystem.current == null) return false;
        GameObject sel = EventSystem.current.currentSelectedGameObject;
        if (sel == null) return false;
        var field = sel.GetComponent<TMP_InputField>();
        return field != null && field.isFocused;
    }

    static bool FeedKeyPressedThisFrame()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.F);
#endif
    }
}
