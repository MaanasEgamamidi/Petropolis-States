# Session reports and tracking

## Overview

After the player **confirms a pet** (`GlobalData.OnPetConfirmed`), a **`SessionTracker`** singleton is created and lives for the play session. It records economy snapshots, pet stat history, **distance walked with the pet**, **purchases**, **feedings**, and cooperates with **`SessionChatLog`** for the HTML report.

On **quit** or **Editor play-mode stop**, **`ReportWriter`** writes a numbered **`SessionReportN.html`** under **`Session Reports/`** at the project root (next to `Assets`).

---

## `SessionTracker` (`Assets/SCRIPTS/Core/SessionTracker.cs`)

- **When:** `RuntimeInitializeOnLoadMethod` subscribes to `GlobalData.OnPetConfirmed` → spawns a `SessionTracker` on a DontDestroyOnLoad GameObject.
- **Snapshots:** On each new **in-game hour** (from `GlobalData.CurrentTimeOfDay`, updated by `DayNightController` — see [DayNightCycle.md](./DayNightCycle.md)), records money and pet stats (`MoneySnapshot`). Consecutive same-hour points are **merged** to avoid duplicate last points on exit.
- **Money watermarks** update on every `OnMoneyChanged`.
- **Purchases** — subscribes to `GlobalData.OnItemPurchased` and stores item name, pack quantity, price per unit, game hour, real seconds since start, UTC.
- **Feedings** — subscribes to `GlobalData.OnPetFed` (dog food from inventory).
- **Distance with pet** — `SessionTracker.AddDistanceWithPetMeters(float)`; called from `PlayerFollow` with the player’s **planar** movement delta while the pet is active.

**Public:** `Instance`, `DistanceWithPetMeters`, `WriteReportIfNeeded()` (used when Editor stop order is unreliable — see `SessionReportPlayModeExit` in Editor).

**Report gate:**

- In **Editor:** `Edit >` menu item / `EditorPrefs` key **`Petropolis_GenerateSessionReports`** (default true) — if false, reports are skipped and a log explains.
- In **builds:** reports always attempt to generate (same `ShouldGenerateReport()` code path returns true outside Editor).

## `SessionChatLog` (`Assets/SCRIPTS/Core/SessionChatLog.cs`)

- **Static list** of `Entry` rows: NPC name, from-player flag, **visible** message text, `GameHour`, sequence, optional `Purchases` list.
- `ChatManager` records every player send and every NPC line (greeting, API, errors) — see [NPCChatAndOpenAI.md](./NPCChatAndOpenAI.md).
- `SessionChatLog.AttachPurchaseToLastNpcMessage` lets trackers attach finalized shop/vet/loan info to the **most recent** NPC line from that NPC (for the HTML transcript).

## `ReportWriter` (`Assets/SCRIPTS/Core/ReportWriter.cs`)

- **Output path:** `Application.dataPath/../Session Reports/SessionReport{nextNumber}.html` (increments from existing `SessionReport*.html` files).
- **Contents (high level):** timestamp (Washington-local style per implementation), player/pet names, session length, distance with pet, money start/end/net/high/low, **Chart.js** (CDN) money series with green/red/gray segments, **multi-line pet stat chart** (health, happiness, energy, hunger), NPC chat as **You:** / **NPC:** transcript, **badges** section with tooltips from `BadgeDefinitions`, **purchases** and **feedings** tables aligned to in-game time.

If tracking never started (no pet confirmed), `WriteReportIfNeeded` logs a **warning** in the Editor and does nothing.

## Editor support

- `Assets/Editor/GenerateSessionReports.cs` (or similarly named) toggles **`Petropolis_GenerateSessionReports`** for turning reports off during quick iteration.

## Related

- [API.md](./API.md) — `GlobalData` events used as inputs.
- [BadgesAndProgression.md](./BadgesAndProgression.md) — badge data embedded in the report.
- [PlayerCharacterAndHUD.md](./PlayerCharacterAndHUD.md) — `PlayerFollow` distance source.

## File reference

- `Assets/SCRIPTS/Core/SessionTracker.cs`, `SessionChatLog.cs`, `ReportWriter.cs`
- `Assets/Editor/` — session report toggle / play-mode exit hook (search for `SessionReport` or `GenerateSessionReports`)
