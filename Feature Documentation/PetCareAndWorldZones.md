# Pet care simulation and world zones

## Overview

**Passive decay** of pet needs runs in **`PetNeedsSimulator`**. **Location-based** effects use trigger zones: **`HomeZone`**, **`ParkZone`**, and **`BedZone`**. The shared **`GameOverFlow`** ends the run with a black fade to **`ClosingScene`** for neglect, loan seizure, or legacy starvation.

---

## `PetNeedsSimulator` (`Assets/SCRIPTS/Core/PetNeedsSimulator.cs`)

- **When:** `RuntimeInitializeOnLoadMethod` after scene load: if a `DayNightController` exists in the scene, adds this component to that GameObject; otherwise creates a **`DontDestroyOnLoad`** object.
- **Runs only** if the player has a pet (`GlobalData.StoredPetSkin > 0`).
- **Paused** while `NpcProximityChatOpener.IsWorldNpcChatOpen` (see [NPCChatAndOpenAI.md](./NPCChatAndOpenAI.md)) so needs do not tick during NPC chat.
- **Update:** decays `PetHappiness`, `PetHunger`, `PetEnergy`, `PetCleanliness` (each has its own per-second rate in the Inspector). Calls `GlobalData.RecomputeHealth()`.
- **Energy** decay is multiplied by time of day: **lower at night** (configurable `nightStartHour` / `nightEndHour`), **slightly higher in afternoon** window.
- **LateUpdate:** recomputes health; if `GlobalData.IsNeglectDeathConditionMet()` (two or more care stats effectively zero), calls `GameOverFlow.TriggerPetNeglect` — unless `GameOverFlow.IsTriggered` already.

`DefaultExecutionOrder(-100)` runs before most gameplay.

---

## `HomeZone` (`Assets/SCRIPTS/World/HomeZone.cs`)

- While the player (with a pet) stays inside the trigger: **`PetEnergy`** increases per real-time second, clamped 0–100, then `RecomputeHealth()`.
- Same Rigidbody + trigger pattern as other zones (see [WorldTravelAndInteraction.md](./WorldTravelAndInteraction.md)).

## `ParkZone` (`Assets/SCRIPTS/World/ParkZone.cs`)

- While inside with a pet: **happiness** up, **energy** and **hunger** down per second (tunable). Logs periodic “inside park” lines for debugging.
- **`BadgeManager.AddParkGameHours`** — accumulates in-game hour deltas while in the park for the **Park Pro** badge (see [BadgesAndProgression.md](./BadgesAndProgression.md)).

## `BedZone` (`Assets/SCRIPTS/World/BedZone.cs`)

- **Interact (E / gamepad X)** while in zone, **with a pet**, during **night window** (default 9 PM–6 AM): full-screen **fade**, snap **`DayNightCycle`** to **6:00 AM** (`SetTimeInstantly` or smooth reset per setup), optional **`PetEnergy`** boost.
- Uses scene **`FadeCanvas` / `Fade`** if not assigned in Inspector.
- Respects `WorldPauseController` and NPC chat focus the same way as other interactables.

---

## `GameOverFlow` (`Assets/SCRIPTS/Core/GameOverFlow.cs`)

- **Static** guard: one trigger per session (`IsTriggered`).
- **Reasons:** `PetStarved`, `PetNeglect` (2+ empty care stats), `PetTakenByBank` (loan default).
- Creates a full-screen black panel and loads **`ClosingScene`** after a fade. **`LastReason`** is read in closing UI.
- **`ResetForNewSession`** clears the trigger flag (e.g. return to main menu) while **`LastReason`** may remain for the closing scene.

`PetNeedsSimulator` uses **neglect**; `LoanDayTicker` / loan logic wires **bank**; any legacy “starvation” path may still call `TriggerStarvation`.

---

## Related

- [API.md](./API.md) — `GlobalData` pet stats, `TryUseItem`, loans.
- [DayNightCycle.md](./DayNightCycle.md) — in-game time driving decay multipliers and bed sleep.

## File reference

- `Assets/SCRIPTS/Core/PetNeedsSimulator.cs`, `GameOverFlow.cs`
- `Assets/SCRIPTS/World/HomeZone.cs`, `ParkZone.cs`, `BedZone.cs`
