# Builder NPC System

## Overview
NPCs at farms can now accept wood and stone donations, then build structures from predefined blueprints.

## How It Works
1. Find a farm location (`/findfarm` command)
2. Locate the BuilderNPCHuman (spawns at farms alongside Material/Message NPCs)
3. Give wood/stone via [E] interact
4. When NPC has enough resources, it auto-starts building
5. One piece placed every 3 seconds until blueprint complete

## Blueprints Implemented

### 1. Wood Hut (40 wood)
- 2x2 floor grid
- Full walls with door
- 45-degree roof
- Compact living space

### 2. Stone Hearth (20 stone)
- 2x2 stone foundation
- Low stone walls around perimeter
- Fire pit in center

### 3. Wood Fence (30 wood)
- 6x6 perimeter
- Open space inside
- Decorative/defensive

### 4. Workshop (35 wood)
- 2x1 floor
- Lean-to style (back wall + partial sides)
- Slanted roof
- Includes workbench

## Prefab Names Used (MAY NEED ADJUSTMENT)

**These are guesses based on Valheim modding conventions. Test in-game and replace with correct names from ZNetScene if pieces don't spawn.**

### Wood Pieces
- `wood_floor_1x1` - 1x1 floor tile
- `wood_wall_roof` - Full-height wall (2m)
- `wood_wall_half` - Half-height wall (1m)
- `wood_door` - Wooden door
- `wood_roof_45` - 45-degree roof piece
- `wood_roof_top_45` - 45-degree roof top ridge
- `wood_fence` - Fence segment

### Stone Pieces
- `stone_floor_2x2` - 2x2 stone floor
- `stone_wall_1x1` - 1x1 stone wall block

### Misc
- `fire_pit` - Campfire/hearth
- `piece_workbench` - Crafting workbench (confirmed correct)

## Testing Steps

1. **Launch game, load world with OdinPlus**
2. **Check BepInEx log** for:
   - `"Blueprints initialized: 4 structures"`
   - `"Create Spawner BuilderNPCHumanSpawner"`
3. **Use `/findfarm`** to reveal nearest WoodFarm1
4. **Travel to farm**, look for builder NPC (spawns at offset (6, 0, 6) from farm center)
5. **Give 40 wood** via [E] interact
6. **Watch for errors** in F5 console:
   - If you see `"BuilderNPC: Prefab 'wood_floor_1x1' not found"`, note the EXACT error message
   - The prefab name case/format is wrong

## How to Fix Prefab Names

### Option 1: Use RuntimeUnityEditor
1. Press F12 in-game (if RuntimeUnityEditor is installed)
2. Navigate to Object Browser → ZNetScene → m_namedPrefabs
3. Search for "wood" or "floor" to find correct names
4. Update `BuilderNPC.cs` line 170+ with correct names

### Option 2: Use Log Debugging
Add this to `Blueprints.Init()` to dump all available prefabs:
```csharp
foreach (var prefab in ZNetScene.instance.m_prefabs)
{
    if (prefab.name.Contains("wood") || prefab.name.Contains("stone"))
        DBG.blogInfo($"Available: {prefab.name}");
}
```

### Option 3: Check Valheim Wiki/Modding Docs
- Valheim Wiki has lists of building pieces
- JotunnLib documentation lists piece names
- PlanBuild mod source code has comprehensive lists

## Future Enhancements
- **Faction-based blueprints**: RedTeam builds viking longhouses, BlueTeam builds stone towers
- **Progressive unlocks**: Give more resources → bigger structures unlock
- **Dynamic placement**: NPC builds near its home point, avoids overlapping existing structures
- **Resource pooling**: Multiple NPCs share village resource pool
- **Repair mode**: NPC auto-repairs damaged nearby structures using its resource pool

## Files Modified
- `OdinPlus/6Humans/BuilderNPC.cs` - New builder component + blueprint system
- `OdinPlus/6Humans/HumanManager.cs` - Spawns BuilderNPCHuman at farms, initializes blueprints
- `OdinPlus/OdinPlus.csproj` - Added BuilderNPC.cs to compile list

## Known Issues
- **Prefab names unverified** - These are educated guesses, NOT tested against actual game
- **No collision detection** - Pieces may overlap terrain/rocks/trees
- **No persistence** - If NPC dies mid-build, progress is lost (built pieces persist as world objects)
- **No snapping** - Pieces placed at absolute positions, may have gaps if Valheim's grid doesn't align

## Next Steps
1. Test in-game
2. Get BepInEx log output when builder tries to place first piece
3. Correct prefab names in `BuilderNPC.cs` lines 170-285
4. Adjust blueprint piece positions/rotations if structures look wrong
5. Add more blueprints (stone tower, viking hall, barn, etc.)
