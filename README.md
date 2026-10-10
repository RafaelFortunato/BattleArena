# Battle Arena

A top-down survivors-style arena game built in Unity 6 with the Universal Render Pipeline, targeting WebGL. You play a wizard husky holding off ten minutes of ever-growing monster waves with spells, taking on missions as you go, then facing the boss that ends the match.

![Gameplay](docs/screenshot.png)

---

## Play It

Play it directly in browser on itch.io
https://rafaelpaz.itch.io/battlearena

---

### Controls

| Action | Keyboard | Gamepad |
|---|---|---|
| Move | **WASD** / **Arrow keys** | Left stick |
| Cast spells | **Q** / **E** / **R** | **X** / **Y** / **B** (Square / Triangle / Circle) |
| Interact (chests, healing wells) | **F** | **A** |
| Pause | **Esc** / **Enter** | **Start** |
| Choose a card | **A** / **D** or **Arrow keys**, **F** to confirm | D-pad, **A** to confirm |

## Gameplay

Survive for **10 minutes**, then defeat the boss. Your staff fires magic bolts at the nearest enemy on its own, so the run is about where you stand and when you cast. Fallen enemies drop gems. Each level-up offers a choice of cards: new spells, spell upgrades or stat boosts. Spell slots open at **levels 5, 10 and 15**, and the level cap is **40**.

The arena fills up over time. Treasure chests appear 25 seconds in, then every 40 seconds, and hold stat cards. Healing wells restore half your health and take a minute to recharge. Every 35–55 seconds a mission starts. Completing it earns spell upgrade cards, so missions are the fastest way to power up your spells.

### Spells

| Spell | Cooldown | Damage (share of Attack) | Effect |
|---|---|---|---|
| **Fireball** | 4s | 375% | Explodes on the nearest enemy |
| **Arcane Lance** | 3s | 450% per enemy | Long-range lance that pierces everything in its path |
| **Frost Nova** | 8s | 300% | Burst around you that slows every enemy hit to 40% speed for 3s |
| **Meteor Shower** | 20s | 750% per meteor | Six meteors rain down on nearby enemies |
| **Chain Lightning** | 5s | 350% to the first target | Jumps to up to 6 enemies, weaker each jump |
| **Arcane Orbit** | 12s | 175% per hit | Three orbs circle you for 6s |
| **Toxic Miasma** | 10s | 100% every 0.5s | Poison cloud that damages and slows for 5s |
| **Phase Step** | 6s | — | Blink forward and become immune to damage for 2s |

Spell upgrades add **Extra Charge**, **Quick Recharge**, **Empowered** damage, **Split Cast** extra projectiles, a **Lingering** duration, an **Amplified** area or **Arc Jump** extra bounces. Not every upgrade suits every spell. Stat cards come in **Common**, **Rare** and **Epic**, and the **Luck** stat makes the rarer ones more likely.

### Enemies

| Enemy | Health | Speed | Appears |
|---|---|---|---|
| **Goblin Grunt** | 22 | 3 | 0:00 |
| **Giant Slime** (splits into Slimelings) | 54 | 2.1 | 0:00 |
| **Ogre Brute** | 96 | 4 | 1:15 |
| **Goblin Archer** | 48 | 3 | 2:30 |
| **Goblin Bomber** | 66 | 2.9 | 2:30 |
| **Dark Shaman** | 84 | 2.5 | 2:30 |
| **Swift Imp** | 72 | 5.2 | 4:30 |
| **Swarm Bat** | 54 | 6 | 4:30 |
| **Bomb Beetle** | 60 | 4.4 | 4:30 |
| **Boar Charger** | 264 | 3.3 | 6:30 |
| **Storm Wyvern** | 192 | 9.5 | 6:30 |
| **Arcane Wisp** | 168 | 4.2 | 6:30 |
| **Obsidrax, Dread of the Graveyard** (boss) | 24,000 | 6 | 10:00 |

From 8:30 only the two strongest tiers spawn. The **Treasure Bunny** and the **Ogre Warlord** don't spawn in waves. They only show up as mission targets.

### Missions

