# Game scripting API (GlobalData and related)

## Overview

Petropolis does not expose a public HTTP API in the game runtime. The **scripting API** for cross-scene state, economy, inventory, and progression is centered on the static members of **`GlobalData`** in `Assets/SCRIPTS/Core/GlobalData.cs`, plus a few small helper types (`DayNightSettings`, etc.).

Use these from C# scripts and UI. Prefer **subscribing to events** when you need to react to money, inventory, or loan changes.

---

## GlobalData — identity and flow

| Member | Description |
|--------|-------------|
| `StoredGender`, `StoredBody`, `StoredHead`, `StoredName` | After character creation; set via `SaveChoice`. |
| `StoredPetSkin` | Pet skin 1–6; set via `SavePetChoice`. |
| `PetChooseOpenedFromWorld`, `HasOpenedPetChooseFlowFromWorld` | Additive pet shop flow from the world. |
| `SaveChoice(gender, body, head, name)` | Persists character; fires `OnCharacterConfirmed`. |
| `SavePetChoice(petSkin)` | Persists pet; fires `OnPetConfirmed`. |
| `HasCharacterChoice()` | Whether a character was confirmed. |
| `MarkPetChooseFlowStartedFromWorld()` | When opening pet choose from world. |
| `PrintStoredChoice()` | Debug: logs stored character once. |

**Events:** `OnCharacterConfirmed`, `OnPetConfirmed`

---

## Time

| Member | Description |
|--------|-------------|
| `CurrentTimeOfDay` | 0–24, fractional. Updated by `DayNightController` when the **in-game hour** changes (not every frame). |
| `SetTimeOfDay(float)` | Intended to be called from `DayNightController` when the hour changes. |

For continuous time, read `DayNightController`’s `DayNightCycle.TimeOfDay` instead.

**Related:** `DayNightSettings` — PlayerPrefs for full-day length in real minutes; see [DayNightCycle.md](./DayNightCycle.md).

---

## Economy and pet stats

| Member | Description |
|--------|-------------|
| `PlayerMoney` | Cash; changing value fires `OnMoneyChanged` with **delta** (positive = gain). |
| `StartingPlayerMoney` | Default starting cash constant. |
| `PetName`, `PetHunger`, `PetHealth`, `PetEnergy`, `PetHappiness`, `PetCleanliness` | Care and display stats (0–100 typical range). |
| `VitalDepletedEpsilon` | Threshold for “empty” care stats. |
| `CountDepletedCareStats()` | How many of hunger/energy/happiness/cleanliness are depleted. |
| `IsNeglectDeathConditionMet()` | True if at least two care stats are depleted (game-over condition). |
| `RecomputeHealth()` | Sets `PetHealth` to average of `PetEnergy` and `PetHunger`. |

**Events:** `OnMoneyChanged` — `(float delta)`

---

## Inventory and items

- **`Inventory`**: `Dictionary<string, float[]>` — per item: `[0]` list price, `[1]` **remaining uses** (not packs).
- Constants: `InventoryPriceIndex`, `InventoryQuantityIndex`, item name strings (`NormalDogFoodItemName`, etc.), and `ItemUsesPerPack` (uses added per pack purchase).
- **`ItemEffects`**: per-use `ItemEffect` (stat + amount); `TryUseItem` applies and decrements a use.

| Method | Description |
|--------|-------------|
| `TryBuyItem(itemName, quantity, pricePerUnit)` | Buy **packs**; adds `quantity × uses per pack` to uses. |
| `GetItemQuantity(itemName)` | Remaining **uses** (rounded). |
| `HasAnyDogFoodInInventory()` | Normal or enhanced food available. |
| `TryUseItem(itemName)` | One use; applies effect; for dog food also boosts energy; `RecomputeHealth`. |
| `TryFeedDogFromInventory()` | Uses normal food first, then enhanced. |

**Events:** `OnItemPurchased` — `(itemName, packQuantity, pricePerUnit)`; `OnInventoryChanged`; `OnPetFed` — `(foodItemName)`.

**Enum:** `PetStat` — `Hunger`, `Energy`, `Happiness`, `Cleanliness` for item effects.

---

## Vet visits

- **`VetVisits`**: `Dictionary<string, VetVisit>` — base price and health boost for each visit name (Basic / Wellness / Full).
- `TryCompleteVetVisit(visitName, agreedPrice)` — charges player, applies boost to energy and hunger, recomputes health, fires `OnVetVisitCompleted` with `(name, price, healthDelta)`.

**Events:** `OnVetVisitCompleted`

---

## Bank loans

| Field / property | Description |
|------------------|-------------|
| `HasActiveLoan`, `LoanPrincipal`, `LoanInterestRate`, `LoanTermDays`, `LoanDaysRemaining`, `LoanAmountOwed` | Current loan state (amount owed includes interest, rounded). |
| Constants | Min/max principal, interest, term; see source for exact ranges. |

| Method | Description |
|--------|-------------|
| `TryOpenLoan(principal, interestRate, termDays)` | Credits principal; sets repay amount; fails if a loan already exists or values invalid. |
| `TryRepayLoan()` | Full repayment if `PlayerMoney` ≥ `LoanAmountOwed`. |
| `AdvanceOneDay()` | Decrements `LoanDaysRemaining`; may fire `OnLoanDefaulted` at 0 with balance. |
| `ForceDefault()` | Test/cheat: trigger default without clearing state. |

**Events:** `OnLoanOpened`, `OnLoanRepaid`, `OnLoanChanged`, `OnLoanDefaulted` (signatures in source).

In-game day ticks for the loan are driven from **`DayNightCycle.OnDayStarted`** via `LoanDayTicker` → `AdvanceOneDay`.

---

## File reference

- Primary: `Assets/SCRIPTS/Core/GlobalData.cs`
- Day/night prefs: `Assets/SCRIPTS/Core/DayNightSettings.cs`
- Loan bridge: `Assets/SCRIPTS/Core/LoanDayTicker.cs`

---

## Editor / tooling note

The repository includes **MCP for Unity** under `Assets/MCPForUnity` for editor automation; that is separate from in-game `GlobalData` and is not required for normal gameplay code.
