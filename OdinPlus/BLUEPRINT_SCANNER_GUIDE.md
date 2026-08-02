# Blueprint Scanner - Complete User Guide

## How to Create Blueprints for Builder NPCs

The Blueprint Scanner tool lets you build structures in-game with your hammer, then scan them into C# code that Builder NPCs can reconstruct.

---

## Quick Start

1. **Build** a structure with your hammer (2x2 floor, walls, door, roof)
2. **Stand** at the center when done
3. **Open console** (F5) and type: `/scanblueprint MyHut 10`
4. **Check** `BepInEx/plugins/blueprint_MyHut.txt` for the generated code
5. **Copy** the code and add it to `BuilderNPC.cs` in the `Blueprints.Init()` method
6. **Rebuild** the mod and deploy

---

## Console Commands

### `/previewscan <radius>`
Preview the scan area with yellow spheres (lasts 10 seconds).

**Example:**
```
/previewscan 10
```

Shows yellow spheres around all pieces within 10 meters. Use this to:
- Check if your radius is correct
- Verify all pieces will be captured
- Adjust your standing position if needed

### `/scanblueprint <name> <radius> [woodCost] [stoneCost]`
Scan all pieces within radius and generate C# blueprint code.

**Parameters:**
- `name` - Blueprint name (e.g. "SmallHut", "Tower", "Farm")
- `radius` - Scan radius in meters (default: 10)
- `woodCost` - Optional: wood cost override (default: auto-calculated)
- `stoneCost` - Optional: stone cost override (default: auto-calculated)

**Examples:**
```
/scanblueprint SmallHut 10
/scanblueprint Tower 15 100 50
/scanblueprint Farm 20 200
```

**Auto-Cost Calculation:**
If you omit wood/stone costs, the scanner calculates them automatically:
- Wood pieces: 2 wood per piece
- Stone pieces: 4 stone per piece
- Total = (wood pieces × 2) + (stone pieces × 4)

---

## Step-by-Step Workflow

### Step 1: Plan Your Structure

**Good Blueprint Sizes:**
- **Small Hut:** 2x2 floor, 4 walls, door, pitched roof (10m radius)
- **Medium House:** 4x4 floor, walls, windows, complex roof (15m radius)
- **Large Building:** 6x6+ floor, multiple rooms (20m radius)

**Keep It Simple:**
- NPCs build slowly (1 piece every 3 seconds)
- Players will watch the build animation
- 20-50 pieces = good size (60-150 seconds build time)
- 100+ pieces = very slow (5+ minutes)

### Step 2: Build with Hammer

**Important:**
- Build on **flat ground** - uneven terrain causes height mismatches
- Use **normal building pieces** (wood, stone, etc.) - no modded pieces
- **Don't place** workbenches, chests, or other interactive objects (scanner ignores them)
- Build **complete structures** - missing walls look bad when NPC rebuilds

**Recommended Pieces:**
```
Floors:     wood_floor_1x1, wood_floor
Walls:      wood_wall_roof, wood_wall_half
Roofs:      wood_roof, wood_roof_45, wood_roof_top
Doors:      wood_door
Windows:    wood_window
Beams:      wood_beam, wood_beam_45
Stairs:     wood_stair
```

### Step 3: Preview the Scan

**Stand at the center** of your structure (where you want the NPC to be when building).

Run:
```
/previewscan 10
```

You should see:
- Yellow spheres appear around ALL pieces you built
- No spheres on nearby trees/rocks/terrain (scanner only grabs Piece components)

**Adjust radius if needed:**
```
/previewscan 15   (bigger radius)
/previewscan 8    (smaller radius)
```

### Step 4: Run the Scan

Still standing at center:
```
/scanblueprint MyHut 10
```

**You'll see:**
- Console message: "Scanned X pieces! Saved to: blueprint_MyHut.txt"
- On-screen center message with piece count
- BepInEx log shows full blueprint code

**If you see "No pieces found":**
- Increase radius
- Stand closer to your structure
- Check that you built with normal pieces (not modded)

