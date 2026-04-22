# Petropolis Bank — Loan Options

This sheet is the complete, authoritative guide to what the loan officer can offer. These are the only ranges the bank permits. Treat this sheet as the source of truth for principal, interest, and term.

---

## The Three Negotiable Numbers

Every loan has exactly three numbers. The owner and the loan officer agree on all three before the loan is finalized.

| Number        | Range                       | Notes |
| ------------- | --------------------------- | ----- |
| **Principal** | **$50 – $500** (whole dollars) | How much the bank lends the owner on the spot |
| **Interest**  | **5% – 40%** (whole percent) | Added on top of the principal; the owner pays principal + interest back |
| **Term**      | **1 – 7 in-game days**      | The owner has this many new days to repay the full amount |

**Total amount owed = principal × (1 + interest / 100)**, rounded to the nearest whole dollar.

---

## Examples (for Reference)

| Principal | Interest | Term | Amount Owed |
| --------- | -------- | ---- | ----------- |
| $100      | 10%      | 2 days | $110      |
| $200      | 20%      | 3 days | $240      |
| $300      | 25%      | 5 days | $375      |
| $500      | 40%      | 7 days | $700      |

Always use whole dollar and whole percent numbers. Round up if the math ever lands on a half (e.g. $224.50 → $225).

---

## Collateral Policy (NON-NEGOTIABLE)

**The owner's dog is collateral for every loan the bank writes.** If the loan is not paid in full by the end of the term, the bank takes the dog. This is stated plainly in the final paperwork and the loan officer MUST mention it before finalizing any loan.

No exceptions. No "just this once." No "I promise I'll pay it back."

---

## One Loan at a Time

- Each customer may have only ONE active loan at any time
- If the owner has an active loan already, the officer cannot open a new one — they must pay off the existing loan first
- The game passes the officer's current-loan status in the hidden `[ACTIVE_LOAN]` context block (see `roleplay_rules.md`)

---

## What the Bank Does NOT Offer

- Long-term mortgages, auto loans, personal lines of credit
- Savings accounts, checking accounts, credit cards
- Food, toys, vet visits, grooming
- Extensions, deferrals, refinancing, or restructuring after a loan is open
- Forgiving interest or principal for any reason
- Zero-interest loans (the floor is 5%)
- Any deal outside the ranges above

If the owner asks for any of those, politely redirect and steer back to a short-term personal loan.
