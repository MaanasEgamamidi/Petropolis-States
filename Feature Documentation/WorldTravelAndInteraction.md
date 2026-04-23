# World travel, interaction, and boundaries

## Overview

The **World** scene ties together **trigger volumes** (chat, shops, minigames, home, park, bed), **teleportation** (`TeleportMenuController`), **elevators** (`ElevatorTeleportZone` / `ElevatorDownTeleportZone`), **scene loads** (`SceneTriggerZone`), and **out-of-bounds** detection (`BoundsExitDetector`). Most triggers use a **kinematic Rigidbody** on the zone and **`isTrigger`** on the collider so **CharacterController** reliably generates overlap events.

---

## NPC chat vs minigame vs shop entry

| System | Typical input | Purpose |
|--------|---------------|---------|
| `NpcChatZone` + `NpcProximityChatOpener` | **C** | Open AI chat panel named by zone. |
| `MinigameLauncherZone` | **E / X** | Start `MinigameSession` (see [Minigames.md](./Minigames.md)). |
| `SceneTriggerZone` | **E** (configurable) | Load another scene (fade optional). |

**Hint text:** `NpcProximityChatOpener` shows `MinigameLauncherZone.CurrentInteractHint` when present, else `NpcChatZone.CurrentInteractHint`, so minigames take priority over “Press C to chat”.

All of the above respect **`WorldPauseController.IsPaused`** and **NPC chat** / **text input focus** where applicable (see individual scripts).

---

## `TeleportMenuController` (`Assets/SCRIPTS/World/TeleportMenuController.cs`)

- **T** toggles a **runtime-built** grid of buttons: one per child under `teleportLocationsRoot` (default name `TeleportLocations`).
- Each click **teleports** the player to a fixed world position (mapped in source from child order — see script for `FixedTeleportWorldPositions` or equivalent) and deducts **`teleportCost`** from `GlobalData.PlayerMoney`. Buttons **disable** when unaffordable.

## `SceneTriggerZone` (`Assets/SCRIPTS/World/SceneTriggerZone.cs`)

- Player in trigger + **interact key** loads **`targetSceneName`** (Build Settings). Optional **fade** panel + duration.
- **One-time use** option tracks per-zone id so return visits do not re-fire.
- **PetChoose** additive path is **special-cased** when the target is the pet-choose flow (see script comments) vs generic scene loads.

## Elevators

- **`ElevatorTeleportZone`:** in zone, **R** or **Y (north)** moves player **up** (fixed `AscendMeters`), with CharacterController disabled briefly. Uses overlap checks because triggers can be unreliable for CharacterController.
- **`ElevatorDownTeleportZone`:** partner for descending (read script for key and offset).
- **`ElevatorSlide`:** related animation/position script for elevator movement (if used in scene).

## `AreaEnterZone` (`Assets/SCRIPTS/World/AreaEnterZone.cs`)

- On **first** trigger enter: shows **`NotificationController`** message **"You entered the &lt;area&gt;!"** (area name from Inspector or **humanized** `GameObject` name, e.g. `PetStore` → "pet store"). Uses a scene **`AreaEnter`** object or a serialized `NotificationController` reference.

## `BoundsExitDetector` (`Assets/SCRIPTS/World/BoundsExitDetector.cs`)

- Large trigger **“Bounds”** wrapping the play space: when the **player leaves**, calls **`BadgeManager.MarkOutOfBounds`** once per session (Off the Leash).
- **Dual detection:** `OnTriggerExit` plus **polling** the player position against collider bounds on an interval, so fast exits, teleport out, or spawn outside still count.

## `RotateDoor` (`Assets/SCRIPTS/World/RotateDoor.cs`)

- Animated / interactable door (see Inspector and script for use).

## `ColliderInteractionDistance` (`Assets/SCRIPTS/World/ColliderInteractionDistance.cs`)

- Optional helper to gate interaction by distance to collider (used where assigned).

## `MinigameLauncherZone` (`Assets/SCRIPTS/World/MinigameLauncherZone.cs`)

- Stack-based hint (like `NpcChatZone`) for **“Press E / X to play”**; launches **`MinigameSession.StartSession`**.

## Related

- [NPCChatAndOpenAI.md](./NPCChatAndOpenAI.md) — C vs E behavior, chat open state.
- [Minigames.md](./Minigames.md) — session lifecycle.
- [BadgesAndProgression.md](./BadgesAndProgression.md) — bounds badge.

## File reference

- `Assets/SCRIPTS/World/TeleportMenuController.cs`, `SceneTriggerZone.cs`, `NpcChatZone.cs`, `NpcProximityChatOpener.cs`, `MinigameLauncherZone.cs`
- `Assets/SCRIPTS/World/ElevatorTeleportZone.cs`, `ElevatorDownTeleportZone.cs`, `ElevatorSlide.cs`, `AreaEnterZone.cs`, `BoundsExitDetector.cs`, `RotateDoor.cs`, `ColliderInteractionDistance.cs`