### Step 5: Get the Generated Code

**Two ways to access:**

**A) From file:**
```
Open: C:\Program Files (x86)\Steam\steamapps\common\Valheim\BepInEx\plugins\blueprint_MyHut.txt
```

**B) From BepInEx log:**
```
Open: C:\Users\<YOU>\AppData\LocalLow\IronGate\Valheim\Player.log
Search for: "=== BLUEPRINT CODE ==="
```

**The code looks like:**
```csharp
// Blueprint: MyHut
// Generated from 24 in-game pieces
// Scanned at: 2026-08-01 09:30:15
All.Add(new Blueprint(
    "MyHut",
    new Dictionary<string, int> { { "Wood", 48 } },
    new BlueprintPiece[]
    {
        new BlueprintPiece("wood_floor_1x1", new Vector3(0f, 0f, 0f), Vector3.zero),
        new BlueprintPiece("wood_floor_1x1", new Vector3(2f, 0f, 0f), Vector3.zero),
        // ... more pieces
    }
));
```

### Step 6: Add to Mod Code

**Edit:** `c:\Users\zarboz\Desktop\valhoom\buzzvalheimmods\OdinPlus\6Humans\BuilderNPC.cs`

**Find** the `Blueprints.Init()` method (around line 200):

```csharp
public static void Init()
{
    All = new List<Blueprint>();
    
    // Paste your blueprint code here:
    All.Add(new Blueprint(
        "MyHut",
        new Dictionary<string, int> { { "Wood", 48 } },
        new BlueprintPiece[]
        {
            new BlueprintPiece("wood_floor_1x1", new Vector3(0f, 0f, 0f), Vector3.zero),
            // ... rest of pieces
        }
    ));
    
    // Add more blueprints...
}
```

### Step 7: Rebuild and Test

**PowerShell:**
```powershell
cd "c:\Users\zarboz\Desktop\valhoom\buzzvalheimmods\OdinPlus"
dotnet build -c Release

# Merge and deploy (if Valheim is closed):
MSYS_NO_PATHCONV=1 "..\ILRepack.exe" /internalize /lib:"..\0libDep" /out:"bin\Debug\OdinPlus.merged.dll" "bin\Debug\OdinPlus.dll" "bin\Debug\YamlDotNet.dll"
cp "bin\Debug\OdinPlus.merged.dll" "C:\Program Files (x86)\Steam\steamapps\common\Valheim\BepInEx\plugins\OdinPlus.dll"
```

**In-Game Test:**
1. Start Valheim
2. Run `/findfarm` to find a WoodFarm1 location
3. Go there, find BuilderNPCHuman
4. Give wood (e.g. 50 wood for a 48-wood blueprint)
5. NPC says "I'll start building a MyHut!"
6. Watch it construct piece-by-piece

---

## Tips & Tricks

### Perfect Alignment

**Problem:** NPC builds the structure offset from where you want it.

**Solution:** The build origin is set when you give the NPC resources. Stand at the exact spot you want the NPC to build, give resources, then watch where it walks. The structure will appear ~5m in front of the NPC's current position when it starts building.

### Resource Costs

**Auto-calculated costs:**
- Scanner counts wood vs stone pieces
- Wood pieces = 2 wood each
- Stone pieces = 4 stone each

**Override costs:**
If the auto-cost is wrong:
```
/scanblueprint Tower 15 100 50
```
(Force 100 wood, 50 stone regardless of piece count)

**Why override?**
- You want to make it cheaper/expensive
- You added decorative pieces the scanner didn't catch
- You want to balance blueprint difficulty

### Multiple Blueprints

Builder NPCs build **in order** from the `Blueprints.All` list.

**Example:**
```csharp
All.Add(new Blueprint("SmallHut", ...));     // Builds first
All.Add(new Blueprint("MediumHouse", ...));  // Builds second
All.Add(new Blueprint("Tower", ...));        // Builds third
```

NPC checks resources, builds first affordable blueprint, then moves to next.

