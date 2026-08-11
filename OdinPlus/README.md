# OdinPlus

A Valheim mod that adds NPC villages, factions, quests, blueprints, and a builder system. Villages with faction-aligned NPCs spawn at world locations across all biomes. Players earn reputation, complete quests, scan structures into blueprints, and watch Builder NPCs construct them piece by piece.

## Requirements

- Valheim 0.221.12+
- BepInEx 5.4.23.3 (BepInExPack Valheim 5.4.2333)

## Installation

1. Install [BepInEx](https://valheim.thunderstore.io/package/denikson/BepInExPack_Valheim/) if you haven't already
2. Copy `OdinPlus.dll` to `BepInEx/plugins/`
3. Launch the game once - config files auto-generate in `BepInEx/config/`

Both server and all clients need OdinPlus installed for multiplayer.

### Files Created on First Run

```
BepInEx/config/faction_config.yaml        Factions, reputation, village locations
BepInEx/config/faction_quests.yaml        Quest definitions
BepInEx/config/blueprints/                Scanned blueprint YAML files (folder)
```

---

## Features

### NPC Villages

Villages spawn automatically at world locations (ruins, runestones, camps, crypts, houses) across every biome. Each village contains:

- **Guards** - Patrol and protect the village (60m radius)
- **Messenger** - Gives message delivery quests with map markers
- **Supplier** - Accepts item donations for Odin Credits and reputation
- **Builder** - Constructs blueprints when given materials, piece by piece
- **Priest** - Trains player skills for 100 gold per level

Each village is randomly assigned a faction at spawn time. All NPCs within a village share that faction. Factions persist across saves.

### Faction System

Per-player reputation tracked from -100 to +100. Press **F7** to view standings.

| Tier | Range | Behavior |
|------|-------|----------|
| Hostile | < -30 | NPCs attack on sight |
| Unfriendly | -30 to -10 | NPCs refuse interaction |
| Neutral | -10 to +10 | Normal interactions |
| Friendly | +10 to +30 | Positive interactions |
| Honored | > +30 | Best rewards |

Reputation changes:

| Action | Change |
|--------|--------|
| Kill NPC | -50 |
| Damage NPC | -10 per hit |
| Donate items to Supplier | +15 |
| Complete quest | +35 |
| Train with Priest | +5 |

Cascade effects: Helping a faction boosts its allies (+delta/3) and hurts its enemies (-delta/2).

### Quests

**Munin** at Odin's camp offers randomized quests (Dungeon, Hunt, Treasure, Search).

**Messenger NPCs** in villages offer delivery quests - deliver a message to a marked NPC. Map pin shows the target location. Completing delivery gives credits and reputation.

### Blueprint System

Scan player-built structures, then Builder NPCs reconstruct them.

**Visual selection:**
1. Console (F5): `selectblueprint MyHouse`
2. Click Corner 1 (green), Click Corner 2 (red)
3. Scroll wheel adjusts selection box height
4. Click to confirm - saves to `BepInEx/config/blueprints/MyHouse.yaml`

**Radius scan:**
1. Stand at center of structure
2. Console: `scanblueprint MyHouse 15`

**Builder workflow:**
- Builder picks a blueprint (from faction assignment, or randomly if idle)
- Hover text shows required materials and current stock
- Give materials via interact [E] - they accumulate
- Builder places one piece every 3 seconds once fully stocked

Blueprints sync to all clients on join. Copy `.yaml` files between servers to share.

### Odin's Camp

Odin, Shaman, and Munin NPCs. Trade items for Odin Credits, buy special meads, get skill buffs, summon combat pets.

### Console Commands

| Command | Description |
|---------|-------------|
| `odinhere` | Teleport Odin's camp to player |
| `whereodin` | Print Odin camp coordinates |
| `whereami` | Print player coordinates |
| `setodin` | Save Odin position to config |
| `findfarm` | Reveal nearest WoodFarm1 on map |
| `scanblueprint <name> <radius>` | Radius-based blueprint scan |
| `selectblueprint <name>` | Visual 2-corner blueprint selection |
| `previewscan <radius>` | Show scan area with markers |
| `listblueprints` | List all loaded blueprints |

### Hotkeys

| Key | Action |
|-----|--------|
| F7 | Toggle faction reputation overlay |
| Alt+E | Secondary NPC interact (configurable in BepInEx config) |

---

## YAML Configuration

All config files use YAML format and are hot-reloadable (changes take effect without restart for `faction_config.yaml`).

### faction_config.yaml

Controls factions, their relationships, reputation thresholds, and village spawn locations.

```yaml
Factions:
  RedTeam:
    Name: RedTeam
    Allies: [GreenTeam]          # Helping RedTeam also helps GreenTeam (+delta/3)
    Enemies: [BlueTeam]          # Helping RedTeam hurts BlueTeam (-delta/2)
    HostileThreshold: -30        # Below this = Hostile tier
    UnfriendlyThreshold: -10     # Below this = Unfriendly tier
    NeutralThreshold: 10         # Below this = Neutral tier
    FriendlyThreshold: 30        # Above this = Honored tier
    AssignedBlueprints: []       # Empty = build any blueprint. Or: [SmallHut, Tower]
  BlueTeam:
    Name: BlueTeam
    Allies: []
    Enemies: [RedTeam]
    HostileThreshold: -30
    UnfriendlyThreshold: -10
    NeutralThreshold: 10
    FriendlyThreshold: 30
  GreenTeam:
    Name: GreenTeam
    Allies: [RedTeam]
    Enemies: []
    HostileThreshold: -30
    UnfriendlyThreshold: -10
    NeutralThreshold: 10
    FriendlyThreshold: 30

ReputationEvents:
  NPCKilled: -50        # Reputation lost when player kills a faction NPC
  NPCDamaged: -10       # Per hit
  ItemGiven: 15         # Per donation to Supplier NPC
  QuestCompleted: 35    # Per quest completion

# Valheim location prefab names where villages spawn.
# Each location spawns one village (once, permanently) when a player loads that zone.
VillageLocations:
  # Meadows
  - WoodFarm1
  - WoodVillage1
  - WoodHouse1
  - WoodHouse2
  - WoodHouse3
  - WoodHouse4
  - WoodHouse5
  - WoodHouse6
  - WoodHouse7
  - WoodHouse8
  - WoodHouse9
  - WoodHouse10
  - WoodHouse11
  - WoodHouse12
  - WoodHouse13
  - Runestone_Meadows
  - Runestone_Boars
  - Dolmen01
  - Dolmen02
  - Dolmen03
  - ShipSetting01
  - CombatRuin01
  # Black Forest
  - Greydwarf_camp1
  - Ruin1
  - Ruin2
  - Crypt2
  - Crypt3
  - Crypt4
  - Runestone_BlackForest
  - Runestone_Greydwarfs
  - StoneTowerRuins03
  - StoneTowerRuins07
  - StoneTowerRuins08
  - StoneTowerRuins09
  - StoneTowerRuins10
  # Swamp
  - SwampHut1
  - SwampHut2
  - SwampHut3
  - SwampHut4
  - SwampHut5
  - SwampRuin1
  - SwampRuin2
  - Runestone_Swamps
  - Runestone_Draugr
  # Mountains
  - AbandonedLogCabin02
  - AbandonedLogCabin03
  - AbandonedLogCabin04
  - MountainGrave01
  - Waymarker01
  - Waymarker02
  - Runestone_Mountains
  - DrakeLorestone
  - StoneTowerRuins04
  - StoneTowerRuins05
  # Plains
  - GoblinCamp2
  - StoneHenge1
  - StoneHenge2
  - StoneHenge3
  - StoneHenge4
  - StoneHenge5
  - StoneHenge6
  - StoneTower1
  - StoneTower3
  - Ruin3
  - Runestone_Plains
  # Mistlands
  - Mistlands_Harbour1
  - Mistlands_DvergrTownEntrance1
  - Mistlands_DvergrTownEntrance2
  - Mistlands_GuardTower1_new
  - Mistlands_GuardTower2_new
  - Mistlands_GuardTower3_new
  - Mistlands_Lighthouse1_new
  - Mistlands_Viaduct1
  - Mistlands_Viaduct2
  - Mistlands_Statue1
  - Mistlands_Statue2
  - Runestone_Mistlands
  # Ashlands
  - CharredFortress
  - AshlandRuins
  - FortressRuins
  - CharredRuins1
  - CharredRuins2
  - CharredRuins3
  - CharredRuins4
  - CharredTowerRuins1
  - CharredTowerRuins2
  - CharredTowerRuins3
  - PlaceofMystery1
  - PlaceofMystery2
  - PlaceofMystery3
  - Runestone_Ashlands
```

**Adding custom factions:** Add a new entry under `Factions:`. Villages randomly pick from all factions except `Neutral` and `Villagers`.

**Controlling village density:** Add or remove location names. More entries = more villages. Each is a Valheim location prefab name (case-sensitive). Remove WoodHouse entries if you want fewer meadows villages.

**AssignedBlueprints:** Controls which blueprints a faction's builders can use. Empty/omitted = any blueprint. Set specific names to make factions build different things.

### faction_quests.yaml

Defines quests offered by faction NPCs.

```yaml
Quests:
  - ID: redteam_hunt_01             # Unique identifier
    Name: Greydwarf Menace           # Display name
    FactionName: RedTeam             # Which faction offers this quest
    RequiredReputation: 10           # Minimum rep needed (0 = anyone can take it)
    Description: Hunt down 5 Greydwarfs threatening our village
    Objective:
      Type: Kill                     # Kill, Collect, Deliver, Explore
      Target: Greydwarf              # Creature/item/location prefab name
      Amount: 5                      # How many
      Biome: BlackForest             # Any, Meadows, BlackForest, Swamp, Mountain, Plains, Mistlands, Ashlands
    Reward:
      ReputationGain: 35             # Rep gained with the quest's faction
      OdinCredits: 50                # Currency reward
      Items:                         # Item rewards (optional)
        - ItemName: OdinLegacy
          Amount: 3
          Quality: 1

  - ID: blueteam_deliver_01
    Name: Iron Shipment
    FactionName: BlueTeam
    RequiredReputation: 30           # Requires Honored tier
    Description: Deliver 30 iron bars to our smithy
    Objective:
      Type: Collect
      Target: Iron
      Amount: 30
      Biome: Any
    Reward:
      ReputationGain: 50
      OdinCredits: 75
      Items:
        - ItemName: Coins
          Amount: 500
          Quality: 1
```

**Objective types:**
- `Kill` - Target is a creature prefab (Greydwarf, Wolf, Skeleton, Draugr, etc.)
- `Collect` - Target is an item prefab (Stone, Wood, IronScrap, Iron, etc.)
- `Deliver` - Target is an item to bring to a faction NPC
- `Explore` - Target is a location to discover

**Adding quests:** Add entries to the `Quests:` list. Each needs a unique `ID`. `FactionName` must match a faction defined in `faction_config.yaml`.

### Blueprint YAML Files

Located in `BepInEx/config/blueprints/`. One file per blueprint, auto-generated by scan commands.

```yaml
Name: SmallHut
ResourceCosts:
  Wood: 120
  Stone: 45
  RoundLog: 8
Pieces:
  - PrefabName: wood_floor_1x1
    PosX: 0.0
    PosY: 0.0
    PosZ: 0.0
    RotX: 0.0
    RotY: 0.0
    RotZ: 0.0
  - PrefabName: wood_wall_half
    PosX: 0.0
    PosY: 0.0
    PosZ: 2.0
    RotX: 0.0
    RotY: 90.0
    RotZ: 0.0
  # ... more pieces
```

**ResourceCosts** is auto-calculated from each piece's crafting recipe when scanned. You can manually edit costs to rebalance.

**Positions** are relative to the blueprint origin (first piece scanned). Builder NPCs place the structure at their chosen site using these offsets.

**Sharing:** Copy `.yaml` files between players/servers. They auto-sync to joining clients in multiplayer.

---

## Multiplayer

- **Server-authoritative reputation** - Changes validated by server, broadcast to all clients
- **Join sync** - Joining players receive full reputation state and all blueprints
- **Persistent villages** - Faction assignment stored in ZDO, survives restarts
- **Quest sync** - Faction quest definitions sync from server config

Both server and all clients must have `OdinPlus.dll` in their `BepInEx/plugins/` folder.

---

## Troubleshooting

**No villages spawning:**
- Villages spawn when you load a zone containing a listed location. Walk near ruins/runestones.
- Check log for: `[HumanManager] TrySeedVillage matched:` entries
- Already-visited locations that spawned before mod install won't re-trigger unless you start a new world

**NPCs won't interact:**
- Check faction reputation (F7). Unfriendly/Hostile NPCs refuse interaction.
- Use Alt+E for secondary interactions (skill training, quest accept).

**Builder not building:**
- Donate materials first. Check hover text for what's needed and current stock.
- Ensure at least one `.yaml` file exists in `BepInEx/config/blueprints/`.

**Blueprint scan captures nothing:**
- Stand closer or increase radius. Only player-built pieces are captured.
- Use `previewscan <radius>` first to visualize the scan area.

**Reputation cascade unexpected:**
- Check `Allies` and `Enemies` in faction_config.yaml. Helping RedTeam hurts all its Enemies.

**Log file:** `C:\Users\<you>\AppData\LocalLow\IronGate\Valheim\Player.log`

Filter for mod messages:
```powershell
Select-String -Path Player.log -Pattern "OdinPlus|HumanManager|FactionManager|BuilderNPC"
```

---

## Compatibility

Works alongside PlanBuild, Infinity Hammer, BuildShare, Valheim Plus, and most other mods. No Harmony patches on building/placement systems.

---

**Mod GUID:** `buzz.valheim.OdinPlus`
**Version:** 0.2.6
**Nexus Mods ID:** 798
