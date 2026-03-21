# Notification system — implementation reference

This document captures design notes for Petropolis-States (Unity) so you can implement HUD/toast notifications later without re-deriving the details. It covers **general Unity UI patterns**, **LeanGUI `LeanPulse`** (as used in the bundled example), and **tradeoffs** between approaches.

---

## Goals (what you want)

- Show a message on screen (e.g. after spawn: “Go to the pet shop”, movement hints).
- Trigger from **code** (not only from a button).
- Support **event-based** triggers (on spawn, quest step, etc.) and optionally **time-based** repeating reminders.
- Optional: box **grows with text**; **center** or **top** placement; **auto-hide** after N seconds.
- Optional: isolated setup (dedicated scene or prefab) so gameplay scenes stay clean.

---

## Two main architecture options

### A. Prefab + manager (recommended default)

- Build a **Canvas** (usually **Screen Space – Overlay**) with your notification panel as a **child hierarchy**.
- Save that hierarchy as a **prefab**; place **one instance** in your main game scene (e.g. `World`) or under a **DontDestroyOnLoad** bootstrap object.
- Expose something like `NotificationService.Show(string message, float durationSeconds)` that:
  - Sets **TextMeshPro** (or UI Text) content.
  - Rebuilds layout if the box should resize (see [Dynamic layout](#dynamic-layout-text-that-expands)).
  - Shows the panel (and optional fade/slide).
  - Waits `duration`, then hides (coroutine, `async`, or Lean Transition).

**Pros:** Fast, no extra scene loads, easy to call from anywhere.  
**Cons:** Not literally a separate scene (still easy to edit in a dedicated empty scene).

### B. Additive UI scene

- Small scene (e.g. `Notifications.unity`) with **Canvas + panel** only.
- Load with `SceneManager.LoadSceneAsync("Notifications", LoadSceneMode.Additive)` when the game is ready.
- Register a controller in that scene with a static/singleton service so gameplay code can call `Show(...)`.
- On hide, optionally `UnloadSceneAsync` if you want the scene fully unloaded.

**Pros:** Very separated in the editor; can share across projects.  
**Cons:** Scene load overhead; avoid **duplicate EventSystems** (only one active EventSystem); think about **canvas sort order** if you stack overlays.

**Screen space note:** Overlay UI is not “above the camera” in 3D — it draws in **screen space**. You **animate** `RectTransform` / `CanvasGroup` so the panel **looks** like it slides from off-screen.

---

## Dynamic layout (text that expands)

Typical stack:

- **Background** (`Image`, 9-slice) under a **Vertical Layout Group** (or horizontal + vertical).
- **TMP** with wrapping on; constrain width with **Layout Element** max width or anchored width so lines wrap instead of running off-screen.
- **Content Size Fitter** on the text (or wrapper): vertical = **Preferred Size**, as needed horizontally.

After setting `text.text = message`:

- Call `Canvas.ForceUpdateCanvases()`.
- Often `LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform)` on the layout root so size updates **this frame** before show/animate.

---

## Auto-hide after N seconds

- **Coroutine:** `yield return new WaitForSeconds(duration);` then hide/disable.
- **Async:** `await Task.Delay(...)` with a **CancellationToken** if you need to cancel when a new notification replaces the old one.
- **Policy when overlapping:** simplest is **replace** (cancel previous timer, new message) or **queue** one next — decide explicitly.

---

## LeanGUI: `LeanPulse` (example: `06 Notification.unity`)

Path in this repo: `Assets/Plugins/CW/LeanGUI/Examples/06 Notification.unity`.

### What `LeanPulse` actually does

`LeanPulse` is **not** a full “toast with message and duration” system by itself. It:

- Runs **Lean Transition** animations via **`Pulse Transitions`** (`LeanPlayer`) when a **pulse** happens.
- Can invoke **`On Pulse`** (`UnityEvent`).
- Optionally pulses on a **time interval** (or manual-only if interval is **-1**).

The “slides in / hides after a while” behavior in the demo is authored in **Lean Transition** on the linked objects, not as a single “seconds on screen” field on `LeanPulse`.

### Button vs code

The example uses a **button** for convenience. You do **not** need a button: any script can trigger the same pulse (e.g. in `Start` when the player spawns).

### Time-based vs event-based

- **Event-based:** Call pulse from code when something happens.
- **Repeating time-based:** If **`Time Interval`** ≠ **-1**, `LeanPulse` counts down and calls **`TryPulse`** on that interval (see `UpdatePulse` in `LeanPulse.cs`).
- **Manual-only:** Set **`Time Interval`** to **-1`** so pulses only happen when you invoke them.

### `TryPulse` vs `Pulse` vs `PulseAll`

- **`Pulse()`** — Forces a pulse **now** (runs transitions / events). Use this when you want “show whenever I call it.”
- **`TryPulse()`** — Respects **`Remaining Pulses`**. If remaining is **0**, it may **refuse** to pulse. Use when you intentionally limit how many times it can fire.
- **`LeanPulse.PulseAll(string name)`** — Calls **`Pulse()`** on every **enabled** `LeanPulse` whose **GameObject `name`** matches the string **exactly**.

**Doc quirk:** The example scene’s banner text mentions `LeanPulse.Pulse("Name")`, but in the current plugin code the static API is **`PulseAll`**, not `Pulse`.

### Customization

- **Font, colors, sprites:** Normal Unity UI / TMP on the objects under your notification hierarchy.
- **Motion and how long it stays visible:** Mostly **Lean Transition** (delays, position/alpha tweens) wired under **`Pulse Transitions`**.

### Prefab workflow (no need to remake the example scene)

1. Open `06 Notification.unity` (or your own UI scene).
2. Select the **Canvas** subtree for the notification (e.g. object named like **`Notification (LeanPulse)`** and children).
3. **Create prefab** → place one instance in **World** (or persistent UI root).

### Dynamic messages (“Go to the pet shop”)

`LeanPulse` does **not** set text. Before calling **`Pulse()`** / **`PulseAll`**:

- Assign the **TMP/Text** string in code, rebuild layout if needed, **then** pulse.

Or wrap everything in your own **`Show(string message, float duration)`** that updates text and either:

- Calls **`Pulse()`** if transitions handle hide timing, or  
- Drives **CanvasGroup** / **RectTransform** yourself for per-message duration (often simpler if every toast needs a different lifetime without authoring new transitions each time).

### When LeanPulse is enough vs when to add a small script

- **LeanPulse + transitions:** Great when you have a **fixed** visual recipe and mostly fixed timing authored in the transition graph.
- **Thin C# wrapper:** Better when you need **per-call duration**, **queues**, or **changing copy** every time without touching the transition asset.

---

## Calling from gameplay (conceptual API)

```csharp
// Example shapes — not project code until you implement them.

// Option 1: Your own service (recommended for variable messages/durations)
// NotificationService.Show("Visit the pet shop", durationSeconds: 4f);

// Option 2: LeanPulse by GameObject name (exact match)
// Lean.Gui.LeanPulse.PulseAll("ToastNotification");

// Option 3: Serialized reference
// [SerializeField] Lean.Gui.LeanPulse toast;
// toast.Pulse();
```

---

## Related assets in this repo

| Item | Location |
|------|----------|
| LeanPulse script | `Assets/Plugins/CW/LeanGUI/Required/Scripts/LeanPulse.cs` |
| Notification example scene | `Assets/Plugins/CW/LeanGUI/Examples/06 Notification.unity` |
| Other LeanGUI examples (popups, modals) | `Assets/Plugins/CW/LeanGUI/Examples/` |

---

## Checklist before shipping

- [ ] Only one **EventSystem** in play (if using additive UI scene, disable or don’t duplicate).
- [ ] Canvas **sort order** / **Sort Order** correct relative to other overlays.
- [ ] Safe area / notches (if mobile): consider **anchor to top** with offset, or a safe-area helper.
- [ ] Decide **queue vs replace** when multiple notifications fire quickly.
- [ ] If using **`PulseAll`**, GameObject names are **unique** enough to avoid accidental double-firing.

---

*Last updated: reference for future implementation; not a substitute for reading `LeanPulse.cs` and your chosen transition setup when you build this.*