### Build Time Estimates

| Pieces | Build Time | Good For |
|--------|------------|----------|
| 10-20  | 30-60s     | Decoration, small shed |
| 20-40  | 1-2 min    | Small hut, outpost |
| 40-60  | 2-3 min    | Medium house |
| 60-100 | 3-5 min    | Large house, tower |
| 100+   | 5+ min     | Castle, complex (patience required!) |

### Troubleshooting

**"No pieces found within Xm"**
- Increase radius: `/scanblueprint Name 20`
- Stand closer to structure
- Check you built with normal pieces (not modded)

**"Blueprint builds in wrong location"**
- Build origin is set when NPC receives resources
- NPC builds ~5m in front of its position at that moment
- Position yourself near where you want the build before giving resources

**"Some pieces missing from scan"**
- Increase radius to capture entire structure
- Use `/previewscan` to verify all pieces have yellow spheres
- Scanner only captures `Piece` components (skips workbenches, chests, etc.)

**"NPC builds pieces underground/floating"**
- Build on flat terrain when scanning
- Scanner uses Y position from your scan - uneven ground causes mismatches
- Flatten terrain with hoe before building original structure

**"Resource costs seem wrong"**
- Auto-calc: wood pieces × 2, stone pieces × 4
- Override with custom costs: `/scanblueprint Name 10 100 50`
- Check generated code in .txt file to see piece breakdown

---

## Advanced: Editing Blueprints

**Manually adjust positions:**
```csharp
new BlueprintPiece("wood_floor_1x1", new Vector3(2f, 0f, 0f), Vector3.zero),
                                                 // ^ Change X/Y/Z
```

**Rotate pieces:**
```csharp
new BlueprintPiece("wood_wall_roof", new Vector3(0f, 0f, 2f), new Vector3(0f, 90f, 0f)),
                                                                        // ^ Y rotation
```

**Change piece types:**
```csharp
// Change floor type:
new BlueprintPiece("stone_floor", new Vector3(0f, 0f, 0f), Vector3.zero),
                   // ^ was "wood_floor_1x1"
```

**Remove pieces:**
Just delete the line from the array.

**Add pieces:**
Add a new `new BlueprintPiece(...)` line to the array.

---

## Example Blueprints

### Tiny Shed (8 pieces, 16 wood)
```csharp
All.Add(new Blueprint(
    "TinyShed",
    new Dictionary<string, int> { { "Wood", 16 } },
    new BlueprintPiece[]
    {
        new BlueprintPiece("wood_floor_1x1", new Vector3(0f, 0f, 0f), Vector3.zero),
        new BlueprintPiece("wood_floor_1x1", new Vector3(2f, 0f, 0f), Vector3.zero),
        new BlueprintPiece("wood_wall_half", new Vector3(-1f, 0f, 1f), new Vector3(0f, 90f, 0f)),
        new BlueprintPiece("wood_wall_half", new Vector3(3f, 0f, 1f), new Vector3(0f, 270f, 0f)),
        new BlueprintPiece("wood_wall_half", new Vector3(1f, 0f, 2f), new Vector3(0f, 180f, 0f)),
        new BlueprintPiece("wood_door", new Vector3(1f, 0f, 0f), new Vector3(0f, 0f, 0f)),
        new BlueprintPiece("wood_roof_45", new Vector3(1f, 2f, 1f), new Vector3(0f, 0f, 0f)),
        new BlueprintPiece("wood_roof_45", new Vector3(1f, 2f, 1f), new Vector3(0f, 180f, 0f))
    }
));
```

### Guard Tower (40 pieces, 80 wood)
```csharp
// Build a 2x2 tower with ladder and platform on top
// Scan from ground level at base center
/scanblueprint GuardTower 10
```

### Farm House (60 pieces, 120 wood)
```csharp
// Build a 4x4 house with windows, interior walls, loft
// Scan from center of ground floor
/scanblueprint FarmHouse 15
```

---

**Happy Building!**
