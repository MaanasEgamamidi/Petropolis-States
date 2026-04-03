# Petropolis-States — Detailed backlog

Each item below has a **goal**, **acceptance criteria** (must be true to mark done), and **notes** where helpful.

---

## 1. Pet stats UI — polish

**Goal:** Primary and secondary pet stats are easy to read at a glance, match the game’s visual direction, and feel finished (spacing, typography, icons or bars, feedback).

**Acceptance criteria**
- [ ] HUD shows primary stats (Hunger, Health, Happiness, Energy) with clear labels and current values (numeric and/or bar).
- [ ] Secondary stats (Cleanliness, Attachment, Runaway Risk) are reachable from one obvious control (e.g. menu button or collapsible panel) without cluttering the main HUD.
- [ ] Stat changes are visible within ~1s of the underlying value changing (bar tween, flash, or short pulse — not silent updates).
- [ ] Layout works at the project’s target resolution(s); no overlapping text or clipped controls in normal play.
- [ ] Colors/icons distinguish stat types; critical thresholds (e.g. low health) use a distinct visual treatment.

**Notes:** Align with “General UI & HUD” and Phase 3 pet stats in `roadmap.md`.

---

## 2. Pet stats — decay over time

**Goal:** Stats drift when the player is not actively caring for the pet, so neglect has consequences.

**Acceptance criteria**
- [ ] Passive tick applies to relevant stats (per design: e.g. Hunger increases, Energy decreases over in-game time) at defined rates.
- [ ] Decay/gain is deterministic or seeded consistently (no random drift unless specified).
- [ ] Rates are configurable (constants, ScriptableObject, or inspector fields) without code edits for tuning.
- [ ] Decay respects pause / menu state if the game should freeze simulation while paused (define behavior and implement consistently).
- [ ] `PetStatsManager` (or equivalent) owns tick logic; no duplicate competing timers.

**Notes:** Roadmap already mentions passive ticks; this item is “fully wired and tunable.”

---

## 3. Game over / pet death when stats are critically low

**Goal:** Extreme neglect or harm ends the run with a clear failure state.

**Acceptance criteria**
- [ ] Documented thresholds per stat (or composite rule) that trigger failure (e.g. Health ≤ 0, Hunger maxed for too long — **exact rules are a design choice; must be explicit in code/data**).
- [ ] On trigger: gameplay input stops or transitions to a dedicated Game Over flow; pet cannot recover without restart/load.
- [ ] Player sees a clear message explaining *why* the run ended (which condition fired).
- [ ] Session end hooks fire (e.g. session report, save flags) same as quit where applicable.
- [ ] No soft-lock: UI remains navigable (retry / main menu / quit).

**Notes:** Coordinate with runaway events if both exist; avoid two conflicting “lose” systems.

---

## 4. Areas — non-default zone effects

**Goal:** Entering a zone changes stat or economy behavior beyond a single global default.

**Acceptance criteria**
- [ ] Each major zone (Home, Parks, Vet, Groomer, Grocery, Bank, jobs, etc.) has an explicit **effect profile** (data or script): which stats are influenced, direction, and magnitude or curve.
- [ ] “Base” behavior is defined once; zones **override or add** to base (documented: additive vs multiplicative).
- [ ] Enter/exit or dwell-time rules are consistent (e.g. on trigger enter, on interact, or timed while inside).
- [ ] Effects are testable in isolation (debug toggle or unit-style test for at least one zone).

**Notes:** Extends Phase 2 zone triggers + Phase 4 interactions.

---

## 5. Special areas — full functionality

**Goal:** Zones that are not “walk and passive buff” have real interactions (UI, costs, outcomes).

**Acceptance criteria**
- [ ] List of “special” zones is explicit (e.g. Bank, stores, job sites, Vet, Groomer).
- [ ] Each special zone: player can complete at least one **meaningful loop** (open UI → action → state change → feedback).
- [ ] Costs, inventory, and pet stats update correctly after each action; failures (insufficient funds) are communicated.
- [ ] Zone-specific rules from the roadmap are implemented or explicitly deferred with a ticket.

**Notes:** Overlaps grocery/vet/groomer/bank/job flows; criteria here are “no empty shells.”

---

## 6. NPCs — exclude player’s chosen character

**Goal:** World-populating NPCs never use the same head/body combo (or same identity) as the player’s current character.

