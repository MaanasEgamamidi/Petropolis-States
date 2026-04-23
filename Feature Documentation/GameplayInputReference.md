# Gameplay input reference

Single source of truth: **`GameplayInput`** (`Assets/SCRIPTS/Core/GameplayInput.cs`). It uses the **new Input System** when `ENABLE_INPUT_SYSTEM` is defined; otherwise legacy `UnityEngine.Input` key checks for keyboard-only shortcuts.

`GetUsableGamepad()` is provided because on Windows, `Gamepad.current` can be null until the first input; the helper scans `Gamepad.all` and nudges the Input System with `InputSystem.Update()` when legacy joystick names appear.

## World / gameplay (typical)

| Action | Keyboard | Gamepad (Xbox layout) |
|--------|----------|------------------------|
| Interact (zones, doors) | **E** | **West (X)** |
| NPC chat (open) | **C** | **North (Y)** |
| Pause / resume | **Esc** | **Right bumper (RB)** |
| Gameplay settings (pause menu) | (via pause UI) | **View/Back (select)** — see `WorldPauseController` |
| Minimap show/hide | **M** | **Left bumper (LB)** |
| Teleport menu | **T** | **D-pad Up** |
| Inventory | **G** | **D-pad Left** or **Start/Menu (hamburger)** |
| Feed pet | **F** | **D-pad Down** |
| Loans panel | **L** | **Right trigger (RT)** |
| D-pad UI navigation (step) | — | D-pad only (left stick is reserved for the virtual pointer) |

**Cancel / back** in some overlays: **B (East)** via `GamepadCancelPressedThisFrame()`.

**Submit / confirm** on focused UI: `GamepadSubmitPressedThisFrame()` — **A (South)**.

Individual scripts may add extra conditions (e.g. `WorldPauseController.IsPaused`, `NpcProximityChatOpener.IsWorldNpcChatOpen`, `InventoryPanelController.IsOpen`).

## Related

- [WorldTravelAndInteraction.md](./WorldTravelAndInteraction.md) — C vs E vs T and zone stacks.
- [PlayerCharacterAndHUD.md](./PlayerCharacterAndHUD.md) — pause and cursor.
- [WorldUIPanels.md](./WorldUIPanels.md) — G / L / F behavior with HUD hide rules.

## File reference

- `Assets/SCRIPTS/Core/GameplayInput.cs`
