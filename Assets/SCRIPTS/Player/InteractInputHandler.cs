using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

/// <summary>Listens for E key press in the World scene and prints a debug message.</summary>
public class InteractInputHandler : MonoBehaviour
{
    void Update()
    {
        if (SceneManager.GetActiveScene().name != "World")
            return;

        if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
        {
            Debug.Log("pressed interact");
        }
    }
}
