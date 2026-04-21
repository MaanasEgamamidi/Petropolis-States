# Petropolis

> **FBLA Introduction to Programming 2025-2026**  
> *Build a Virtual Pet*

A third-person 3D life-simulation RPG where players create a character, adopt and care for a dog, manage money, and explore a stylized low-poly city with AI-powered merchant interactions. Built with Unity 6000.3.5f2 LTS and powered by OpenAI for natural-language shop negotiation.

**Team:** Maanas Egamamidi | Dev Agarwal | Harshil Sharma

---

## Table of Contents

- [About](#about)
- [Getting Started](#getting-started)
- [How to Play](#how-to-play)
- [Features](#features)
- [Technical Details](#technical-details)
- [Credits](#credits)
- [License](#license)

---

## About <a id="about"></a>

**Petropolis** is a life-simulation RPG focused on financial literacy, responsible pet ownership, and interactive world systems. Players adopt a dog, manage a budget, care for their pet's needs over time, and interact with AI-driven shopkeepers inside a city sandbox.

### Key Features

- **Character Creation**: Choose gender, head, and body options at the start of the game
- **Pet Care Simulation**: Manage Hunger, Health, Happiness, Energy, and Cleanliness over time
- **AI-Powered Negotiation**: Talk to store owners with OpenAI-backed chat and negotiate prices before buying
- **Economy System**: Start with cash, receive a daily allowance, buy supplies, and track every transaction
- **Time System**: Play through a customizable day/night cycle with an in-game clock and day counter
- **Badge Tracking**: Unlock progression-based badges tied to exploration, care, and interaction
- **Session Reports**: Generate detailed HTML reports with money history, pet stats, chat logs, purchases, and earned badges

### Competition Requirements Met

- [x] Character customization and player naming  
- [x] Virtual pet adoption and care mechanics  
- [x] Time-based pet needs and neglect consequences  
- [x] Budgeting and cost-of-care gameplay  
- [x] Natural-language AI interaction with merchants  
- [x] Session reporting and progress tracking  

---

## Getting Started <a id="getting-started"></a>

### Prerequisites

- **Unity 6000.3.5f2 LTS**
- **Universal Render Pipeline (URP)**
- **OpenAI API Key** (for merchant negotiation)

### Installation

1. Clone the repository:
   ```bash
   git clone https://github.com/MaanasEgamamidi/Petropolis-States.git
   cd Petropolis-States
   ```

2. Open the project in **Unity Hub** with **Unity 6000.3.5f2 LTS**

3. Import the required Unity Asset Store packages listed in the Asset List section

4. Create a `.env` file in the project root and add:
   ```env
   OPENAI_API_KEY=your-key-here
   ```

5. Press **Play** in the Unity Editor

---

## How to Play <a id="how-to-play"></a>

### Getting Started

1. Start from the main menu and create your character by choosing a gender, head, and body
2. Adopt a dog from the pet selection flow and choose one of 6 material skins
3. Enter the world and begin balancing pet care, spending, and exploration
4. Keep your pet healthy while making smart financial choices across each in-game day

### Core Gameplay Loop

- Explore the city, interact with NPCs, and visit important locations
- Monitor your money, your pet's condition, and the in-game clock
- Buy pet supplies and negotiate better prices through AI shop conversations
- Care for your dog regularly so stats do not decay into neglect states
- Continue across multiple in-game days to earn badges and build stronger session results

### Pet Care System

- **Primary stats**: Hunger, Health, Happiness, Energy
- **Additional tracked stat**: Cleanliness
- **Derived health rule**: Health is recalculated from hunger and energy
- Stats decay passively over time, so neglect has visible gameplay consequences
- Your dog follows the player and reacts to care and neglect

### Store Negotiation

- Chat with store owners through an OpenAI-powered merchant system
- Negotiate prices before completing purchases
- Merchant responses follow an inventory-aware persona with pricing rules
- If the API is unavailable, the system falls back to base item prices

### Tips

- Keep money in reserve before buying multiple items at once
- Use negotiation to reduce recurring pet-care costs
- Watch cleanliness as well as the main four bars
- Review your session report after each run to see how your choices affected the outcome

---

## Features <a id="features"></a>

### Character Creation

- Choose gender, head, and body at game start
- Includes 5 head options and 5 body options per gender
- Character selections persist across scenes via shared game state

### Pet System

- Adopt a dog from the in-game pet shop with 6 selectable material skins
- Dog follows the player and reacts to attention or neglect
- Hunger, happiness, energy, cleanliness, and derived health create layered pet-simulation behavior
- Pet care decisions shape moment-to-moment survival, spending, and session outcomes

### AI-Powered Shop Interaction

- Powered by the OpenAI Chat Completions API
- Uses `gpt-4o-mini` by default unless overridden in environment settings
- Players can chat with merchants to negotiate prices before purchasing
- Final agreed prices are parsed and applied to live transactions

### Economy and Time

- Players begin with a starting balance and receive a daily morning allowance
- Purchases update the player's money in real time
- Day length is customizable
- Day/night progression drives the world lighting and time-based gameplay flow

### Badges and Reports

- Badge system includes 8 named badges, with tiered progress for several of them
- Current implemented badge categories include walking distance, park time, pet ownership duration, boundary discovery, and NPC interactions
- Session reports are generated automatically on quit
- Reports are saved as numbered HTML files in `Session Reports/`
- Reports include money history, pet stat history, purchases, feedings, chat logs, distance traveled with your pet, and earned badges

---

## Technical Details <a id="technical-details"></a>

### Project Structure

```text
Petropolis-States/
|-- Assets/
|   |-- Scenes/
|   |   |-- Main Menu.unity
|   |   |-- Instructions.unity
|   |   |-- CharacterChoose.unity
|   |   |-- PetChoose.unity
|   |   |-- World.unity
|   |   `-- Minigames/
|   |-- SCRIPTS/
|   |   |-- Chat/
|   |   |-- Core/
|   |   |-- Menus/
|   |   |-- Minigames/
|   |   |-- NPC/
|   |   |-- Player/
|   |   |-- UI/
|   |   `-- World/
|   |-- AUDIO/
|   |-- Badges/
|   |-- NPC_Personalities/
|   |-- Prefabs/
|   `-- Resources/
|-- Packages/
|-- ProjectSettings/
|-- Session Reports/
|-- .env
`-- README.md
```

### Core Technologies

- **Game Engine**: Unity 6000.3.5f2 LTS
- **Render Pipeline**: Universal Render Pipeline (URP)
- **Language**: C#
- **AI Integration**: OpenAI Chat Completions API
- **Default Model**: `gpt-4o-mini`
- **Report Format**: HTML

### Main Systems

- **Global State**: Character, pet, inventory, and money data persist through `GlobalData`
- **Chat Layer**: OpenAI-backed merchant dialogue lives under `Assets/SCRIPTS/Chat/`
- **World Simulation**: Day/night, minimap, teleport zones, and scene triggers live under `Assets/SCRIPTS/World/`
- **UI Layer**: Pet stat bars, inventory views, notifications, pause flow, and HUD updates live under `Assets/SCRIPTS/UI/`
- **Session Tracking**: Session snapshots and report generation are handled by `SessionTracker` and `ReportWriter`

### Asset List

| Asset | Version | Purpose |
|---|---|---|
| 3D Game Assets Market | 1.0.0 | General 3D game assets |
| Dynamic Day/Night Cycle: Lite | 1.3 | Lighting and skybox system |
| Fruit Market | 3.2 | Store and market environment |
| Furniture FREE - Low Poly 3D Models Pack | 2.1 | Interior props |
| Lean GUI | 2.1.0 | UI animations and toast notifications |
| Low Poly Medieval Market Stalls | 1.0 | Market zone props |
| Low Poly Mega City | 1.02 | World environment |
| Low-Poly Medieval Market | 1.1 | Market zone environment |
| MCP for Unity \| AI Driven Development | 9.0.3 | Editor AI tooling |
| Medieval Greengrocer Stand & Fruits Pack - Low Poly | 1.0 | Grocery store environment |
| Medieval Markets Pack Demo | 1.0 | Additional market assets |
| PolyArt Characters | 1.0 | Player and NPC visuals |
| SimplePoly City - Low Poly Assets | 1.2.3 | Additional city props |
| Small Kit 3D Stylized Petshop Asset | 1.0 | Pet shop interior |
| Starter Assets - ThirdPerson \| URP | 1.1.5 | Player controller base |
| Stylized Dog and Animations | 1.0 | Pet model and animations |
| OpenAI API | `gpt-4o-mini` | Merchant negotiation and chat |

---

## Credits <a id="credits"></a>

### Development Team

- **Maanas Egamamidi** - Lead Developer
- **Dev Agarwal** - Map Designer
- **Harshil Sharma** - UI Designer

### Assets and Services

- Unity Asset Store packages listed in the Asset List section
- OpenAI API for merchant dialogue and price negotiation

---

## License <a id="license"></a>

This project is licensed under the [MIT License](LICENSE).
