# Minimap system (camera, UI, and preferences)

## `MinimapController` (`Assets/SCRIPTS/World/MinimapController.cs`)

- **Orthographic** camera, fixed height above the player, **north-up** (rotation X = 90°).
- **Follow target:** optional `Transform`; if null, finds **`ThirdPersonController`**, else **`GameObject` with "Player" tag**. Uses **`CharacterController`** position when found so camera pitch from child objects does not skew the point.
- **When disabled:** if `MinimapSettings.Mode == None`, or when hidden and not fading (`PresentationAlpha` check), the camera is turned off to save work.
- **M / LB** toggles `MinimapSettings.RuntimeSuppressed` (works with `GameplayInput.MinimapTogglePressedThisFrame` / legacy **M**).
- While **NPC world chat** is open, minimap is effectively suppressed (`!NpcProximityChatOpener.IsWorldNpcChatOpen` for “requested” state).

## `MinimapDisplayController` (`Assets/SCRIPTS/UI/MinimapDisplayController.cs`)

- Drives the **UI RawImage** for the minimap: **Custom/UI/MinimapRawImage** material (`MinimapRawImageUI.mat` in project) for **circle vs square** and optional **border** in UV space.
- **`ExecuteAlways`**: editor preview of material instance.
- On play: applies **`MinimapSettings.Mode`**. For **None**, forces canvas alpha so other systems (pause HUD) cannot show a “dot with no map.”
- **Fade** when toggling visibility; tracks **`MinimapSettings.PresentationAlpha`** for the orthographic camera handoff in `MinimapController`.
- Shape from settings: **Circle** vs **Square** maps to `MinimapMode` and shader keywords / properties (see file for `ApplyFromPlayerSettings`).

## `MinimapSettings`

See [PlayerPreferencesAndSettingsUI.md](./PlayerPreferencesAndSettingsUI.md) for prefs and pause dropdown.

## Related

- [GameplayInputReference.md](./GameplayInputReference.md) — M / LB.
- [WorldUIPanels.md](./WorldUIPanels.md) — inventory hides minimap.
- [NPCChatAndOpenAI.md](./NPCChatAndOpenAI.md) — chat blocks minimap rendering.

## File reference

- `Assets/SCRIPTS/World/MinimapController.cs`
- `Assets/SCRIPTS/UI/MinimapDisplayController.cs`
- `Assets/SCRIPTS/Core/MinimapSettings.cs`
