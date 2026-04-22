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
- [Dependencies and libraries](#dependencies-and-libraries)
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
| Medieval Greengrocer Stand & Fruits Pack - Low Poly | 1.0 | Grocery store environment |
| Medieval Markets Pack Demo | 1.0 | Additional market assets |
| PolyArt Characters | 1.0 | Player and NPC visuals |
| SimplePoly City - Low Poly Assets | 1.2.3 | Additional city props |
| Small Kit 3D Stylized Petshop Asset | 1.0 | Pet shop interior |
| Starter Assets - ThirdPerson \| URP | 1.1.5 | Player controller base |
| Stylized Dog and Animations | 1.0 | Pet model and animations |
| OpenAI API | `gpt-4o-mini` | Merchant negotiation and chat |

For a full inventory of UPM packages, editor plugins, and web tooling used in the repository, see [Dependencies and libraries](#dependencies-and-libraries).

---

## Dependencies and libraries <a id="dependencies-and-libraries"></a>

This section lists third-party and Unity-supplied **packages**, **editor-local plugins** shipped with the project, **web dependencies** for the `codeflow` presentation tool, and **imported art/content** under `Assets/`. Versions match `Packages/manifest.json` and embedded `package.json` files as of the last README update.

### Unity Package Manager (UPM)

**Rendering and visuals**

| Package | Version |
| --- | --- |
| `com.unity.render-pipelines.core` | 17.3.0 |
| `com.unity.render-pipelines.universal` | 17.3.0 |
| `com.unity.render-pipelines.universal-config` | 17.0.3 |
| `com.unity.shadergraph` | 17.3.0 |
| `com.unity.postprocessing` | 3.5.1 |
| `com.unity.2d.sprite` | 1.0.0 |

**Systems and gameplay**

| Package | Version |
| --- | --- |
| `com.unity.inputsystem` | 1.17.0 |
| `com.unity.cinemachine` | 2.10.5 |
| `com.unity.ai.navigation` | 2.0.9 |
| `com.unity.timeline` | 1.8.10 |
| `com.unity.ugui` | 2.0.0 |
| `com.unity.visualscripting` | 1.9.9 |

**Performance and data**

| Package | Version |
| --- | --- |
| `com.unity.burst` | 1.8.27 |
| `com.unity.mathematics` | 1.3.3 |
| `com.unity.collections` | 2.6.2 |

**Editor, tooling, and collaboration**

| Package | Version |
| --- | --- |
| `com.unity.ide.rider` | 3.0.38 |
| `com.unity.ide.visualstudio` | 2.0.26 |
| `com.unity.collab-proxy` | 2.11.2 |
| `com.unity.searcher` | 4.9.4 |
| `com.unity.multiplayer.center` | 1.0.1 |

**Testing**

| Package | Version |
| --- | --- |
| `com.unity.test-framework` | 1.6.0 |
| `com.unity.test-framework.performance` | 3.2.0 |
| `com.unity.ext.nunit` | 2.0.5 |

**Dependencies via Unity**

| Package | Version |
| --- | --- |
| `com.unity.nuget.newtonsoft-json` | 3.2.2 |
| `com.unity.nuget.mono-cecil` | 1.11.6 |

**Built-in `com.unity.modules.*` packages** (1.0.0 in manifest)

`accessibility`, `adaptiveperformance`, `ai`, `androidjni`, `animation`, `assetbundle`, `audio`, `cloth`, `director`, `hierarchycore`, `imageconversion`, `imgui`, `jsonserialize`, `particlesystem`, `physics`, `physics2d`, `screencapture`, `subsystems`, `terrain`, `terrainphysics`, `tilemap`, `ui`, `uielements`, `umbra`, `unityanalytics`, `unitywebrequest`, `unitywebrequestassetbundle`, `unitywebrequestaudio`, `unitywebrequesttexture`, `unitywebrequestwww`, `vectorgraphics`, `vehicles`, `video`, `vr`, `wind`, `xr`

### Local Unity packages (vendored under `Assets/Plugins/CW/`)

| Package | Version | Location |
| --- | --- | --- |
| Common (`com.cwilkes.common`) | 1.0.0 | `Assets/Plugins/CW/Shared/Common` |
| Lean Common (`com.cwilkes.lean.common`) | 1.0.0 | `Assets/Plugins/CW/LeanCommon` |
| Lean Transition (`com.cwilkes.lean.transition`) | 1.0.0 | `Assets/Plugins/CW/LeanTransition` |
| Lean GUI (`com.cwilkes.lean.gui`) | 1.0.0 | `Assets/Plugins/CW/LeanGUI` (depends on Lean Common and Lean Transition) |

Author: [Carlos Wilkes / CW](https://carloswilkes.com).

### `codeflow/codeflow.html` (web)

| Resource | Version / source |
| --- | --- |
| Google Fonts (Inter, JetBrains Mono) | Loaded from `fonts.googleapis.com` |
| Monaco Editor (VS Code editor in the browser) | 0.44.0 via `cdnjs.cloudflare.com` (loader and `vs` path) |

The `script/script.html` helper page does not use external script or font CDNs.

### Other Unity-bundled or stock imports in `Assets/`

- **TextMesh Pro** — standard TMP import under `Assets/TextMesh Pro`.
- **Starter Assets** — `Assets/StarterAssets` (Unity [Companion License](https://unity3d.com/legal/licenses/Unity_Companion_License)).
- **Tutorial / readme scaffolding** — `Assets/TutorialInfo` (URP project readme pattern).

### Third-party art and content (folder inventory)

The following top-level `Assets/` content folders (and nested packs) are third-party or marketplace-style **art and prefabs**, not UPM packages: `3D_Game_Assets_Market`, `Bublisher`, `Fries and Seagull`, `Fruit Market`, `ithappy`, `itsMakingThings`, `JC_LP_MegaCity`, `Low-Poly Medieval Market`, `MedievalMarketDemo`, `My Universe Studio`, `NPC_Personalities`, `PolyArt`, `PolyRonin`, `SimplePoly City - Low Poly Assets`, `Sprout Lands - UI Pack`, `StarterAssets`, and other pack folders such as `Dog` and shared environment bundles used by scenes. Specific credits and store links may appear in each pack’s `readme` / `read_me` files.

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
