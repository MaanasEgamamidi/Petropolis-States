# Badges and progression

## Overview

**Badges** are defined in **`BadgeDefinitions`** (metadata, thresholds, image paths) and evaluated at runtime by **`BadgeManager`**. Progress is **per session only** (not saved to `PlayerPrefs`). Tier promotions enqueue toast-style notifications through **`BadgeNotificationQueue`** / **`BadgeNotificationController`**.

Earning data is also passed to **`ReportWriter`** for the HTML **Badges** section in [SessionReportsAndTracking.md](./SessionReportsAndTracking.md).

---

## `BadgeId` and `BadgeDefinitions` (`Assets/SCRIPTS/Core/BadgeDefinitions.cs`)

- Enum **`BadgeId`**: e.g. `PavementPounder`, `ParkPro`, `FaithfulCompanion`, `HighRoller`, `Philanthropist`, `VipPassenger`, `OffTheLeash`, `SocialButterfly`.
- **`BadgeTier`**: `None = 0`, or bronze/silver/gold for tiered badges; `Earned = 1` for single-tier badges.
- Each **`BadgeDefinition`** holds display name, category, tiered flag, `NotImplemented` (report tooltip notes missing tracking), criteria strings, and **repo-relative** PNG paths for report HTML.

**Threshold constants** (examples): distance with pet (`PavementPounder`), park in-game hours (`ParkPro`), pet-owned in-game time as days (`FaithfulCompanion`), etc. — see the source for exact numbers.

## `BadgeManager` (`Assets/SCRIPTS/Core/BadgeManager.cs`)

- **Created** on `GlobalData.OnPetConfirmed` (same moment as `SessionTracker`), DontDestroyOnLoad.
- **Metrics ingested** (static helpers):
  - `AddParkGameHours` — from `ParkZone` as game time passes in park.
  - `AddPetOwnedGameHours` — from `SessionTracker` for time owning a pet.
  - `MarkOutOfBounds` — from `BoundsExitDetector`.
  - `MarkNpcTalkedTo` — from `ChatManager` on each player message to an NPC.
- **`RecountNpcs`:** on load, counts unique `NpcIdentity.NpcName` in loaded scenes to support **Social Butterfly** (talked to all NPCs).
- **`Update` → `EvaluateAll`:** maps metrics to a target tier per badge, then **`TryPromote`** — promotes **one tier at a time** and enqueues a notification for each step (e.g. jumping three tiers still shows bronze, silver, gold toasts in order).
- **Not implemented:** `HighRoller`, `Philanthropist`, `VipPassenger` can remain at `NotImplemented` in definitions (report shows a note).

## UI queue

- **`BadgeNotificationQueue`** / **`BadgeNotificationController`** (under `Assets/SCRIPTS/UI/`) show earned badges; exact styling is in those scripts.

## Related

- `SessionTracker.DistanceWithPetMeters` → Pavement Pounder.
- [PetCareAndWorldZones.md](./PetCareAndWorldZones.md) — park and bounds hooks.

## File reference

- `Assets/SCRIPTS/Core/BadgeDefinitions.cs`, `BadgeManager.cs`
- `Assets/SCRIPTS/UI/BadgeNotificationController.cs`, `BadgeNotificationQueue.cs`
- `Assets/Badges/` — artwork referenced by `BadgeDefinitions`
