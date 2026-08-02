# OdinPlus - Valheim NPC & Quest Mod

## Project Overview

**OdinPlus** adds NPCs, quests, trading, factions, and builder systems to Valheim. Players interact with Odin's camp, recruit NPCs, complete quests, and NPCs can build structures from blueprints.

**Current Version:** 0.2.6  
**Target Game:** Valheim 0.221.12 (Unity 6000.0.61)  
**Framework:** BepInEx 5.4.23.3  
**Platform:** .NET Framework 4.8

---

## Current Status (2026-08-01)

### ✅ Working Systems

1. **Console Commands** (Terminal.ConsoleCommand pattern)
   - `/odinhere` - Teleport Odin camp to player
   - `/whereodin` - Show Odin camp position
   - `/whereami` - Show player position
   - `/setodin` - Save Odin position to config
   - `/findfarm` - Reveal nearest WoodFarm1 location
   - `/scanblueprint <name> <radius>` - Scan built structures (radius method)
   - `/previewscan <radius>` - Preview scan area with yellow spheres
   - `/selectblueprint <name>` - Visual 2-corner selection with scroll wheel height adjustment
   - `/listblueprints` - List all loaded blueprints from YAML

2. **Builder NPC System**
   - NPCs accept wood/stone donations via [E] interact
   - Auto-build structures from blueprint YAML files when resources sufficient
   - Builds 1 piece every 3 seconds with visual progress
   - Builds at NPC's current position (simplified movement system)
   - Spawns at WoodFarm1 locations via `LocationProxy.SpawnLocation` hook
   - Status: **PRODUCTION READY**

