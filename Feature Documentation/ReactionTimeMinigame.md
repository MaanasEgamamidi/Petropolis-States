# Reaction time minigame

## Overview

**`ReactionTimeGame`** (`namespace Petropolis.Minigames`, `Assets/SCRIPTS/World/Minigames/ReactionTimeGame.cs`) is a **three-attempt** reaction test: wait for a **green** “go” signal, then press as fast as possible (click / **Space** / **A (South)**). Early presses register as **too soon** on that attempt.

## Economy

When run from a **`MinigameLauncherZone`** and **`MinigameSession`** is active:

- Each completed attempt gets a **base dollar** amount (Inspector: `baseDollarsPerAttempt`).
- Reactions **faster** than a cutoff (default **600 ms**) add **`dollarsPerMsSaved`** per millisecond under that cutoff.
- After the **third** attempt, totals are banked into **`MinigameSession.PendingEarnings`**, then the normal **EndSession** path credits **`GlobalData.PlayerMoney`** (rounded).

Tuning: `minWait` / `maxWait` random delay before green, `finishedHoldSeconds` before returning to the world.

## Related

- [Minigames.md](./Minigames.md) — `MinigameSession` lifecycle, Pong / Flappy entry points.
- [WorldTravelAndInteraction.md](./WorldTravelAndInteraction.md) — launcher zones.

## File reference

- `Assets/SCRIPTS/World/Minigames/ReactionTimeGame.cs`
