# 🐾 Petropolis

> A life-simulation RPG built in Unity where you manage your finances, care for your dog, and navigate the responsibilities of everyday life.

---

## Overview

**Petropolis** is a third-person 3D life simulation game in which the player adopts a dog, holds a job, manages a bank account, and balances the real costs of pet ownership — all within a stylized low-poly city. The game uses an in-game economy, time system, and AI-driven NPC interactions to create a meaningful simulation of financial literacy and emotional care.

---

## Features

### 🧍 Character Creation
- Choose gender, head, and body at game start
- 5 head options and 5 body options per gender
- Selections persist across scenes via ScriptableObject or PlayerPrefs

### 🐕 Pet System
- Adopt a dog from the in-game pet shop with 6 selectable material skins
- Dog follows the player and reacts to care and neglect
- **Primary stats:** Hunger, Health, Happiness, Energy
- **Secondary stats:** Cleanliness, Attachment
- **Derived stat:** Runaway Risk (calculated from primary and secondary stats)
- Stats decay passively over time — neglect has consequences
- Dog gains XP through interactions (feeding, walking, grooming) and levels up

### 💬 AI-Powered Store Negotiation
- Powered by **OpenAI API** (gpt-4o-mini)
- Players can chat with store owners to negotiate prices before purchasing
- NPC maintains a merchant persona and inventory with base prices
- Agreed prices are parsed and applied to the current transaction
- Falls back to base price if the API is unavailable

### 💰 Economy
- Daily allowance paid each in-game day
- Optional bank loans with configurable interest
- All transactions are tracked for end-of-session reporting

### 🕐 Time System
- In-game day length is customizable
- Day/Night cycle drives skybox and lighting
- In-game clock and day counter visible in HUD

### 📊 Session Reports
- Generated automatically on quit or game over
- Saved as a timestamped JSON file (e.g. `session_2026-04-06_1430.json`)
- Includes: economy summary, pet stat history, negotiation outcomes, action log, and dog level

---

## Asset List

| Asset | Version | Purpose |
|---|---|---|
| 3D Game Assets Market | 1.0.0 | General 3D game assets |
| Dynamic Day/Night Cycle: Lite | 1.3 | Lighting and skybox system |
| Fruit Market | 3.2 | Store/market environment |
| Furniture FREE – Low Poly 3D Models Pack | 2.1 | Interior props |
| Lean GUI | 2.1.0 | UI animations and toast notifications |
| Low Poly Medieval Market Stalls | 1.0 | Market zone props |
| Low Poly Mega City | 1.02 | World environment |
| Low-Poly Medieval Market | 1.1 | Market zone environment |
| MCP for Unity \| AI Driven Development | 9.0.3 | Editor AI tooling |
| Medieval Greengrocer Stand & Fruits Pack – Low Poly | 1.0 | Grocery store environment |
| Medieval Markets Pack Demo | 1.0 | Additional market assets |
| PolyArt Characters | 1.0 | Player and NPC visuals |
| SimplePoly City – Low Poly Assets | 1.2.3 | Additional city props |
| Small Kit 3D Stylized Petshop Asset | 1.0 | Pet shop interior |
| Starter Assets – ThirdPerson \| URP | 1.1.5 | Player controller base |
| Stylized Dog and Animations | 1.0 | Pet model and animations |
| OpenAI API | gpt-4o-mini | Store owner NPC negotiation |

---

## Unity Version

- **Unity:** 6000.3.5f2 LTS
- **Render Pipeline:** Universal Render Pipeline (URP)

---

## Getting Started

### Prerequisites

- **Unity 6000.3.5f2 LTS**
- **OpenAI API Key** (for store owner NPC negotiation)

### Installation

1. Clone the repository:
   ```bash
   git clone https://github.com/your-username/Petropolis-States.git
   cd Petropolis-States
   ```

2. Open the project in **Unity Hub** — click **Add**, select the cloned folder, and open it with Unity 6000.3.5f2 LTS

3. Import all required assets from the Unity Asset Store (see Asset List above)

4. Create a `.env` file in the project root and add your OpenAI API key: `OPENAI_API_KEY=your-key-here`

5. Press **Play** in the Unity Editor

---

## Credits

| Name | Role |
|---|---|
| Maanas Egamamidi | Lead Developer |
| Dev Agarwal | Map Designer |
| Harshil Sharma | UI Designer |

---

## License

This project is licensed under the [MIT License](LICENSE).
