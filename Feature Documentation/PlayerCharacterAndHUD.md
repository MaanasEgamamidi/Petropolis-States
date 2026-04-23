# Player character, companion, and HUD

## Overview

**`PlayerSpawner`** instantiates the selected **body/head** meshes from `GlobalData` under the **PlayerArmature** with correct **avatars** and **Starter Assets** animator. **`PlayerFollow`** moves the **dog** companion, records **distance** for `SessionTracker` and badges, and animates the pet. **`WorldHUDManager`**, **`MoneyCounterDisplay`**, **`PetStatsBarsController`**, and **`NotificationController`** show live HUD state. Input helpers live in **`GameplayInput`**. Pause behavior is centralized in **`WorldPauseController`** (see below).

---

## `PlayerSpawner` (`Assets/SCRIPTS/Player/PlayerSpawner.cs`)

- Reads **`GlobalData.StoredGender`**, `StoredBody`, `StoredHead`.
- Picks **male** or **female** prefab, parents mesh children under a **visual container** under `playerArmature` (scale affects **visuals only** — not CharacterController or camera).
- Strips unselected **Body_*** / **Head_*** parts per naming convention.
- Assigns **StarterAssetsThirdPerson** controller and **male/female Avatar** to the **Animator** on the armature.
- Can disable a **default** placeholder character if assigned.

## `PlayerFollow` (`Assets/SCRIPTS/Player/PlayerFollow.cs`)

- Positions the pet in a **follow slot** relative to the player, **MoveTowards** / ground height sampling, **matches speed** to player and sprint.
- **Dog animator** `Speed` parameter from shared helper (see [PlayerMovement.md](./PlayerMovement.md)).
- **`SessionTracker.AddDistanceWithPetMeters`:** passes horizontal movement delta for session report distance and Pavement Pounder badge.
- **Record distance** for session report when applicable (read script for `RecordDistanceWithPetForSessionReport` or similar).

## HUD and UI

- **`WorldHUDManager`** — binds legacy **UI.Text** to pet name and stat labels; finds objects by name if references null (`Hunger Text`, `Health Text`, etc.).
- **`PetStatsBarsController`**, **`PetStatLowWarningController`**, **`MoneyCounterDisplay`**, **`InventoryPanelController`**, **`LoansPanelController`**, **`PetFeedController`**, **`MinimapDisplayController`** — dedicated scripts; all typically respect `WorldPauseController.IsPaused` before handling input.
- **`NotificationController`** — slide-in toasts; auto modes **OnSceneStart** / **AfterPetChosen**; `MinigameSession` uses it for payout messages. See [Minigames.md](./Minigames.md).

## `GameplayInput` (`Assets/SCRIPTS/Core/GameplayInput.cs`)

- Shared **Keyboard + gamepad** checks for world gameplay; documents face-button layout (South confirm, **West = interact**). **`GetUsableGamepad()`** works around `Gamepad.current` being null on Windows until first input.

## `WorldPauseController` (`Assets/SCRIPTS/UI/WorldPauseController.cs`)

- **Escape / RB** toggles pause menu; **View/Back** can open settings. Sets **`Time.timeScale = 0`**, static **`IsPaused`**, fades HUD, manages **help overlay** additive scene.
- Many systems short-circuit when paused — player movement, feed, teleporter, etc.

## `ControllerVirtualCursor` (`Assets/SCRIPTS/UI/ControllerVirtualCursor.cs`)

- Gamepad **cursor** for UI when World uses locked mouse; cooperates with pause and NPC chat.

## `InteractInputHandler` (`Assets/SCRIPTS/Player/InteractInputHandler.cs`)

- **Debug** listener for E in World; minimal — real interact lives on zones and `GameplayInput`.

## Related

- [PlayerMovement.md](./PlayerMovement.md) — `ThirdPersonController`, Cinemachine, pause and chat gating.
- [SessionReportsAndTracking.md](./SessionReportsAndTracking.md) — distance tracking from `PlayerFollow`.
- [API.md](./API.md) — `GlobalData` for HUD values.

## File reference

- `Assets/SCRIPTS/Player/PlayerSpawner.cs`, `PlayerFollow.cs`, `ThirdPersonController.cs`, `InteractInputHandler.cs`
- `Assets/SCRIPTS/UI/WorldHUDManager.cs`, `WorldPauseController.cs`, `NotificationController.cs`, `ControllerVirtualCursor.cs`
- `Assets/SCRIPTS/Core/GameplayInput.cs`, `NameSelectionSettings.cs` (naming / menu prefs if used with character flow)

## Menus (high level)

- **`ChracterChoose` / `PetChooseController`** — character and pet selection writing to `GlobalData`.
- **`InstructionsController`**, **`HelpQaOverlayController`**, **`ClosingSequenceController`** — onboarding, help Q&A, end-of-run sequence reading `GameOverFlow.LastReason`.

For menu-specific setup, see scene wiring and the scripts in `Assets/SCRIPTS/Menus/`.
