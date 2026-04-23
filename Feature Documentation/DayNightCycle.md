# Day / night cycle

## Overview

The visual and logical day/night loop combines:

1. **`DayNightCycle`** — third-party `itsmakingthings_daynightcycle` component that advances time, drives sun/moon, blends lighting, and fires Unity events.
2. **`DayNightController`** — Petropolis glue: 12-hour UI, 80/20 day-vs-night speed, `GlobalData` time sync, daily allowance, and optional street/building lights.

## DayNightCycle (`Assets/itsMakingThings/DynamicDayNightCycle/Scripts/DayNightCycle.cs`)

### Time model

- Internal `_timeOfDay` is **0–24** hours (fractional, e.g. 7.5 = 7:30 AM). Exposed as `TimeOfDay`.
- `cycleSpeed` is added each frame: `_timeOfDay += cycleSpeed * Time.deltaTime`, wrapping at 24.
- **Day** is **6:00–18:00**; **night** is the rest. Crossing those boundaries toggles `OnDayStarted` (6:00) and `OnNightStarted` (18:00).
- If `OnDayStarted` / `OnNightStarted` are wired, **daybreak = 6:00** and **sunset = 18:00** for event purposes (names match tooltips on the component).

### Lighting and environment

- A **rotation pivot** tilts the sun; sun/moon and scene camera are updated from time.
- **Four anchor presets** (daybreak, midday, sunset, night) each hold ambient, sun, background, fog, and optional water color. The active segment is chosen by hour block (0–6, 6–12, 12–18, 18–24) and **lerped** between endpoints.
- Optional **fog** and **water** color updates; can be turned off for manual control.
- `timeText` (TextMeshPro) shows **24-hour `HH:MM`**, updated when the in-game **minute** changes.

### Public controls

- `StopTime` / `StartTime` — pause or resume the cycle.
- `ResetTimeSmoothly(float)` — coroutine blend to a target hour.
- `SetTimeInstantly`, `SetToDaybreak` (6), `SetToMidday` (12), `SetToSunset` (18), `SetToNight` (0).

Namespace: `itsmakingthings_daynightcycle`.

## DayNightController (`Assets/SCRIPTS/World/DayNightController.cs`)

### Execution and scene setup

- `DefaultExecutionOrder(-50)` so it can set `dayNightCycle.startTimeOfDay` and rendering defaults before `DayNightCycle.Start`.
- Forces `RenderSettings.ambientMode` to **Flat** and, when a camera is assigned, **Solid color** clear flags so sky color from the cycle is visible.
- Resets the sun’s local rotation to identity if needed so the pivot drives the arc.

### 80% day / 20% night (real time)

- `fullCycleMinutes` (default from `DayNightSettings`) defines how long one **in-game 24h** takes in real time.
- **Day** (6–18 in-game) uses `baseDaySpeed` so 12 in-game hours consume **80%** of that real-time window.
- **Night** (18–6) multiplies that speed by **4** so 12 in-game night hours consume **20%** of the same real-time window.
- `DayNightSettings.GetFullCycleMinutes` can override the scene default with a **PlayerPrefs** value from the main menu.

### Gameplay hooks

- **Daily morning allowance** — on `OnDayStarted`, adds `dailyMorningAllowance` to `GlobalData.PlayerMoney` (configurable; not duplicated just for starting mid-day; see field tooltip in code).
- **Hourly `GlobalData` sync** — when the **hour** index changes, calls `GlobalData.SetTimeOfDay(timeOfDay)` for `CurrentTimeOfDay`.
- **UI** — if `timeDisplayText` is set, shows **12-hour** text like `7 AM` / `6 PM` (replaces the built-in 24h TMP on `DayNightCycle` for that slot when you use this field).
- **Lights** — optional `SimpleLightController`: `TurnLightsOff` on day, `TurnLightsOn` at night, with an initial state from current time.

## DayNightSettings (`Assets/SCRIPTS/Core/DayNightSettings.cs`)

- `PrefsKey` = `Petropolis_FullCycleMinutes` — saved full-cycle length in **minutes** (clamped, default 15).
- `GetFullCycleMinutes(sceneDefault)` / `SetFullCycleMinutes` / `GetMenuDisplayMinutes` for menus and scene defaults.

## Related systems

- **`LoanDayTicker`** subscribes to `DayNightCycle.OnDayStarted` and calls `GlobalData.AdvanceOneDay` for loan term countdown (see `GlobalData` loan docs).
- **Pet / simulation** code can read `GlobalData.CurrentTimeOfDay` (updated on hour changes) or query `dayNightCycle.TimeOfDay` directly for continuous time.

## File reference

- `Assets/itsMakingThings/DynamicDayNightCycle/Scripts/DayNightCycle.cs`
- `Assets/SCRIPTS/World/DayNightController.cs`
- `Assets/SCRIPTS/Core/DayNightSettings.cs`
- `Assets/SCRIPTS/Core/LoanDayTicker.cs`
