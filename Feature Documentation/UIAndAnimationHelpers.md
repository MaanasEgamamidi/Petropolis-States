# UI and animation helper components

Small, reusable building blocks that other pages reference lightly.

## `ButtonStyleHelper` (`Assets/SCRIPTS/UI/ButtonStyleHelper.cs`)

- **Static** helpers: **`EnsureDimCanvasGroup`** on a `Button` root, **`ApplyInteractableWithDim`** to set `interactable` and **CanvasGroup** alpha (disabled ≈ 0.48) so text and image dim together.

## `ChatInputField` (`Assets/SCRIPTS/Chat/ChatInputField.cs`)

- Requires **`TMP_InputField`**. **MultiLineSubmit** — Enter sends, Shift+Enter newline.
- **Expanding height** between min/max (iMessage-style) with **`LayoutElement`**.
- Fires **`OnSubmitMessage`** with trimmed text; used by `ChatManager` (see [NPCChatAndOpenAI.md](./NPCChatAndOpenAI.md)).

## `CursorVisibilityKeeper` (`Assets/SCRIPTS/UI/CursorVisibilityKeeper.cs`)

- On **EventSystem** in menu scenes (main menu, character choose, pet choose): **forces** visible, unlocked cursor every frame.
- Respects **`SuppressMenuHardwareCursor`** (set by **`ControllerVirtualCursor`** when the gamepad pointer idles) so a hidden software cursor can stay hidden.

## `PetStatLowWarningController` (`Assets/SCRIPTS/UI/PetStatLowWarningController.cs`)

- Watches **GlobalData** health, happiness, energy, hunger, cleanliness; when a stat **crosses** from at/above a **threshold** to below, shows a linked **`NotificationController`** (debounced by stat).

## `NPCProximity` (`Assets/SCRIPTS/NPC/NPCProximity.cs`)

- Per-frame distance from **Player** tag to listed **`Animator`s**; sets bool **`PlayerNearby`** only if the **controller actually has** that parameter (caches per animator to avoid Unity warnings for wrong controller types). Used for **NPC idle / wave** type transitions.

## `WindowsCtrlWinGamepadBridge` (`Assets/SCRIPTS/Core/WindowsCtrlWinGamepadBridge.cs`)

- **Empty, disabled stub** — kept so old scene references do not break. **Start** and **RT** for inventory/loans are now handled in **`GameplayInput`** (see [GameplayInputReference.md](./GameplayInputReference.md)).

## Related

- [PlayerCharacterAndHUD.md](./PlayerCharacterAndHUD.md) — `ControllerVirtualCursor`, `NotificationController`.
- [NPCChatAndOpenAI.md](./NPCChatAndOpenAI.md) — chat input.

## File reference

- `Assets/SCRIPTS/UI/ButtonStyleHelper.cs`, `CursorVisibilityKeeper.cs`, `PetStatLowWarningController.cs`
- `Assets/SCRIPTS/Chat/ChatInputField.cs`
- `Assets/SCRIPTS/NPC/NPCProximity.cs`
- `Assets/SCRIPTS/Core/WindowsCtrlWinGamepadBridge.cs`
