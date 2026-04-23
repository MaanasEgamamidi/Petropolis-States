# NPC chat and OpenAI integration

## Overview

World NPCs use a **Unity UI chat** (`ChatManager`) plus optional **OpenAI Chat Completions** (`NpcOpenAiChatBridge`, `OpenAiChatCompletionClient`). Personality and store rules live in **TextAsset** markdown files referenced by `NpcIdentity`. Separate **JSON extraction** passes (`ShopPurchaseTracker`, `VetVisitTracker`, `BankLoanTracker`) turn role-play replies into `GlobalData` transactions without showing raw JSON to the player.

**Configuration:** API keys and model defaults come from a **`.env`** file at the project or build root (`EnvFileLoader`).

---

## Core components

### `NpcIdentity` (`Assets/SCRIPTS/Chat/NpcIdentity.cs`)

- Inspector: **NPC name**, **greeting** (supports `{name}`), and a list of **context files** (TextAssets, usually Markdown under `NPC_Personalities` or similar).
- `BuildSystemPrompt()` concatenates all context files, with substitutions:
  - `{name}` → NPC name
  - `[pet name]` → `GlobalData.PetName` (or a fallback)
  - `[pet type]` → `dog`
- `FormattedGreeting` is shown when the panel opens (first assistant message).

### `ChatManager` (`Assets/SCRIPTS/Chat/ChatManager.cs`)

- Binds input field, send button, scroll area, and bubble prefabs.
- On send: records the player line in `SessionChatLog`, spawns a player bubble, appends to `History`, and invokes `OnUserMessageCommitted` (the bridge starts the API request).
- `ReceiveNpcReply` appends the assistant message; **player-visible** text has **hidden blocks stripped** (see below). Full text (including hidden blocks) stays in history for the model and trackers.
- `SetInteractionLocked` disables input while a request is in flight.
- `OnNpcReplyReceived` fires with the **raw** reply for parsers.

**Hidden block stripping (UI only):** Regex removes `[PRICES]...[/PRICES]` and `[LOAN]...[/LOAN]` (case-insensitive) so offers meta does not show in bubbles. The unstripped string remains in `History`.

### `NpcOpenAiChatBridge` (`Assets/SCRIPTS/Chat/NpcOpenAiChatBridge.cs`)

- Subscribes to `OnUserMessageCommitted`.
- Builds the request from `NpcIdentity.BuildSystemPrompt()` and `ChatManager.History` via `OpenAiChatCompletionClient.SendChatRequest`, then `ReceiveNpcReply` or an error string.

### `OpenAiChatCompletionClient` (`Assets/SCRIPTS/Chat/OpenAiChatCompletionClient.cs`)

- **Coroutines** talking to the Chat Completions HTTP API (UnityWebRequest + Newtonsoft.Json).
- `EnvFileLoader.Get("OPENAI_API_KEY")` is required. Optional: `OPENAI_MODEL` (default `gpt-4o-mini`), `OPENAI_BASE_URL` (default `https://api.openai.com/v1`).
- Folds the first assistant (greeting) into the system message so API roles alternate correctly.
- **`SendSingleTurnRequest`** — used by purchase/vet/loan extractors: system prompt + one user body, for structured JSON only.

### `EnvFileLoader` (`Assets/SCRIPTS/Chat/EnvFileLoader.cs`)

- Loads `<project>/.env` (Editor) or next to the player executable (builds), once.
- `KEY=value` lines; `#` comments; optional quotes.

---

## World flow: opening chat

### `NpcChatZone` (`Assets/SCRIPTS/World/NpcChatZone.cs`)

- Trigger volume: `chatWindowName` matches a **child** under the Chats UI root (e.g. `ChatWindow_PetStore`). Empty name = **hint-only** (e.g. park).
- Overlapping zones use a **stack**; the top of stack determines which window name is active. `CurrentInteractHint` uses the topmost non-empty hint.
- Pattern: **kinematic Rigidbody** on trigger, `isTrigger` on collider, for reliable `CharacterController` overlap.

### `NpcProximityChatOpener` (`Assets/SCRIPTS/World/NpcProximityChatOpener.cs`)

- In the **World** scene only: **C** opens the panel named by `NpcChatZone.GetActiveChatWindowName()`.
- **Escape / B / Back** closes (without stealing letters while typing in the input).
- `IsWorldNpcChatOpen` freezes movement/look like pause (used by `ThirdPersonController`, `PetNeedsSimulator`, etc.).
- Minigame zones can override the hint via `MinigameLauncherZone.CurrentInteractHint` (E to play).

---

## Gameplay trackers (extraction, not display)

| Script | Watches | Applies via |
|--------|---------|-------------|
| `ShopPurchaseTracker` | `OnNpcReplyReceived` | `GlobalData.TryBuyItem` + `SessionChatLog.AttachPurchaseToLastNpcMessage` |
| `VetVisitTracker` | same | `GlobalData.TryCompleteVetVisit` + same attach (visit shown like a “purchase” line) |
| `BankLoanTracker` | same, skips if `HasActiveLoan` | `GlobalData.TryOpenLoan` + attach a synthetic “Loan $…” line |

Each uses **`SendSingleTurnRequest`** with a strict JSON schema in the system prompt, parses with **Newtonsoft.Json.Linq**, and logs to **Console** (`Debug.Log` / `Debug.LogWarning`), not the chat UI.

**Shop / vet:** `AttachPurchaseToLastNpcMessage` links transactions to the **last NPC message** from that NPC for session HTML reports (see [SessionReportsAndTracking.md](./SessionReportsAndTracking.md)).

---

## Related

- [API.md](./API.md) — `GlobalData` economy, inventory, vet visits, loans.
- [SessionReportsAndTracking.md](./SessionReportsAndTracking.md) — `SessionChatLog` in reports.
- `MessageBubbleController` — per-bubble name + body text.
- Greeting and first reply are logged to `SessionChatLog` with `GlobalData.CurrentTimeOfDay` at send time.

## File reference

- `Assets/SCRIPTS/Chat/NpcIdentity.cs`, `ChatManager.cs`, `NpcOpenAiChatBridge.cs`, `OpenAiChatCompletionClient.cs`, `EnvFileLoader.cs`
- `Assets/SCRIPTS/Chat/ShopPurchaseTracker.cs`, `VetVisitTracker.cs`, `BankLoanTracker.cs`
- `Assets/SCRIPTS/World/NpcChatZone.cs`, `NpcProximityChatOpener.cs`
