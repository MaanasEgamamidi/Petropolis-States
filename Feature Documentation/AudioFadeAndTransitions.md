# Audio, fade transitions, and shared menus

## `AudioSettings` (`Assets/SCRIPTS/Menus/MainMenu.cs`)

**Static** class (defined at the top of `MainMenu.cs`, not a separate file):

- **PlayerPrefs** keys: `Petropolis_MusicVolume`, `Petropolis_SFXVolume` (0–1 raw slider values).
- **`SliderToLinear`** maps slider position to a **linear** gain using a dB curve so the bottom of the slider is ~`-24 dB` (quiet but not off immediately).
- **`MusicVolume` / `SFXVolume`** — use these when applying volume to `AudioSource` or one-shot SFX.
- **Consumers:** `MainMenu` music source, `PauseMenuSettingsHelper` (sliders), `ThirdPersonController` footstep/land clips multiply by `AudioSettings.SFXVolume`.

## `FadeTransitionRunner` (`Assets/SCRIPTS/Menus/MainMenu.cs`)

**MonoBehaviour** in the same file as `MainMenu` / `AudioSettings`:

- **`CreateFullScreenBlackFadePanel()`** — spawns a **ScreenSpaceOverlay** black `Image` with `CanvasGroup` alpha 0.
- **`RunFadeThenLoadScene`**, **`RunFadeThenLoadSceneAdditive`**, **`RunFadeThenQuit`** — build a high-sort-order canvas, run coroutine fade, then load scene or quit.
- Used by **`GameOverFlow`** (fade to `ClosingScene`), `SceneTriggerZone`, and other flows that need a full-screen black transition without a scene prefab.

## Related

- [PlayerCharacterAndHUD.md](./PlayerCharacterAndHUD.md) — `MainMenu` context.
- **Game over** (`GameOverFlow`): [PetCareAndWorldZones.md](./PetCareAndWorldZones.md). **Closing UI:** [HelpAdditiveAndClosingScenes.md](./HelpAdditiveAndClosingScenes.md).
- [PlayerPreferencesAndSettingsUI.md](./PlayerPreferencesAndSettingsUI.md) — where sliders are bound in pause.

## File reference

- `Assets/SCRIPTS/Menus/MainMenu.cs` — `AudioSettings`, `FadeTransitionRunner`, and main menu scene logic
