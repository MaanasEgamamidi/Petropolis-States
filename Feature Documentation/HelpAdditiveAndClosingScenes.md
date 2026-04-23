# Additive loading, help overlays, and closing flow

## `AdditiveSceneHelper` (`Assets/SCRIPTS/Core/AdditiveSceneHelper.cs`)

- **Auto-created** after scene load if none exists (DontDestroyOnLoad).
- When **`PetChoose`** loads **additively**:
  - **Disables the World’s EventSystem** first (`FreezeWorldBeforePetChooseLoad` — public static) to avoid duplicate EventSystem warnings.
  - After load: **deactivates all root GameObjects** in **World** (full freeze, same idea as `MinigameSession`).
  - **Offsets** PetChoose root objects by **(0, 0, 25000)** so content does not overlap the world in the editor or at runtime.
- On **PetChoose unload:** **re-enables** World roots, **locks** cursor and hides it for third-person.
- Pairs with **`SceneTriggerZone`** and **`GlobalData.PetChooseOpenedFromWorld`** for return behavior (see [WorldTravelAndInteraction.md](./WorldTravelAndInteraction.md)).

## Instructions scene — `InstructionsController`

**`Assets/SCRIPTS/Menus/InstructionsController.cs`**

- **Play** usually loads `CharacterChoose`.
- If **`ReturnToPauseOnPlay`** is true (set by **`WorldPauseController`** before additive load from pause **Help**), **Play** unloads the instructions overlay and returns to the paused world instead.
- Can spawn an **“Ask the guide”** style button to load **`HelpQa`** (configurable `helpQaSceneName`).

## Help Q&amp;A overlay — `HelpQaOverlayController`

**`Assets/SCRIPTS/Menus/HelpQaOverlayController.cs`**

- Additive **HelpQa** scene. **`ReturnToPauseOnClose`**: if set by `WorldPauseController`, **Close** / **Escape** / **B** unloads and returns under pause.
- Optional **full-screen** backdrop sprite. Creates a **HelpBack** button if missing.
- **Skip cancel** if `WorldPauseController.IsFocusedOnTextInput()`.

**`HelpQaOverlayLoadUtility.cs`** — small helper for loading the overlay (see file for static API).

## Closing sequence — `ClosingSequenceController`

**`Assets/SCRIPTS/UI/ClosingSequenceController.cs`**

- Used for **`ClosingScene`** after game over (or when quitting into closing from the menu — flow-dependent).
- Shows screen for **`displaySeconds`**, optional click to skip, then **`FadeTransitionRunner.RunFadeThenQuit`**.
- **`reasonSubtext`**_TMP fills from **`GameOverFlow.LastReason`** and templates: starvation, **neglect**, **pet taken by bank**; `{pet}` comes from `GlobalData.PetName`.
- Unlocks cursor for the closing UI; **`incomingFadeCompensation`** and **`clickGraceAfterLoad`** avoid double-input from the previous fade/button.

## Related

- [AudioFadeAndTransitions.md](./AudioFadeAndTransitions.md) — `FadeTransitionRunner`.
- [WorldTravelAndInteraction.md](./WorldTravelAndInteraction.md) — `SceneTriggerZone`.
- [PlayerCharacterAndHUD.md](./PlayerCharacterAndHUD.md) — `WorldPauseController` help overlay.

## File reference

- `Assets/SCRIPTS/Core/AdditiveSceneHelper.cs`
- `Assets/SCRIPTS/Menus/InstructionsController.cs`, `HelpQaOverlayController.cs`, `HelpQaOverlayLoadUtility.cs`
- `Assets/SCRIPTS/UI/ClosingSequenceController.cs`
