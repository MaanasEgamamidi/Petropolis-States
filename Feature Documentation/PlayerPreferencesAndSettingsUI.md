# Player preferences and settings UI

Values are stored in **`PlayerPrefs`** unless noted. The **main menu** and **pause menu** use the same bindings via **`PauseMenuSettingsHelper`**.

## Audio

**`AudioSettings`** — see [AudioFadeAndTransitions.md](./AudioFadeAndTransitions.md). Keys: `Petropolis_MusicVolume`, `Petropolis_SFXVolume`.

**Pause:** `PauseMenuSettingsHelper.BindAudioSliders` finds `MusicSlider` and `SFXSlider` under the pause root; music changes also call `MainMenu.RefreshMusicVolumeFromSettings()` so the world music source updates.

## Minimap mode

**`MinimapSettings`** (`Assets/SCRIPTS/Core/MinimapSettings.cs`)

- Key: `Petropolis_MinimapMode`.
- **`MinimapMode`**: Circle, Square, None — order matches the TMP dropdown in menus.
- **`RuntimeSuppressed`** — in-session only (e.g. **M** / **LB** in `MinimapController`); not saved. Cleared when the minimap UI reapplies.
- **`PresentationAlpha` / `IsVisible`** — used by the minimap UI and `MinimapController` to avoid rendering a map when hidden.

Full rendering pipeline: [MinimapSystem.md](./MinimapSystem.md).

## Pet info display mode

**`PetInfoSettings`** (`Assets/SCRIPTS/Core/PetInfoSettings.cs`)

- Key: `Petropolis_PetInfoMode`.
- **`PetInfoDisplayMode`**: AlwaysOn, Toggle, HoldToShow (0–2). Drives [Pet statistics panel](./WorldUIPanels.md#pet-statistics-slide-out--petstatisticspanelcontroller).

`PauseMenuSettingsHelper.BindPetInfoDropdown` wires **`PetInfoDropdown`**.

## Full day length (real minutes)

**`DayNightSettings`** — see [DayNightCycle.md](./DayNightCycle.md). `PauseMenuSettingsHelper.BindLengthOfDayInput` finds **`LengthOfDayInput`** TMP field and `DayNightSettings.SetFullCycleMinutes`.

## Name selection (character / pet)

**`NameSelectionSettings`** (`Assets/SCRIPTS/Core/NameSelectionSettings.cs`)

- Key: `Petropolis_MandatoryNameSelection` — when **1** (default), flows require non-empty custom names.
- **Editor menu:** `Edit > Mandatory Name Selection` (checked = mandatory) — see [EditorSessionTools.md](./EditorSessionTools.md).

## `PauseMenuSettingsHelper` entry point

**`Bind(Transform root)`** (`Assets/SCRIPTS/UI/PauseMenuSettingsHelper.cs`):

- Audio sliders, minimap dropdown, pet info dropdown, length-of-day input — searches children by **object name** (e.g. `MusicSlider`, `MinimapDropdown`, `LengthOfDayInput`).

## Related

- [MinimapSystem.md](./MinimapSystem.md)
- [API.md](./API.md) — economy not covered here.

## File reference

- `Assets/SCRIPTS/Core/MinimapSettings.cs`, `PetInfoSettings.cs`, `NameSelectionSettings.cs`, `DayNightSettings.cs`
- `Assets/SCRIPTS/UI/PauseMenuSettingsHelper.cs`
- `Assets/SCRIPTS/Menus/MainMenu.cs` — settings UI on the main menu side
