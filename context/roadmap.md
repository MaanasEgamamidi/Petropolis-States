# Petropolis-States — Implementation Checklist

---

## Phase 1 — Main Menu & Character Setup

### Main menu
- [ ] Create main menu scene
- [ ] Title screen with Play, Options, Quit buttons
- [ ] Transition from main menu into character creation on Play

### Character creation
- [ ] Build character creation UI screen
  - [ ] Gender selection (male / female)
  - [ ] Head selection (5 options per gender, preview in UI)
  - [ ] Body selection (5 options per gender, preview in UI)
  - [ ] Confirm / start game button
- [ ] Create player prefab with modular head and body attachment points
- [ ] In Inspector: expose `maleHeads[]`, `femaleHeads[]`, `maleBodies[]`, `femaleBodies[]` mesh arrays
- [ ] Write script to apply chosen head and body meshes at runtime based on UI selection
- [ ] Persist character choices across scenes (ScriptableObject or PlayerPrefs)

### Player movement & animations
- [ ] Basic movement (walk, run)
- [ ] Set up Animator Controller with idle, walk, run states
- [ ] Hook movement input to animation parameter transitions
- [ ] Camera follow / third-person controller
- [ ] Interact trigger (press key near zones)

---

## Phase 2 — Map Integration & Navigation

- [ ] Import / open map scene
- [ ] Set up zone colliders/triggers on each key location (Home, Parks, Vet, Groomer, Grocery Store, Bank, Job locations)
- [ ] Add named empty GameObjects at each zone entry point (e.g. `Zone_Vet`, `Zone_Park_01`, `Zone_Bank`)
- [ ] Mark player spawn point (`SpawnPoint_Player`)
- [ ] Set up scene transitions if locations span multiple scenes
- [ ] Confirm player can walk around the full map correctly with camera
- [ ] Verify interact trigger fires when player enters each zone

---

## Phase 3 — Pet Shop, Pet Selection & Pet Implementation

### Pet shop & skin selection
- [ ] Build pet store interaction (triggered when player enters pet store zone)
- [ ] Pet store UI:
  - [ ] Display 5 dog skin choices with preview
  - [ ] Confirm selection button
- [ ] Create dog prefab (single mesh + rig)
- [ ] Create 5 material variants (skins)
- [ ] In Inspector: expose `dogSkins[]` material array
- [ ] Write script to apply selected material from array based on UI choice

### Pet movement & following
- [ ] Pet follow AI — dog walks/runs to stay near player
- [ ] Pet Animator Controller: idle, walk, run states
- [ ] Hook pet movement speed to animation parameter transitions
- [ ] Verify all animations play correctly for every skin

### Pet stats system
- [ ] Define dog stats data model: Hunger, Health, Happiness, Energy, Cleanliness, Attachment
- [ ] Implement **Runaway Risk** as a derived stat (formula based on Happiness, Attachment, Health, etc.)
- [ ] Write `PetStatsManager` script — ticks all stats passively over time (decay/gain rates)
- [ ] Always-visible HUD: primary stats (Hunger, Health, Happiness, Energy)
- [ ] Secondary stats panel (Cleanliness, Attachment, Runaway Risk) accessible via menu button

### Pet leveling
- [ ] Define dog level system (XP gained through interactions: walks, feeding, grooming, etc.)
- [ ] Level-up thresholds and XP amounts per interaction type
- [ ] Display dog level and XP progress bar in stats UI
- [ ] Implement level-up event (notification + stat bonuses or unlocks — TBD)

---

## Phase 4 — Zone Interactions & Pet Stats

- [ ] **Park** — Entering park zone + spending time → increases Happiness / Energy
- [ ] **Vet** — Interact at vet zone → pay fee → increases Health
- [ ] **Groomer** — Interact at groomer zone → pay fee → increases Cleanliness
- [ ] **Home / Feeding** — Use food item from inventory → decreases Hunger
- [ ] **General** — All positive interactions increase Attachment and grant dog XP
- [ ] Define stat thresholds at which Runaway Risk increases (e.g. low Happiness + low Attachment)
- [ ] Implement runaway event when Runaway Risk exceeds threshold
- [ ] Notification system: alert player when stats are critically low (e.g. "Your dog is hungry!")

---

## Phase 5 — Day/Night Cycle & Time System

