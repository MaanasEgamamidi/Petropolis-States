# Minigames (additive sessions)

## Overview

**`MinigameSession`** (`namespace Petropolis.Minigames`) loads a minigame scene **additively** from the **World** scene, **deactivates** all World root GameObjects (same pattern as additive Pet Shop), and sets the minigame as the **active** scene. **Cursor** is unlocked for minigame UI. On end, the minigame unloads, World is restored, **cursor locks** for third-person again, and **rounded** accumulated dollars are added to **`GlobalData.PlayerMoney`**.

**Entry:** stand in a **`MinigameLauncherZone`** and press **E** (keyboard) or **X** (Xbox / west face) — see [WorldTravelAndInteraction.md](./WorldTravelAndInteraction.md).

---

## `MinigameSession` (`Assets/SCRIPTS/Minigames/MinigameSession.cs`)

| Member | Role |
|--------|------|
| `ActiveMinigameSceneName` | Current additive scene, or empty. |
| `ActiveMinigameLabel` | Short string for the return notification. |
| `PendingEarnings` | Dollars accumulated this run (integer-rounded on payout). |
| `IsActive` | Whether a minigame session is in progress. |

| Method | Role |
|--------|------|
| `StartSession(sceneName, friendlyLabel)` | Validates World is loaded, freezes roots, async additive load, resets earnings. |
| `AddPayout(float dollars)` | Minigame logic calls this while the session is active. |
| `EndSession()` | Unloads, unfreezes, credits money, shows **`NotificationController`** message. |

- **Re-entrancy:** `EndSession` clears state immediately so double “game over” does not pay twice.
- If additive load **fails** (e.g. scene not in Build Settings), the World is **unfrozen** and an error is logged.

## Included minigames (scripts under `Assets/SCRIPTS/Minigames/`)

- **Pong** — `PongGameManager`, `PongBall`, `PongPaddle`, `PongAIPaddle` (classic paddle game; payout wired via `AddPayout` in manager logic).
- **Flappy Bird** — `FlappyBirdGameManager`, `FlappyBirdBird`, `FlappyBirdBeamSpawner`, helpers.
- **Reaction time** — `World/Minigames/ReactionTimeGame` (three-attempt timing; see [ReactionTimeMinigame.md](./ReactionTimeMinigame.md)).

Each minigame scene should be listed in **Build Settings** and call **`MinigameSession.AddPayout`** and **`MinigameSession.EndSession`** at appropriate moments (e.g. on exit or game over).

## Related

- [WorldTravelAndInteraction.md](./WorldTravelAndInteraction.md) — `MinigameLauncherZone`, hint priority over NPC chat.
- [PlayerMovement.md](./PlayerMovement.md) — World frozen so player controller does not run during minigame.

## File reference

- `Assets/SCRIPTS/Minigames/MinigameSession.cs`
- `Assets/SCRIPTS/Minigames/Pong/*.cs`, `FlappyBird/*.cs`