| Mission | Goal | Time limit |
|---|---|---|
| **Treasure Bunny** | Catch the bunny before it escapes | 60s |
| **Warlord Hunt** | Defeat the Ogre Warlord | 50s |
| **Hold the Circle** | Stand in the circle for 12s | 40s |
| **Blood Ritual** | Defeat 12 enemies inside the circle (+1.5 per minute played) | 60s |
| **Arcane Shards** | Collect 3 shards scattered across the arena | 45s |
| **Massacre** | Defeat 15 enemies (+2 per minute played) | 35s |
| **Untouchable** | Lose less than 10% of your max health | 20s |

---

## Technical Highlights

**Content built from code.** One editor command, *BattleArena → Build Game Content*, generates the Arena scene, every prefab, material, sound event and data asset from the imported art. Re-running it updates assets in place and keeps their GUIDs, so balance changes are a code edit and a rebuild. The tables above all come from `Assets/Game/Scripts/Editor/ContentBuilder`.

**Data-driven design.** Spells, spell upgrades, stat cards, enemies, the spawn schedule, missions and sound events are ScriptableObjects. Spells share one `SpellDefinition` base, and each one lists which upgrades it supports.

**Composable enemies.** Enemy behaviour is built from swappable movement and attack strategy components: chasing, flying, orbiting, zig-zagging, keeping distance or fleeing, paired with contact hits, dashes, projectiles, lobbed bombs, self-destructs or telegraphed ground strikes. A central `EnemyDirector` ticks every enemy from one update instead of hundreds of `Update` calls.

**Zero-allocation combat loop.** Enemies, projectiles, effects, gems and health bars are pooled through an `IPoolable` interface. Gameplay events travel through a type-safe `EventBus<T>` of structs, and damage numbers reuse a fixed set of TextMeshPro labels. A busy late-game fight produces no per-hit garbage.

**Game feel.** Hit-stop on heavy blows, Cinemachine screen shake that fades with distance, damage flashes, a frost fresnel shader on slowed enemies, and a procedural tail sway on the husky that follows its movement and turns.

**Tuned for WebGL.** Health bars billboard in a custom shader and draw without per-bar canvases. The battle theme is cut into a seamless bar-aligned loop, and the UI is built with UI Toolkit custom elements, styled once and reused across the HUD, menus and cards.

---

## Built With

| Technology | Use |
|---|---|
| **Unity 6** (6000.5.2f1) | Engine |
| **Universal Render Pipeline** (17.5) | Rendering, targeting WebGL |
| **Unity Input System** (1.19) | Keyboard and gamepad input, with prompts that follow the active device |
| **UI Toolkit** | HUD, menus and level-up cards, built from custom visual elements |
| **TextMesh Pro** | Floating damage numbers |
| **Cinemachine** (3.1) | Follow camera and screen shake |
| **AI Navigation** (2.0) | NavMesh baking and ground enemy pathfinding |
| **Unity AI Assistant** / **Unity Pipeline** | Editor automation bridge used during development |
| **Blender** | Scripted environment models; rigging and animation of the player character |
| **ComfyUI** | Stable Audio 3 for sound effects, Z-Image Turbo for rune circles |
| **Claude Code** | AI pair programming: gameplay systems, content builder, art and audio pipelines, tooling and code cleanup |

---

## Asset Credits

The game's code is original and was mostly written by Claude Code. Art and audio came from:

| Source | Used for |
|---|---|
| **Quaternius — Ultimate Monsters** (CC0) | Enemies and the boss |
| **Tripo** | The wizard husky mesh, rigged and animated in Blender (source in `SourceArt/WizardHusky`) |
| **Blender** (scripted) | Trees, boulders, headstones, Celtic crosses, walls and other graveyard props |
| **Procedural textures** (Python / Blender) | Ground and VFX textures |
| **Generated with Z-Image Turbo** (ComfyUI) | Spell rune circles |
| **Hand-written SVG** | UI icons |
| **Claude Design** | UI design |
| **Generated with Stable Audio 3** (ComfyUI) | Sound effects |
| **Alexandr Zhelanov** — *Battle Theme 4* ([SoundCloud](https://soundcloud.com/alexandr-zhelanov), CC-BY 4.0) | Battle music |
| **Emma_MA** — *Determined Pursuit* (OpenGameArt, CC0) | Boss music |
| **Baloo 2** and **Nunito** (SIL Open Font License) | Fonts |

Created by Rafael Paz.
