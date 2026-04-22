# Petropolis — spoken script only

Replace “Person 2” and “Person 3” with your teammates’ names if you want.

---

person1: Good morning, judges. We’re Harshil Sharma, Maanas Egamamidi, and Dev Agarwal. We’re presenting Petropolis, a 3D life-simulation RPG built in Unity for the Build a Virtual Pet topic. Players name and customize themselves and their dog, care for it over time, and pay real costs — food, supplies, vet care — using an in-game economy. They can also earn money through minigames to cover those costs. We designed it as a modular program with clear data flow, validation, and a session report judges can analyze. I’ll hand the controls to Person 2 for the live demo, while Person 3 connects what you see to our code.

---

person2: I’m choosing my look and name — now we’re in the open world.

person3: Customization writes to GlobalData.SaveChoice so the next scene reads one consistent player profile. We also mirror indices in PlayerPrefs for persistence. That separation — static session data plus disk prefs — is the kind of structure the rubric calls Data storage and Data and Logic.

---

person2: I’m adopting my dog and choosing how it looks.

person3: Adoption calls GlobalData.SavePetChoice, which clamps the skin index and fires OnPetConfirmed. The world scene reads that static state so art and UI stay in sync — that’s modular coupling: one source of truth for pet identity.

---

person2: Hunger and happiness dropped over time — I’m feeding and playing to stabilize stats, and my cash is dropping when I buy care.

person3: Stats live on GlobalData — hunger, health, happiness, energy, cleanliness — updated by care actions and time. Purchases and feedings are logged into lists on SessionTracker for the report. That hits running totals and appropriate data structures for the topic.

---

person2: I’m playing a quick minigame job to earn extra money before I go shopping for my pet.

person3: Zones use MinigameLauncherZone: when you’re inside the trigger and press E or the controller’s west face button, we call MinigameSession.StartSession with a scene name like ReactionTime, Pong, or FlappyBird under Assets/Scenes/Minigames/. MinigameSession additively loads that scene, freezes the World’s root GameObjects so cameras, audio, and input don’t conflict, then activates the minigame scene — the same additive pattern we use for PetChoose. Each game calls MinigameSession.AddPayout as you score: ReactionTimeGame pays from reaction speed, PongGameManager pays per successful hit until a miss, FlappyBirdGameManager pays per beam cleared. When the run ends, MinigameSession.EndSession rounds PendingEarnings and credits GlobalData.PlayerMoney, unloads the minigame, and unfreezes the World. That’s the prompt’s optional earning system — tasks to earn pet-care funds — implemented as clean, reusable session logic.

---

person2: I’m negotiating in plain English — the shopkeeper just agreed to a final price.

person3: The NPC uses NpcOpenAiChatBridge to call OpenAiChatCompletionClient with the conversation history. If the API errors, the player sees an error string instead of a fake reply — that’s honest fallback behavior for unreliable conference Wi-Fi. After each assistant message, ShopPurchaseTracker asks the model for strict JSON extraction of finalized items only; we only debit cash when the model confirms a completed sale. ChatManager strips hidden price and loan blocks from the visible transcript. Together that’s input validation on a semantic level plus clean separation of concerns.

---

person2: I’m taking a short loan so we can show interest and repayment pressure.

person3: Negotiated loans are extracted by BankLoanTracker, then GlobalData.TryOpenLoan validates principal, interest rate, and term against min and max constants. Amount owed is Round(principal times (1 plus interestRate)) — simple interest packaged as one payoff total. Each in-game day ticks down with LoanDayTicker; default triggers a fail state — that’s consequence-based financial literacy tied to the topic.

---

person2: Here’s everything we did — money over time, pet stats, and what I bought.

person3: SessionTracker samples money and pet stats on a schedule and on quit. ReportWriter.Generate writes UTF-8 HTML with Chart.js: judges can analyze trends without re-running the game. That directly targets Program generates a presentable report at the Exceeds level — error-free summaries for analysis.

---

person1: To summarize: Petropolis meets the prompt with customization, care gameplay, emotional feedback through stats, tracked spending, minigames to earn pet-care money, optional loans, AI-assisted shopping, and an HTML session report. Our codebase is split into readable modules with validation and documentation, including a README, full source, and attributions for Unity packages and external APIs. Thank you for your time — we’re happy to answer your questions.