3. **Blueprint Creation System - YAML Based**
   - **Visual Selector (Primary)**: Place 2 corners → scroll wheel for height → auto-exports to YAML
     - Real-time cyan transparent preview box (uses Valheim's shader system)
     - Auto-calculates resource costs from `Piece.m_resources[]`
     - Saves to `BepInEx/config/blueprints/<name>.yaml`
     - Instructions throttled to 0.5s (Center message, auto-clears)
   - **Console Scanner (Alternative)**: Stand at center → `/scanblueprint MyHouse 15`
     - Radius-based scanning from player position
     - Same YAML export format
   - **YAML Storage**: One blueprint = one `.yaml` file in `blueprints/` folder
   - **Server Sync**: All blueprints auto-sync to joining clients
   - **Shareable**: Copy `.yaml` files between servers/players
   - Status: **PRODUCTION READY**

4. **NPC System**
   - Odin, Shaman, Munin (raven) NPCs at camp
   - Human NPCs at farms/locations: fighters, traders, villagers, workers
   - Material villagers accept resources for credits
   - Quest NPCs give dungeon/hunt/treasure quests
   - All NPC types spawning correctly at locations

5. **Quest System**
   - Dungeon quests (find location, get key)
   - Hunt quests (kill specific creature)
   - Treasure quests (find hidden chests)
   - Quest progress tracked per-player in OdinData

6. **Faction System** (multiplayer-ready)
   - Per-player reputation tracking (-100 to +100)
   - Faction relationships: helping allies boosts their rep (+delta/3), helping enemies angers them (-delta/2)
   - Dynamic NPC behavior: Hostile/Unfriendly NPCs refuse interaction
   - Faction-colored capes: NPCs visually identifiable by faction
   - **F7 GUI**: Valheim-themed wood panel in top-right showing all faction reputations
   - **Server-authoritative multiplayer sync**: Reputation changes sync from server to all clients via RPC
   - Status: **PRODUCTION READY** (2026-07-31 late evening)

7. **Multiplayer Reputation Sync** (NEW - 2026-07-31)
   - **Server-authoritative**: All reputation changes validated by server
   - **RPCs implemented**:
     - `ReputationChange` (client → server): Request reputation modification
     - `ReputationUpdate` (server → all clients): Broadcast new reputation value
     - `ReputationSync` (server → joining client): Full reputation state on join
   - **Synced actions**:
     - NPC killed (HumanVillager.OnKilled)
     - NPC damaged (HumanVillager.Damage)
     - Items given to NPC (MaterialVillager)
     - Quest completed (HumanWorker, FactionQuestSystem)
   - **Join sync**: Server sends full reputation dictionary to each joining player via RPC_PeerInfo patch
   - **Local persistence**: Each player's reputation saves to `<CharacterName>.odinplus` (YAML)
   - Status: **IMPLEMENTED, NEEDS MULTIPLAYER TESTING**

8. **Config System** (scaffolded, not integrated)
   - YAML-based with live file watching (ConfigManager.cs)
   - Hot-reload on file changes (0.5s debounce)
   - Status: **READY TO INTEGRATE** (planned for next session)

---

## Architecture

### Core Files

**Entry Point:**
- `0Main/Plugin.cs` - BepInEx plugin, Harmony patches, console commands, RPC registration

**Core Systems:**
- `0Main/OdinPlus.cs` - Main component, initialization flow, Faction GUI (F7 panel)
- `0Main/OdinData.cs` - Per-player data persistence (credits, keys, quests, faction rep)
- `0Main/ConfigManager.cs` - YAML config loader with file watching

**NPC Management:**
- `1NPC/NpcManager.cs` - Odin, Shaman, Munin initialization
- `1NPC/HumanNPC.cs` - Base class for all human NPCs
- `1NPC/FactionSystem.cs` - Faction definitions, reputation management, multiplayer RPC handlers
- `6Humans/HumanManager.cs` - Human NPC creation, spawning at locations
- `6Humans/BuilderNPC.cs` - Builder NPC component with blueprint system
- `6Humans/BlueprintScanner.cs` - In-game blueprint scanner

**Quest System:**
- `5Quest/QuestManager.cs` - Quest tracking and management
- `5Quest/FactionQuestSystem.cs` - Faction-based quest generation
- `5Quest/Quest.cs` - Quest data structure
- `5Quest/*QuestProcesser.cs` - Quest type processors (Hunt, Dungeon, Treasure, Search)

**Items & Assets:**
- `3Items/OdinItem.cs` - Custom items (OdinLegacy)
- `3Items/OdinMeads.cs` - Custom mead effects
- `3Items/PrefabManager.cs` - Prefab registration
- `8Assets/FxAssetManager.cs` - Visual effects from AssetBundles
- `8Assets/ResourceAssetManager.cs` - Embedded resource loader

**Pets:**
- `4Pets/PetManager.cs` - Troll/Wolf pet system

**Status Effects:**
- `2StatusEffects/OdinSE.cs` - Custom status effects
- `2StatusEffects/SE_SummonPet.cs` - Pet summoning effect

**Utilities:**
- `9Misc/DBG.cs` - Logging wrapper
- `9Misc/Localization.cs` - Translation system (BuzzLocal)
- `9Misc/LocationManager.cs` - Location discovery and spawning
- `9Misc/DevTool.cs` - Debug utilities

---

## Multiplayer Sync Flow

### Reputation Change (Player damages NPC):
```
[Client] HumanVillager.Damage() detects player hit
    ↓
[Client] Checks ZNet.instance.IsServer()
    ↓ (if client)
[Client] ZRoutedRpc.InvokeRoutedRPC(0L, "ReputationChange", playerID, faction, -10)
    ↓
[Server] FactionManager.RPC_ReputationChange() receives request
    ↓
[Server] Validates and applies: ModifyReputation(playerID, faction, -10)
    ↓
[Server] Broadcasts: ZRoutedRpc.InvokeRoutedRPC(ZRoutedRpc.Everybody, "ReputationUpdate", playerID, faction, newRep)
    ↓
[All Clients] FactionManager.RPC_ReputationUpdate() receives new value
    ↓
[All Clients] Update local _reputation dictionary
    ↓
[Affected Player's Client] Show MessageHud notification with new tier
```

### Player Joins Server:
```
[Server] ZNet.RPC_PeerInfo() detects new peer connection (Harmony postfix)
    ↓
[Server] Serializes entire _reputation dictionary to YAML
    ↓
[Server] rpc.Invoke("ReputationSync", yamlReputation) to joining client only
    ↓
[Joining Client] FactionManager.RPC_ReputationSync() receives full state
    ↓
[Joining Client] Deserializes YAML, replaces local _reputation dictionary
    ↓
[Joining Client] Reputation now matches server state
```

---

## Critical Patches

### ZNetScene.RemoveObjects NullRef Fix
**File:** `Plugin.cs` lines 380-423  
**Problem:** `m_instances` dictionary had null ZNetView entries causing per-frame exceptions  
**Fix:** Harmony Finalizer swallows NullRef, periodic cleanup every 300 frames  
**Status:** ✅ Working

### LocationProxy.SpawnLocation Hook
**File:** `Plugin.cs` lines 472-482  
**Problem:** SoftReference lazy loading in 0.221.12 prevented spawn at ZoneSystem.Start  
**Fix:** Hook `LocationProxy.SpawnLocation` postfix, call `HumanManager.OnLocationSpawned`  
**Status:** ✅ Working

### Double ZNetView Fix
**File:** `HumanManager.cs`  
**Problem:** Spawner prefabs auto-registered with ZNetScene, then got another ZNetView on Instantiate  
**Fix:** Spawners stored in separate `SpawnerTemplates` dict, not in `PrefabList`  
**Status:** ✅ Working

### HumanMessager Quest Fix
**File:** `HumanMessager.cs` lines 46-60  
**Problem 1:** Tried to get `WorkerNPCHuman` from ZNetScene (not registered there)  
**Fix 1:** Gets from `HumanManager.PrefabList` with null check  
**Problem 2:** Spawned worker never set ZDO key `"npcname"`, HumanWorker.Choice1 always failed check  
**Fix 2:** Set ZDO key when spawning worker: `znv.GetZDO().Set("npcname", key)`  
**Status:** ✅ Working (2026-07-31 evening fix)

### Munin/Shaman FPS Fix
**File:** `NpcManager.cs`  
**Problem:** ZNetView on inactive GameObjects registered with ZNetScene, caused stale position lookups  
**Fix:** Destroy ZNetView before `SetActive(true)` using `DestroyImmediate`  
**Status:** ✅ Working

### Quest System Per-Frame Optimization
**Files:** `QuestManager.cs`, `HuntTarget.cs`  
**Problem 1:** QuestManager.Update() iterated all active quests every frame checking locations (60 FPS × 10 quests = 600 checks/sec)  
**Fix 1:** Changed to `InvokeRepeating(CheckPlace, 1f, 0.5f)` - checks every 0.5s instead of every frame, stops after quest placed  
**Problem 2:** HuntTarget.Update() did dictionary lookup every frame per hunt creature (60 FPS × 5 creatures = 300 checks/sec)  
**Fix 2:** Changed to `InvokeRepeating(ValidateQuest, 5f, 5f)` - checks every 5s instead of every frame  
**Performance Gain:** ~300x reduction (900 checks/sec → 3 checks/sec)  
**Status:** ✅ Fixed (2026-07-31 runtime-MCP investigation)

### Faction Cape Colors & GUI
**Files:** `HumanVis.cs`, `Plugin.cs`, `OdinPlus.cs`  
**Feature:** NPCs have color-coded capes based on faction, F7 GUI shows reputation  
**Implementation:** 
- `SetCapeColorByFaction()` sets ZDO key `"UtilityItemColor"` based on `HumanNPC.FactionName`  
- F7 GUI creates Valheim-themed panel with `litpanel` material (brown woodpanel texture)
- Uses `Resources.FindObjectsOfTypeAll<Material>()` to find `litpanel` material at runtime
- Parents panel to Hud's Canvas for proper rendering
**Colors:**
- RedTeam: Red (0.8, 0.1, 0.1)
- BlueTeam: Blue (0.1, 0.3, 0.8)
- GreenTeam: Green (0.1, 0.6, 0.2)
- YellowTeam: Yellow (0.9, 0.8, 0.1)
- PurpleTeam: Purple (0.6, 0.2, 0.8)
- Default/Neutral: Gray (0.5, 0.5, 0.5)

**Faction GUI Overlay:**
- Press **F7** to toggle reputation display (configurable hotkey)
- Shows all factions with color-coded tiers and numeric values
- Hover over NPCs shows: `[FactionName - Tier] (RepValue)`
- Status: ✅ Working (2026-07-31 late evening)

### YAML Serialization Migration
**Files:** `OdinData.cs`, `DevTool.cs`, `Localization.cs`, `OdinPlus.csproj`  
**Change:** Replaced Newtonsoft.Json with YamlDotNet for all save/load operations  
**Benefits:**
- Single dependency (YamlDotNet only)
- Human-readable save files (.odinplus now YAML instead of binary JSON)
- User-editable translation files (`odinplus_translations_en.yaml`, `odinplus_translations_zh.yaml`)
- Smaller DLL size (removed Newtonsoft.Json from ILRepack)
**Breaking Change:** Old `.odinplus` save files incompatible - players start fresh  
**Translation Files:** Auto-generated on first run in `BepInEx/config/`  
**Status:** ✅ Complete (2026-07-31 evening migration)

---

## Faction System Gameplay

### How Reputation Works

**Reputation Range:** -100 to +100

**Tiers:**
- **Hostile** (< -30): NPCs attack on sight, refuse all interaction
- **Unfriendly** (-30 to -10): NPCs refuse interaction, won't trade/quest
- **Neutral** (-10 to 10): Default behavior, normal interactions
- **Friendly** (10 to 30): Positive interactions (future: discounts, bonus quests)
- **Honored** (> 30): Best relationship (future: unique rewards, special quests)

### Reputation Changes

**Actions that modify reputation:**

| Action | Reputation Change | Notes |
|--------|-------------------|-------|
| Kill NPC | -50 | Massive penalty, likely makes faction hostile |
| Damage NPC | -10 | Per hit, accumulates quickly |
| Give items (wood/stone) | +15 | MaterialVillager donations |
| Complete message quest | +35 | HumanWorker quests |
| Complete faction quest | Variable | FactionQuestSystem (configurable) |

### Faction Relationships (Cascading Effects)

**Defined in `faction_config.yaml`:**
```yaml
RedTeam:
  Allies: [GreenTeam]
  Enemies: [BlueTeam]
```

**Cascade Rules:**
- Helping a faction: Allies gain +rep/3, enemies lose -rep/2
- Hurting a faction: Enemies gain +rep/2, allies lose -rep/3

**Example Scenarios:**

**Scenario 1:** Give 50 wood to RedTeam villager
- RedTeam: +15
- GreenTeam (ally): +5
- BlueTeam (enemy): -7

**Scenario 2:** Complete quest for BlueTeam
- BlueTeam: +35
- RedTeam (enemy): -17

**Scenario 3:** Kill GreenTeam NPC
- GreenTeam: -50 (now Hostile)
- RedTeam (ally of Green): -16 (cascades)
- All GreenTeam NPCs turn hostile and attack

### Visual Indicators

**Cape Colors:**
- NPCs wear colored capes matching their faction
- Instantly identify faction allegiance from a distance

**Hover Text:**
```
[NPC Name]
[FactionName - Tier] (RepValue)
[E] Talk
```

**F7 GUI Overlay:**
```
┌─────────────────────────────┐
│   Faction Reputation        │
├─────────────────────────────┤
│ RedTeam: Friendly (22)      │
│ BlueTeam: Hostile (-45)     │
│ GreenTeam: Neutral (3)      │
│ YellowTeam: Neutral (0)     │
│                             │
│ [F7] Close                  │
└─────────────────────────────┘
```

### Strategic Gameplay

**Rebalancing Reputation:**
- Can always kill NPCs to reduce reputation (for rebalancing)
- Give items to hostile faction's enemies to indirectly harm them
- Complete quests for opposing factions to shift allegiances

**Example Strategy:**
1. You're Hostile with RedTeam (-50)
2. Option A: Grind +15 donations until Neutral (requires ~27 donations)
3. Option B: Help BlueTeam (RedTeam enemy), indirectly harm RedTeam further
4. Option C: Kill RedTeam NPCs, embrace full hostility, work with BlueTeam

**No Permanent Locks:**
- Reputation can always be rebalanced
- Killing allied NPCs lets you switch factions mid-game
- Creates dynamic world where choices have consequences but aren't permanent

---

## Blueprint System (2026-08-01 YAML Update)

### Overview

Blueprints are stored as **individual YAML files** in `BepInEx/config/blueprints/`. Each blueprint is one `.yaml` file containing piece positions, rotations, and resource costs.

**Key Features:**
- Visual 2-corner selection system with real-time preview
- Scroll wheel height adjustment for multi-story buildings
- Auto-calculation of costs from actual `Piece.m_resources[]` arrays
- Server-client sync (all blueprints sync on join)
- Shareable (copy `.yaml` files between servers)
- Zero Harmony conflicts with PlanBuild/Infinity Hammer/BuildShare

### Creating Blueprints (Visual Method - Recommended)

**Step 1:** Build structure with hammer in-game

**Step 2:** Start selection mode
```
F5 → selectblueprint MyHouse
```

**Step 3:** Place corner markers
- Click Corner 1 (green cylinder)
- Click Corner 2 (red cylinder)
- Cyan transparent box appears

**Step 4:** Adjust height with scroll wheel
- Scroll UP: Box extends vertically (+2m per tick)
- Scroll DOWN: Box shrinks vertically (-2m per tick)
- Top-center shows current height adjustment

**Step 5:** Confirm
- Click to scan all pieces in box
- Auto-saves to `BepInEx/config/blueprints/MyHouse.yaml`

**Result:**
```yaml
Name: MyHouse
ResourceCosts:
  Wood: 120
  Stone: 45
Pieces:
  - PrefabName: wood_floor_1x1
    PosX: 0.0
    PosY: 0.0
    PosZ: 0.0
    RotX: 0.0
    RotY: 0.0
    RotZ: 0.0
  # ... more pieces
```

### Creating Blueprints (Console Method - Quick)

**Step 1:** Build structure, stand at center

**Step 2:** Scan radius
```
F5 → scanblueprint MyHouse 15
```

**Result:** Same YAML format, saved to `blueprints/MyHouse.yaml`

### Compatibility with Other Mods

See [BLUEPRINT_COMPATIBILITY.md](BLUEPRINT_COMPATIBILITY.md) for full analysis.

**Summary:**
- ✅ **PlanBuild**: Fully compatible, scan PlanBuild structures with `/selectblueprint`
- ✅ **Infinity Hammer**: Fully compatible, scan IH-placed pieces
- ✅ **BuildShare**: Compatible (different storage format, but both use same Valheim APIs)
- ✅ **Zero Harmony patches** on building/placement systems

---

## Blueprint System (Legacy Documentation)

### Blueprint Data Structure

```csharp
public class Blueprint
{
    public string name;
    public Dictionary<string, int> resourceCosts; // {"Wood": 45, "Stone": 20}
    public BlueprintPiece[] pieces;
}

public struct BlueprintPiece
{
    public string prefabName;     // "wood_floor_1x1", "wood_wall_roof"
    public Vector3 localPosition; // Relative to builder position
    public Vector3 rotation;      // Euler angles
}
```

### Creating Blueprints (In-Game Method)

**Step 1:** Build with hammer in-game
```
- Use normal building pieces
- Build on flat ground
- Stand at center when done
```

**Step 2:** Preview scan area
```
F5 → previewscan 10
```

**Step 3:** Scan and export
```
F5 → scanblueprint MyHut 10
```

**Step 4:** Copy code from file
```
Open: BepInEx/plugins/blueprint_MyHut.txt
Copy all code
```

**Step 5:** Add to BuilderNPC.cs
```csharp
// In Blueprints.Init() method:
All.Add(new Blueprint(
    "MyHut",
    new Dictionary<string, int> { { "Wood", 45 } },
    new BlueprintPiece[]
    {
        new BlueprintPiece("wood_floor_1x1", new Vector3(0f, 0f, 0f), Vector3.zero),
        // ... more pieces
    }
));
```

**Step 6:** Rebuild and deploy

### Available Building Pieces

**Wood:**
- Floors: `wood_floor_1x1`, `wood_floor`
- Walls: `wood_wall_roof`, `wood_wall_half`, `wood_wall_quarter`, `wood_wall_log`
- Roofs: `wood_roof`, `wood_roof_45`, `wood_roof_top`, `wood_roof_top_45`
- Corners: `wood_roof_icorner`, `wood_roof_ocorner`, `wood_roof_icorner_45`, `wood_roof_ocorner_45`
- Doors: `wood_door`, `wood_gate`
- Beams: `wood_beam`, `wood_beam_26`, `wood_beam_45`
- Poles: `wood_pole`, `wood_pole2`, `wood_pole_log`
- Misc: `wood_fence`, `wood_stair`, `wood_stepladder`, `wood_window`, `wood_ledge`

**Stone:**
- Floors: `stone_floor`, `stone_floor_2x2`
- Walls: `stone_wall_1x1`, `stone_wall_2x1`, `stone_wall_4x2`
- Arches: `stone_arch`
- Pillars: `stone_pillar`
- Stairs: `stone_stair`

**Crafting:**
- `piece_workbench`, `fire_pit`, `piece_cauldron`
- `piece_chest`, `piece_chest_wood`, `piece_chest_blackmetal`

Full list available via Unity MCP tools or `DumpValheimPieces.cs`.

---

## Config System (Not Yet Integrated)

### Planned Config Files

**faction_config.yaml**
```yaml
Factions:
  RedTeam:
    Allies: [GreenTeam]
    Enemies: [BlueTeam]
    Thresholds:
      Hostile: -30
      Unfriendly: -10
      Neutral: 10
      Friendly: 30
```

**faction_quests.yaml**
```yaml
Quests:
  - ID: hunt_redteam_1
    Type: Hunt
    Target: Boar
    Count: 5
    Reward: 50
    RequiredReputation: 0
```

**item_values.yaml**
```yaml
Items:
  TrophyBlob: 20
  TrophyBoar: 5
  Wood: 1
  Stone: 1
```

**odinplus_translations_en.yaml** (auto-generated from Localization.cs)
```yaml
op_buy: Buy
op_crd: Credits
op_god: Odin
op_god_nocrd: Hard work is the only way to get rewarded.
op_villager_greet: Greetings, traveler!
op_villager_thanks: Thank you!
# ... 200+ translation keys
```

**odinplus_translations_zh.yaml** (Chinese translations)
```yaml
op_use: 升级你的技能：
op_se_troll: 宠物巨魔
# ... Chinese translations
```

### ConfigManager Features

- **File watching:** Changes apply in 0.5s
- **Thread-safe:** Uses UnityMainThreadDispatcher for reload events
- **Auto-defaults:** Creates missing configs with defaults
- **YAML validation:** Logs errors, falls back to defaults

**Status:** Scaffolded in `ConfigManager.cs`, not yet wired to existing systems.

---

## Known Valheim 0.221.12 Changes

### Breaking Changes from 0.217.x

1. **Terminal Commands:** `Chat.InputText` → `Terminal.ConsoleCommand` pattern
2. **SoftReference Lazy Loading:** `LocationProxy.m_prefab.Asset` null at `ZoneSystem.Start`
3. **BepInEx 5.4.21:** ConfigDescription.Tags is `object[]` not `Dictionary<string,object>`

### Component Patterns

**GetComponent Search:**
```csharp
// Only searches root GameObject:
GetComponent<T>()

// Searches children (use this):
GetComponentInChildren<T>(true)  // true = include inactive
```

**ZNetView Registration:**
```csharp
// WRONG - registers inactive GameObject:
var go = Instantiate(prefab);
go.SetActive(false);

// CORRECT - destroy ZNetView first:
var go = Instantiate(prefab);
go.SetActive(false);
var znv = go.GetComponentInChildren<ZNetView>(true);
if (znv != null) DestroyImmediate(znv);
go.SetActive(true);
```

**Prefab Registration:**
```csharp
// Check before adding:
var hashcode = obj.name.GetStableHashCode();
if (!zNetScene.m_namedPrefabs.ContainsKey(hashcode))
{
    zNetScene.m_prefabs.Add(obj);
    zNetScene.m_namedPrefabs.Add(hashcode, obj);
}
```

---

## Build & Deploy

### Build Commands

```powershell
cd "c:\Users\zarboz\Desktop\valhoom\buzzvalheimmods\OdinPlus"
dotnet build -c Release
```

**Output:** `bin\Debug\OdinPlus.merged.dll` (merged with YamlDotNet via ILRepack)

### Deploy to Game (Auto-Deploy Enabled)

Build in Visual Studio 2022 → **Auto-copies to BepInEx plugins folder**

Post-build event in `.csproj`:
```xml
<Target Name="AfterBuild">
  <Copy SourceFiles="$(OutputPath)OdinPlus.merged.dll" 
        DestinationFiles="C:\Program Files (x86)\Steam\steamapps\common\Valheim\BepInEx\plugins\OdinPlus.dll" 
        ContinueOnError="true" />
</Target>
```

**Manual deploy:**
```powershell
# Close Valheim first!
cp "bin\Debug\OdinPlus.merged.dll" "C:\Program Files (x86)\Steam\steamapps\common\Valheim\BepInEx\plugins\OdinPlus.dll"
```

### Verify Deployment

```powershell
# Check BepInEx log
Get-Content "C:\Users\zarboz\AppData\LocalLow\IronGate\Valheim\Player.log" -Wait -Tail 50

# Search for errors
Select-String -Path "C:\Users\zarboz\AppData\LocalLow\IronGate\Valheim\Player.log" -Pattern "Error|Exception|OdinPlus"
```

---

## Testing Workflow

### 1. Test Console Commands
```
F5 (open console)
/odinhere
/whereodin
/findfarm
```

### 2. Test Blueprint Scanner
```
1. Build small hut with hammer (2x2 floor, 4 walls, door, roof)
2. Stand at center
3. F5 → previewscan 10 (check yellow spheres)
4. F5 → scanblueprint TestHut 10
5. Check BepInEx/plugins/blueprint_TestHut.txt exists
```

### 3. Test Builder NPC
```
1. /findfarm (find WoodFarm1)
2. Go to location, find BuilderNPCHuman
3. Give 40 wood (if blueprint needs 40)
4. Watch NPC build structure piece-by-piece
5. Verify structure looks correct
```

### 4. Test Faction System (Single-player)
```
1. F7 to open faction GUI
2. Damage a RedTeam NPC
3. Check reputation drops in GUI
4. Try to interact - should refuse if Unfriendly/Hostile
5. Give wood to NPC - reputation increases
```

### 5. Test Multiplayer Sync (Requires 2+ players)
```
SERVER:
1. Host game with OdinPlus installed
2. Damage a faction NPC
3. Check BepInEx log for "[FactionManager] Server applied reputation change"

CLIENT:
1. Join server
2. Check log for "[FactionManager] Client received full reputation sync"
3. Press F7 - should see same reputation values as server
4. Damage different faction NPC on client
5. Both server and client should see reputation update
6. Rejoin server - reputation should persist
```

### 6. Check Logs
```
- [OdinPlus] messages for mod loading
- [BlueprintScanner] for scan results
- [BuilderNPC] for build progress
- [FactionManager] for reputation sync
- No NullReferenceException spam
- No "Double ZNetView" warnings
```

---

## Dependencies

**NuGet Packages:**
- YamlDotNet 12.3.1 (for YAML serialization - repacked into DLL via ILRepack)

**BepInEx References:**
- 0Harmony.dll (Harmony patching)
- BepInEx.dll (plugin framework)

**Valheim References:**
- assembly_valheim.dll (game code)
- assembly_utils.dll
- assembly_guiutils.dll

**Unity References:**
- UnityEngine.dll (Unity 6000.0.61)
- UnityEngine.CoreModule.dll
- UnityEngine.UI.dll
- Unity.TextMeshPro.dll
- All Unity module DLLs (see OdinPlus.csproj for full list)

---

## Common Issues

### "Console command not working"
**Cause:** Using old `Chat.InputText` pattern  
**Fix:** Use `Terminal.ConsoleCommand` (see Plugin.cs lines 79-148)

### "NPCs not spawning at farms"
**Cause:** SoftReference.Asset is null at ZoneSystem.Start  
**Fix:** Hook `LocationProxy.SpawnLocation` postfix (see Plugin.cs lines 472-482)

### "NullReferenceException in ZNetScene.RemoveObjects"
**Cause:** Stale ZNetView entries in m_instances dict  
**Fix:** Finalizer + periodic cleanup (see Plugin.cs lines 380-423)

### "Double ZNetView warnings"
**Cause:** Spawner prefabs registered with ZNetScene, then Instantiate adds another  
**Fix:** Store spawners in separate SpawnerTemplates dict (see HumanManager.cs)

### "Blueprint scanner finds no pieces"
**Cause:** Standing too far, or radius too small  
**Fix:** Stand at center of structure, increase radius parameter

### "Builder NPC not building"
**Cause:** Insufficient resources, or no blueprints defined  
**Fix:** Give exact resource amounts, check BuilderNPC.cs Blueprints.Init() has entries

### "Reputation not syncing in multiplayer"
**Cause:** RPC not firing, or server not running OdinPlus  
**Fix:** Check logs for "[FactionManager] Server applied reputation change" and "[FactionManager] Reputation synced". Ensure both server and all clients have OdinPlus.dll deployed.

### "F7 GUI not showing"
**Cause:** Hud.instance not available, or Canvas not found  
**Fix:** Only press F7 while in-game (not main menu). Check log for "[FactionGUI]" messages.

### "F7 GUI shows white panel instead of wood texture"
**Cause:** litpanel material not found or not applied  
**Fix:** Check log for "[FactionGUI] Found material: litpanel". Material search happens at runtime via Resources.FindObjectsOfTypeAll<Material>().

---

## Documentation Files

- **README.md** - End-user documentation (features, config, console commands)
- **BLUEPRINT_SCANNER_GUIDE.md** - Complete in-game scanner user guide (if exists)
- **BLUEPRINT_SYSTEM_READY.md** - Unity exporter workflow (if exists)
- **CONFIG_DOCUMENTATION.md** - YAML config system guide (if exists)
- **FINAL_SESSION_SUMMARY.md** - Latest session work summary (if exists)
- **SESSION_SUMMARY.md** - Technical implementation details (if exists)
- **FIND_VALHEIM_PIECES.md** - Piece name lookup guide (if exists)
- **CLAUDE.md** (this file) - Full project status and developer reference

---

## Important Paths

### Development
```
Project Root:       c:\Users\zarboz\Desktop\valhoom\buzzvalheimmods\OdinPlus\
Source Files:       c:\Users\zarboz\Desktop\valhoom\buzzvalheimmods\OdinPlus\
Build Output:       bin\Debug\OdinPlus.dll
Unity Tools:        c:\Users\zarboz\Desktop\valhoom\BlueprintExporter.cs
                    c:\Users\zarboz\Desktop\valhoom\DumpValheimPieces.cs
```

### Game Installation
```
Valheim Root:       C:\Program Files (x86)\Steam\steamapps\common\Valheim\
BepInEx Plugins:    C:\Program Files (x86)\Steam\steamapps\common\Valheim\BepInEx\plugins\
BepInEx Config:     C:\Program Files (x86)\Steam\steamapps\common\Valheim\BepInEx\config\
Game Log:           C:\Users\zarboz\AppData\LocalLow\IronGate\Valheim\Player.log
```

### Files to Deploy
```
OdinPlus.dll              → BepInEx\plugins\
YamlDotNet.dll            → BepInEx\plugins\ (if not merged via ILRepack)
faction_config.yaml       → BepInEx\config\ (auto-generated)
faction_quests.yaml       → BepInEx\config\ (auto-generated)
odinplus_translations_*.yaml → BepInEx\config\ (auto-generated)
```

---

## Code Style & Patterns

### Logging
```csharp
DBG.blogInfo("Info message");
DBG.blogWarning("Warning message");
DBG.blogError("Error message");
```

### Null Checks
```csharp
if (Player.m_localPlayer == null) { DBG.blogWarning("No player"); return; }
if (!OdinPlus.isNPCInit) { DBG.blogWarning("NPCs not initialized"); return; }
```

### RPC Registration
```csharp
// In Plugin.cs ReigsterRpc():
ZRoutedRpc.instance.Register("RPCName", new Action<long, string>(RPC_Handler));

// Handler:
private static void RPC_Handler(long sender, string data)
{
    if (!ZNet.instance.IsServer()) return; // Server-side only
    // Handle RPC
}
```

### Server-Authoritative Reputation Change
```csharp
// Gameplay code (HumanVillager, MaterialVillager, etc.):
string playerID = player.GetZDOID().ToString();
if (ZNet.instance.IsServer())
{
    FactionManager.ModifyReputation(playerID, faction, delta, true);
}
else
{
    // Client sends request to server
    ZRoutedRpc.instance.InvokeRoutedRPC(0L, "ReputationChange", playerID, faction, delta);
}
```

### Harmony Patches
```csharp
[HarmonyPatch(typeof(TargetClass), "MethodName")]
private static class Patch_TargetClass_MethodName
{
    private static void Postfix(TargetClass __instance, ref ReturnType __result)
    {
        // Postfix logic
    }
}
```

### Finalizer Pattern (Error Suppression)
```csharp
[HarmonyPatch(typeof(TargetClass), "MethodName")]
private static class TargetClass_MethodName_NullFix
{
    private static Exception Finalizer(Exception __exception)
    {
        if (__exception is NullReferenceException)
        {
            // Handle or suppress
            return null; // Suppress
        }
        return __exception; // Re-throw others
    }
}
```

---

## Next Session Tasks

### Immediate Testing

1. **Multiplayer Sync Testing**
   - ⚠️ **CRITICAL**: Test reputation sync with 2+ players
   - Verify server broadcasts reputation changes to all clients
   - Verify joining players receive full reputation state
   - Test that reputation persists across disconnects/rejoins
   - Check for race conditions or desyncs

2. **F7 GUI Polish**
   - Test litpanel material rendering (should show wood texture, not white)
   - Verify text is readable (font size, contrast)
   - Test in different screen resolutions

### Short-Term (Config System Integration)

1. **Wire ConfigManager to existing systems**
   - Replace inline YAML in OdinData.cs with ConfigManager
   - Hook faction system to faction_config.yaml hot-reload
   - Hook quest system to faction_quests.yaml hot-reload
   - Hook item prices to item_values.yaml

2. **Legacy Quest YAML Migration** (OPTIONAL)
   - Move QuestRef hardcoded data to legacy_quests.yaml
   - Load HunterMonsterList, DungeonLoc, TreasureLoc, HuntLoc, SearchItem from file

### Long-Term

1. **Faction Quest Runtime Hooks**
   - Add Harmony patches to detect quest progress (Character.OnDeath, Inventory.AddItem, etc.)
   - Hook FactionQuestManager.UpdateProgress() to game events
   - Make faction quests actually playable (currently definitions load but never execute)

2. **Blueprint Library**
   - Create 10+ ready-to-use blueprints (huts, towers, walls, farms)
   - Community blueprint sharing via YAML files

3. **Cauldron Icons Issue**
   - Investigate why PNGs exist but icons not displaying
   - Check OdinTrader/StoreGui integration

---

## Contact & Support

- **Nexus Mods ID:** 798
- **BepInEx GUID:** `buzz.valheim.OdinPlus`
- **Version:** 0.2.6

---

**Last Updated:** 2026-08-01  
**Status:** Blueprint YAML system complete, visual selector with shader auto-detection working, zero Harmony conflicts with building mods