- [ ] Implement game clock (1 in-game day ≈ 5–10 real minutes)
- [ ] Hook day/night cycle visuals to game clock (skybox + directional light rotation/color)
- [ ] Display current in-game time and day in HUD
- [ ] Track current in-game week number
- [ ] Trigger weekly events on week rollover:
  - [ ] Pay out weekly allowance
  - [ ] Check work minimum requirement
  - [ ] Reset weekly work counter

---

## Phase 6 — Banking & Economy Systems

### Currency & allowance
- [ ] Implement currency variable on player data
- [ ] Display current balance in HUD
- [ ] Weekly allowance payout on week rollover

### Store owner negotiation (OpenAI API)
- [ ] Integrate OpenAI API into project (API key management, HTTP client setup)
- [ ] Build store chat UI panel:
  - [ ] Text display for conversation history
  - [ ] Player text input field + send button
  - [ ] Close / exit chat button
- [ ] Write `StoreOwnerNPC` script:
  - [ ] System prompt defines the NPC as a negotiating store owner with base prices for their goods
  - [ ] Player messages are sent to the API; response streamed / displayed in chat UI
  - [ ] NPC can agree to lower a price through negotiation; parse the agreed price from the response
  - [ ] Apply negotiated price to the current transaction
- [ ] Hook store chat UI into each store zone (Grocery, Vet, Groomer)
- [ ] Fallback to base prices if API call fails

### Store / spending
- [ ] Grocery/Food Store UI: item list, base prices, buy button, updates inventory
- [ ] Vet UI: service list, base prices, pay to heal pet
- [ ] Groomer UI: service list, base prices, pay to clean pet
- [ ] Apply negotiated or job-level-discounted price at point of purchase
- [ ] Deduct correct amount from balance on purchase

### Bank
- [ ] Bank UI screen
- [ ] Display balance history as a C# line graph (use appropriate charting library)
- [ ] Implement deposit / withdraw functionality
- [ ] Update bank graph dynamically when balance changes
- [ ] (Optional / time-permitting) Loan system with interest tracking

---

## Phase 7 — Jobs & Player Progression

- [ ] Detect when player enters job zone trigger
- [ ] Implement work session (timed activity → player earns money on completion)
- [ ] Track work sessions completed this in-game week
- [ ] Enforce minimum 1 work session per in-game week (consequence if missed — TBD)
- [ ] Overtime: additional sessions in same week yield bonus money
- [ ] Job level system:
  - [ ] Define job levels (1, 2, 3, …) and XP / hours-worked progression
  - [ ] Store player's current job level
  - [ ] Apply job-level discount multipliers to food, vet, and other care costs at purchase

---

## Phase 8 — Session Report System

> Every time a play session ends (quit or game over), generate a comprehensive report file.

### Data tracking (runs throughout entire session)
- [ ] Create `SessionTracker` singleton that persists across scenes
- [ ] Track and timestamp every economy event:
  - [ ] Money earned (work sessions, allowance, negotiated savings)
  - [ ] Money spent (per store, per item/service, negotiated vs base price)
  - [ ] Balance over time (snapshot every in-game hour)
- [ ] Track all actions taken:
  - [ ] Zone visits (location, time of day, duration)
  - [ ] Store interactions and negotiation outcomes (did player negotiate? final price vs base)
  - [ ] Pet care actions (fed, groomed, vet visits, park visits)
  - [ ] Work sessions (time, earnings, overtime flag)
- [ ] Track pet stat history (snapshot stats every in-game hour)
- [ ] Track dog XP and level-ups

### Report generation
- [ ] On session end, serialize all tracked data to a structured JSON or CSV file
- [ ] File is named with a timestamp (e.g. `session_2026-02-24_1430.json`)
- [ ] Report includes:
  - [ ] Session duration (real time and in-game time)
  - [ ] Economy summary: total earned, total spent, net balance change, breakdown by category
  - [ ] Spending trend graph data (balance over time)
  - [ ] Earning trend graph data
  - [ ] Negotiation summary: how many negotiations attempted, average % saved
  - [ ] Pet care summary: stat averages, number of care actions, dog level at end
  - [ ] Work summary: sessions worked, total earned from work, job level at end
  - [ ] Full action log with timestamps
- [ ] (Optional) In-game end-of-session screen that displays a visual summary before quitting

---

## General UI & HUD

- [ ] Main HUD (always visible): money, in-game time/day, primary pet stats
- [ ] Pause / menu screen
- [ ] Stats detail panel (secondary pet stats + runaway risk + dog level/XP)
- [ ] Consistent UI style across all screens
