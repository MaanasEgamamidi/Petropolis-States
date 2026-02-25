# Petropolis-States — Structured Roadmap

## 1. Locations & World

- **Home** — Player and pet home base
- **Parks** — 2–3 park zones where pets can go
- **Vet** — Heal zones
- **Groomer** — Clean zones
- **Grocery / Food store** — Store UI when visiting to add items
- **Bank** — Bank money UI; includes C# graphs (appropriate libraries) and updating UI
- **Job locations** — Places where the player spends time working

---

## 2. Time & Jobs

- **Day length** — 1 in-game day ≈ 5–10 minutes; use a day/night cycle
- **Work requirement** — Player must work at least once per in-game week
- **Overtime** — Extra work time → more money

---

## 3. Pet (Dog)

### Skins & visuals
- **5 material skins** — Player chooses dog skin at the pet store (UI)
- Same dog mesh and animations for all skins; only the material/skin changes
- **Inspector setup** — Drag materials into an array; the correct one is applied based on the UI choice

### Stats
- **Primary stats:** Hunger, Health, Happiness, Energy, Cleanliness, Attachment
- **Derived:** Runaway risk (affected by certain stats)
- **Display:** Main stats always visible; others available via UI menu or elsewhere

### Behavior
- Certain stats influence how much the pet wants to run away

---

## 4. Economy

- **Cost of care** — Money is spent on pet care (food, vet, groomer, etc.)
- **Bank system** — Possibly loans and interest; may be simplified depending on time
- **Weekly allowance** — Player receives a regular allowance
- **Job-level perks** — Prices scale with job level (e.g. level 2: X% cheaper food, X% cheaper vet, etc.)

---

## 5. Person (Player character)

- **Customization** — 5 heads and 5 bodies, for both male and female
- **UI** — Similar flow to dog skin selection, but shown at game start (character creation)
- **Inspector setup** — Drag head meshes and body meshes into arrays (e.g. male heads, female heads, male bodies, female bodies); the correct head and body are applied based on the character-creation UI choice
