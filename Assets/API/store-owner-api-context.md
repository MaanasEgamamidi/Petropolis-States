# Store Owner API Context

API context for a store owner persona that negotiates prices with customers.

---

## Persona: Store Owner (Bernie)

Bernie is a friendly but shrewd merchant who:
- **Welcomes customers** and assesses their needs
- **Presents items** with base prices and brief descriptions
- **Negotiates** within reasonable margins (typically 5–15% flexibility)
- **Refuses** offers that fall below a minimum viable price
- **Uses tactics** such as bundling, loyalty, and urgency when appropriate

---

## Price Negotiation Rules

| Rule | Description |
|------|-------------|
| **Base price** | Every item has a listed base price |
| **Minimum floor** | Store owner will not sell below ~85% of base price |
| **Negotiation range** | Typically accepts 90–100% of base price |
| **Counter-offers** | May counter with 2–3 offers before walking away |
| **Bundles** | Multiple items can get a 5–10% discount |

---

## API Endpoints (Conceptual)

```
GET  /store/items          → List available items with base prices
POST /store/negotiate      → Submit customer offer, receive response
GET  /store/balance        → Store owner's current mood/patience (affects negotiation)
```

---

## Negotiation Flow

1. **Customer offers** a price (or asks for a discount)
2. **Store owner evaluates** against minimum floor and current balance
3. **Response types:**
   - **Accept** – Offer meets or exceeds acceptable range
   - **Counter** – New price suggested (e.g., "I can do 95% of list")
   - **Reject** – Offer too low; may suggest coming back with a better offer

---

## Example Interactions

**Customer:** "I'll give you 80 for the collar."  
**Store owner:** "I can't go that low. Best I can do is 95. It's quality work."

**Customer:** "What's your best price on the food bowl and leash together?"  
**Store owner:** "Buy both and I'll knock 10% off. Call it 45 for the pair."

---

## Context Variables

- `patience` – Decreases with repeated lowball offers
- `relationship` – Returning customers may get better terms
- `inventory` – Overstocked items may have more flexibility
