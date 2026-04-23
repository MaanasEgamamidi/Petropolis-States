# World UI panels (inventory, loans, feed, pet stats)

These scripts run in the **World** scene and cooperate with **`GameplayInput`** and **`WorldPauseController`**.

## Inventory — `InventoryPanelController`

**`Assets/SCRIPTS/UI/InventoryPanelController.cs`**

- **G** (keyboard) or **D-pad Left / Start** (gamepad) toggles the panel — see `GameplayInput.InventoryTogglePressedThisFrame()`.
- **Static `IsOpen`** — other systems gate input when the inventory is up (e.g. teleport menu).
- On open: shows cursor (Starter Assets / shared menu cursor restore pattern), **hides** optional HUD references (minimap, money, day–night, pet stats) and saves their previous active state; restores on close.
- **`InventoryRowView`** rows are spawned once per `GlobalData.Inventory` key; **`OnInventoryChanged`** refreshes quantities.
- Uses **`StarterAssetsInputs`** to zero player move/look while open (same as other full-screen world menus).

## Loans — `LoansPanelController`

**`Assets/SCRIPTS/UI/LoansPanelController.cs`**

- **L** or **RT** toggles — `GameplayInput.LoanPanelTogglePressedThisFrame()`.
- **`IsOpen`** static for gating.
- Reads **`GlobalData`** loan fields; **`OnLoanChanged`** / **`OnMoneyChanged`** update labels; **Pay off** calls `GlobalData.TryRepayLoan()`.
- Same “hide other HUD” pattern as the inventory.

## Feeding — `PetFeedController`

**`Assets/SCRIPTS/UI/PetFeedController.cs`**

- **F** or **D-pad Down** — `GlobalData.TryFeedDogFromInventory()` (normal food first, then enhanced).
- If no food: shows **`OutOfFood`** `NotificationController` if assigned.
- Skips when paused, NPC world chat is open, or a TMP input is focused.

## Pet statistics slide-out — `PetStatisticsPanelController`

**`Assets/SCRIPTS/UI/PetStatisticsPanelController.cs`**

- **`PetInfoSettings.Mode`**: **Always on**, **Toggle**, or **Hold to show** (persisted; bound in main menu and pause — see [PlayerPreferencesAndSettingsUI.md](./PlayerPreferencesAndSettingsUI.md)).
- **Q** and **D-pad Right** open/slide the **left** stats box (read script for full handling and intro peek in hold mode).
- Animates **drag handle** vs **stats** opacity with separate easing exponents.

## Money display

**`MoneyCounterDisplay`** (`Assets/SCRIPTS/UI/MoneyCounterDisplay.cs`) — subscribes to `GlobalData.OnMoneyChanged` for live text updates (details in source).

## Row binding — `InventoryRowView`

**`Assets/SCRIPTS/UI/InventoryRowView.cs`**

- One item name, **Use** button → `GlobalData.TryUseItem` (applies `ItemEffect`, updates quantity).
- Shows list price and formatted effect from `GlobalData`.

## Related

- [API.md](./API.md) — inventory, loans, `TryUseItem`.
- [GameplayInputReference.md](./GameplayInputReference.md) — keys.
- [PlayerCharacterAndHUD.md](./PlayerCharacterAndHUD.md) — HUD overview.

## File reference

- `Assets/SCRIPTS/UI/InventoryPanelController.cs`, `InventoryRowView.cs`, `LoansPanelController.cs`, `PetFeedController.cs`, `PetStatisticsPanelController.cs`, `MoneyCounterDisplay.cs`
