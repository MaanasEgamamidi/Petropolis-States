using UnityEngine;

/// <summary>
/// Disabled. Previously synthesized Ctrl+Win+Space (Menu/Start/hamburger) and Ctrl+Win (right trigger)
/// on Windows. Both bindings have been removed; the hamburger button now opens the inventory via
/// <see cref="GameplayInput.InventoryTogglePressedThisFrame"/>, and the right trigger opens the
/// loans panel via <see cref="GameplayInput.LoanPanelTogglePressedThisFrame"/>.
/// Kept as an empty MonoBehaviour so any existing serialized references remain valid.
/// </summary>
public sealed class WindowsCtrlWinGamepadBridge : MonoBehaviour { }