**Acceptance criteria**
- [ ] On spawn or pool fetch, NPC appearance is chosen from a set that **excludes** the player’s selected head/body indices (or equivalent IDs).
- [ ] If the pool is exhausted (small asset set), behavior is defined (e.g. allow duplicate only after all others used, or log warning) — **no silent duplicate with player**.
- [ ] Changing character mid-session (if supported) updates the exclusion list for new spawns.

---

## 7. NPCs — spawn inside buildings

**Goal:** NPCs appear at plausible indoor anchor points, not only outdoors.

**Acceptance criteria**
- [ ] Defined spawn points or volumes per building/interior (Transforms, spawn volumes, or tile data).
- [ ] NPCs instantiated at those points do not intersect geometry (basic ground/placement validation or hand-placed safe points).
- [ ] Spawn density or count rules exist (max per building, cooldown) to avoid clutter.
- [ ] Entering/exiting buildings does not duplicate or orphan NPCs (cleanup or persistent world rules documented).

---

## 8. NPCs — animation

**Goal:** Non-player characters play idle/walk (and any required) animation states so the world feels alive.

**Acceptance criteria**
- [ ] NPCs use an Animator (or equivalent) with at least **idle** and **locomotion** states when moving.
- [ ] Movement speed matches animation parameters (no sliding feet beyond acceptable threshold).
- [ ] When NPC is stationary, idle plays; when pathing, walk/move plays.
- [ ] Performance: reasonable upper bound on simultaneous animated NPCs for target hardware (pooling or limits if needed).

---

## 9. Pet actions

**Goal:** The pet can perform discrete actions (eat, play, tricks, react) tied to stats, context, or player command.

**Acceptance criteria**
- [ ] At least **N** concrete actions implemented (define N ≥ 3 in implementation: e.g. feed from inventory, play at park, respond to pet).
- [ ] Each action has: trigger condition, animation or VFX hook, effect on one or more stats, and failure feedback if conditions not met.
- [ ] Actions are listed in UI or discoverable (tutorial prompt or action menu) — not hidden debug-only.
- [ ] Cooldowns or limits exist if needed to prevent stat exploits.

**Notes:** Overlaps pet leveling XP sources; keep one source of truth for “what grants XP.”

---

## 10. Pet levels

**Goal:** Pet gains XP, levels up, and progression is visible and rewarding.

**Acceptance criteria**
- [ ] XP sources defined (which actions grant how much); data-driven preferred.
- [ ] Level-up thresholds table or curve exists; level cannot decrease unless by design.
- [ ] UI shows current level and progress toward next level (bar or numeric).
- [ ] Level-up event fires: notification, optional stat bonus or unlock, logged for session report.
- [ ] Max level behavior defined (cap XP, prestige, or flat rewards).

**Notes:** Matches Phase 3 “Pet leveling” in roadmap.

---

## 11. Achievements

**Goal:** Optional goals reward exploration and care; state persists across sessions if the game persists saves.

**Acceptance criteria**
- [ ] Achievement definitions live in data (id, title, description, unlock condition).
- [ ] Unlocking shows in-game feedback (toast, modal, or HUD badge).
- [ ] Progress achievements track partial completion where applicable.
- [ ] Save/load or PlayerPrefs stores unlocked set; new game resets appropriately.
- [ ] At least one achievement per category you care about (economy, pet care, zones, story) — **minimum set agreed with design**.

---

## 12. Notifications — on area enter

**Goal:** Entering a zone gives contextual, non-spammy feedback.

**Acceptance criteria**
- [ ] First enter per session (or per in-game day) can show a short line of context (“You entered the park.” / zone flavor text).
- [ ] Repeat visits: either no toast, or a quieter rule (cooldown per zone).
- [ ] Does not stack multiple full-screen blockers; compatible with walking gameplay.
- [ ] Localizable strings if project uses localization pipeline.

---

## 13. Alerts — important events

**Goal:** Critical situations (low stats, runaway risk, unpaid loan, job deadline) reach the player clearly.

**Acceptance criteria**
- [ ] List of alert conditions is explicit in code/data (e.g. Hunger above threshold, Runaway Risk above X%).
- [ ] Each alert has severity (info / warning / critical) and channel (HUD icon, modal, sound — per design).
- [ ] Cooldowns or deduplication prevent the same alert every frame.
- [ ] Player can acknowledge or dismiss where appropriate without losing the underlying problem (state remains until fixed).

**Notes:** Roadmap mentions critical stat alerts; align with Phase 4.

---

## 14. Buy items from store

**Goal:** Grocery (and similar) shop: browse, pay, receive items into inventory.

**Acceptance criteria**
- [ ] Item catalog with base prices and names displayed in UI.
- [ ] Purchase deducts currency atomically (no double-spend on rapid clicks).
- [ ] Insufficient funds blocks purchase with clear message.
- [ ] Inventory updates; pet consumables can be used from inventory per feeding flow.
- [ ] Purchases feed economy/session tracking for reports.

**Notes:** Vet/groomer as “services” can share the same payment pattern with different effect hooks.

---

## 15. OpenAI API — store negotiation

**Goal:** Store owner chat negotiates price; agreed price applies to the current transaction; graceful degradation without API.

**Acceptance criteria**
- [ ] API key loaded from secure config (not hardcoded); errors handled.
- [ ] Chat UI: history, input, send, close; system prompt defines merchant persona and inventory/base prices.
- [ ] Parsed outcome updates transaction price (or rejects invalid parses with fallback).
- [ ] Fallback to base price if API fails, times out, or returns unusable content.
- [ ] Rate limiting / user feedback on wait states (spinner, disabled send).

**Notes:** Matches Phase 6 roadmap; hook per store zone as specified there.

---

## 16. Loans — principal, interest, repayment

**Goal:** Player can borrow, accrues interest over in-game time, and repayment is enforced or has consequences.

**Acceptance criteria**
- [ ] Loan offer UI: principal, interest rate, term or minimum payment rules, total due over time **shown before confirm**.
- [ ] Balance and interest accrue on a defined schedule (per in-game day/week).
- [ ] Payments reduce principal/interest correctly; early repayment behavior defined.
- [ ] Default/missed payment: defined consequence (penalty, game flag, or alerts) — **must be implemented, not “TBD” in code**.
- [ ] Loan state appears in HUD/bank UI and in session report summary.

**Notes:** Roadmap lists optional loan; this checklist treats it as full feature when tackled.

---

## 17. Session report — actions, prices, chat, etc.

**Goal:** End-of-session artifact includes rich detail: actions, economic breakdown, negotiation/chat logs, and pet progression.

**Acceptance criteria**
- [ ] Report file (JSON/CSV per roadmap) includes: timestamped action list, money in/out with categories, balance snapshots.
- [ ] Per purchase: item/service id, base price, final price, negotiation flag.
- [ ] Store chat transcripts or summarized negotiation outcomes (privacy: no API keys in file).
- [ ] Pet: final stats, level, XP, notable events (level-ups, runaway warnings).
- [ ] File naming includes session end timestamp; format documented for parsers.

**Notes:** Aligns with Phase 8 `SessionTracker` scope.

---

## 18. Pause menu — in game

**Goal:** Player can pause, adjust options or return to menu without corrupting simulation state.

**Acceptance criteria**
- [ ] Single dedicated input opens pause from gameplay (and closes or stacks consistently with other UIs).
- [ ] While paused: game simulation stops **or** audio/UI behavior matches documented design (roadmap: time may still run — pick one and implement consistently).
- [ ] Resume returns to gameplay with correct state; no duplicate pause menus.
- [ ] Entries: Resume, Options (if exists), Return to main menu / Quit — actual set matches project menu flow.
- [ ] Pause is suppressed where inappropriate (e.g. main menu, loading) if applicable.

---

## 19. Consistent UI

**Goal:** Fonts, colors, button styles, panels, and navigation patterns are unified across HUD, shops, bank, pause, and character/pet setup.

**Acceptance criteria**
- [ ] Shared UI prefab or style sheet (Unity UI Toolkit theme, prefab variants, or documented color/spacing tokens).
- [ ] Primary/secondary text styles reused; no one-off fonts per screen without reason.
- [ ] Buttons, sliders, and lists share hover/disabled visuals.
- [ ] Modal vs toast vs HUD layering order documented and consistent (no shop under pause accidentally).
- [ ] Short style guide in repo **optional**; **in-product consistency** is the acceptance bar.

---

## Quick reference — suggested priority tiers (optional)

| Tier | Items |
|------|--------|
| Core loop | 2, 4, 5, 14, 18 |
| Stakes | 3, 13 |
| Polish & feel | 1, 8, 12, 19 |
| Depth | 9, 10, 11, 15, 16, 17 |
| World | 6, 7, 15–17 (negotiation/report tie to economy) |

Reorder tiers to match your milestone planning.
