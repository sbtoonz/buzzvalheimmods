# Visual Blueprint Selector - User Guide

## Like PlanBuild & Infinity Hammer!

The visual blueprint selector lets you click to place corner markers and visually define a zone to scan, just like PlanBuild and Infinity Hammer's blueprint systems.

---

## Quick Start

1. Build a structure with your hammer
2. Open console (F5) and type: `/selectblueprint MyHut`
3. **Click** where you want Corner 1 (a green cylinder appears)
4. **Click** where you want Corner 2 (a red cylinder appears)
5. A transparent cyan box shows the selection zone
6. **Click again** to confirm and scan
7. Blueprint saved to `BepInEx/plugins/blueprint_MyHut.txt`

---

## How It Works

### Step 1: Build Your Structure

Build normally with your hammer - any pieces you want the builder NPC to reconstruct.

**Example:**
```
- Floor: 4x4 wood floors
- Walls: Wood walls around perimeter
- Door: On one side
- Roof: Pitched roof
- Windows: A few windows
```

### Step 2: Start Selection Mode

Open console (F5):
```
/selectblueprint MyHouseName
```

**You'll see:**
- Top-left message: "Blueprint: MyHouseName"
- Instructions: "[LMB] Place Corner 1 | [Esc] Cancel"

### Step 3: Place Corner 1

**Look at one corner** of your structure (e.g. front-left floor corner).

**Click** (Left Mouse Button).

**You'll see:**
- A **green cylinder** appears at that spot (tall, 2m high)
- Instructions update: "[LMB] Place Corner 2 | [RMB] Clear | [Esc] Cancel"

### Step 4: Place Corner 2

**Look at the opposite corner** (e.g. back-right roof corner).

**Click** (Left Mouse Button).

**You'll see:**
- A **red cylinder** appears at that spot
- A **transparent cyan box** fills the entire selection zone
- Instructions: "[LMB] Confirm Scan | [RMB] Clear | [Esc] Cancel"

**The box shows exactly what will be scanned!**

### Step 5: Confirm or Adjust

**If the box looks good:**
- **Click** to confirm and scan

**If you need to adjust:**
- Press **Right Mouse Button** to remove Corner 2 and re-place it
- Press **Esc** to cancel and start over

### Step 6: Scan Complete

Once you click to confirm:
- System scans all pieces inside the box
- Calculates wood/stone costs automatically
- Generates C# blueprint code
- Saves to `BepInEx/plugins/blueprint_MyHouseName.txt`

**You'll see:**
```
"Scanned 42 pieces!
Saved to: blueprint_MyHouseName.txt"
```

---

## Controls Reference

| Key | Action |
|-----|--------|
| **F5** | Open console |
| **LMB** (Left Click) | Place corner marker / Confirm scan |
| **RMB** (Right Click) | Remove last marker (go back) |
| **Esc** | Cancel selection mode |

---

## Visual Indicators

### Green Cylinder
- Your **first corner marker**
- Defines one corner of the selection box

### Red Cylinder
- Your **second corner marker**
- Defines the opposite corner

### Cyan Transparent Box
- **Selection zone** - shows exactly what will be scanned
- All pieces inside this box will be captured
- Extends from Corner 1 to Corner 2 in X/Y/Z

---

## Tips & Best Practices

### Placement Strategy

**Good:**
```
Corner 1: Bottom-front-left floor
Corner 2: Top-back-right roof
```
This captures the entire structure from ground to rooftop.

**Also Good:**
```
Corner 1: Any floor corner
Corner 2: Opposite floor corner (same height)
```
This captures one floor/level at a time.

**Avoid:**
```
Corner 1: Inside the structure
Corner 2: Far outside the structure
```
You'll capture extra terrain pieces or nearby trees.

### Selection Box Height

The box extends in Y (vertical) between your two corner heights:
- If both corners are on the ground → flat box, captures only ground floor
- If Corner 1 is ground, Corner 2 is roof → tall box, captures whole building
- You can place corners at any height (climb ladders, jump, fly)

### Scanning Large Structures

For huge builds:
1. Stand far back to see the whole thing
2. Zoom in with mouse wheel if needed
3. Place Corner 1 at one extreme
4. Walk/fly to the opposite extreme
5. Place Corner 2

The transparent box will stretch across the entire distance.

### Multi-Story Buildings

**Option A: Capture all floors at once**
- Corner 1: Ground floor
- Corner 2: Top floor
- Result: Entire building in one blueprint

**Option B: Capture floors separately**
- `/selectblueprint House_Floor1` → scan ground floor
- `/selectblueprint House_Floor2` → scan second floor
- `/selectblueprint House_Floor3` → scan third floor
- Result: Three separate blueprints (Builder builds them in order)

---

## Comparison to Other Methods

### Visual Selector (This Tool)

**Pros:**
- ✅ Click to select, like PlanBuild/Infinity Hammer
- ✅ Visual box shows exactly what you'll get
- ✅ Can select irregularly shaped areas
- ✅ Height control (ground floor only vs entire building)
- ✅ No need to stand at center

**Cons:**
- ❌ Requires 2 clicks + confirm (3 total)

### Console Radius Scanner (`/scanblueprint`)

**Pros:**
- ✅ Fast (one command)
- ✅ Good for circular structures

**Cons:**
- ❌ Must stand at exact center
- ❌ Hard to visualize radius
- ❌ Circular selection (not rectangular)
- ❌ May capture unwanted nearby objects

### Preview Scanner (`/previewscan`)

**Pros:**
- ✅ Shows yellow spheres on all pieces
- ✅ Good for checking radius before full scan

**Cons:**
- ❌ Still uses circular radius
- ❌ Spheres disappear after 5 seconds

---

## Examples

### Small Hut (2x2)

```
1. Build: 2x2 floor, 4 walls, door, roof
2. F5 → /selectblueprint SmallHut
3. Click front-left floor corner
4. Click back-right roof top
5. Confirm
```

**Result:**
```
Scanned 18 pieces!
Wood cost: 36
```

### Large House (6x6 with loft)

```
1. Build: 6x6 floor, walls, loft, stairs, windows
2. F5 → /selectblueprint LargeHouse
3. Click one ground corner
4. Climb to loft, click opposite corner
5. Confirm
```

**Result:**
```
Scanned 94 pieces!
Wood cost: 188, Stone cost: 12
```

### Wall Segment

```
1. Build: Long wall with battlements
2. F5 → /selectblueprint DefenseWall
3. Click left end of wall
4. Click right end of wall (same height)
5. Confirm
```

**Result:**
```
Scanned 45 pieces!
Wood cost: 90
```

---

## Troubleshooting

### "No pieces found in selection!"

**Cause:** The box is empty or doesn't contain any valid pieces.

**Fix:**
- Press Esc, restart selection
- Place corners closer to the actual structure
- Make sure box overlaps your building

### Markers appear underground

**Cause:** Raycast hit terrain below surface.

**Fix:**
- Click on solid surfaces (floors, walls)
- Don't click on thin air
- Adjust camera angle if needed

### Box is too small/large

**Cause:** Corners placed wrong.

**Fix:**
- Press **RMB** to remove Corner 2
- Re-place it at correct position
- OR press **Esc** to restart

### Scanned extra terrain/rocks

**Cause:** Box extends too far beyond structure.

**Fix:**
- Be more precise with corner placement
- Place corners closer to building edges
- Use `/selectblueprint` again with tighter corners

### Can't see the cyan box

**Cause:**
- Graphics settings too low
- Box is very small
- Camera angle

**Fix:**
- Zoom out to see the whole box
- Increase graphics settings
- The box is semi-transparent, look for cyan tint

---

## Advanced: Combining with Console Commands

### Workflow A: Visual select, then check costs

```
1. /selectblueprint MyHouse
2. (Place corners, scan)
3. Open blueprint_MyHouse.txt
4. Check costs: Wood: 120, Stone: 40
5. Decide if you want to adjust
```

### Workflow B: Preview first, then visual select

```
1. /previewscan 15  (yellow spheres show pieces)
2. /selectblueprint MyHouse  (visual selection)
3. Place corners based on where spheres appeared
```

### Workflow C: Use both methods

```
1. /scanblueprint QuickScan 10  (fast circular scan for reference)
2. Check piece count
3. /selectblueprint RefinedScan  (precise rectangular scan)
4. Use the refined version
```

---

## What Gets Scanned?

### ✅ Included

- All `Piece` components (floors, walls, roofs, doors, etc.)
- Wood, stone, and other building pieces
- Stairs, ladders, beams
- Decorative pieces (poles, fences)

### ❌ Excluded

- Workbenches, forges, crafting stations
- Chests and storage
- Beds, chairs, furniture
- Lights, torches, fires
- Trees, rocks, terrain
- Other players' structures (if not within box)

**Why?**
The scanner only captures `Piece` components. Interactive objects like workbenches use different components and are skipped.

---

## After Scanning

### Step 1: Get the Code

Open:
```
C:\Program Files (x86)\Steam\steamapps\common\Valheim\BepInEx\plugins\blueprint_MyHouseName.txt
```

### Step 2: Copy to Mod

Edit `BuilderNPC.cs`, find `Blueprints.Init()`, paste:

```csharp
All.Add(new Blueprint(
    "MyHouse",
    new Dictionary<string, int> { { "Wood", 120 }, { "Stone", 40 } },
    new BlueprintPiece[]
    {
        new BlueprintPiece("wood_floor", new Vector3(0f, 0f, 0f), Vector3.zero),
        // ... rest of pieces
    }
));
```

### Step 3: Rebuild & Deploy

```powershell
cd "c:\Users\zarboz\Desktop\valhoom\buzzvalheimmods\OdinPlus"
dotnet build
# ... ILRepack and deploy
```

### Step 4: Test In-Game

1. Find a Builder NPC
2. Give wood/stone (match blueprint costs)
3. NPC says "I'll start building a MyHouse!"
4. Watch it construct your scanned structure

---

## FAQ

**Q: Can I scan someone else's structure?**  
A: Yes, if you have permission. The scanner works on any pieces within the selection box, regardless of ownership.

**Q: Can I edit the blueprint after scanning?**  
A: Yes! The `.txt` file is C# code. You can manually adjust positions, rotations, or remove/add pieces before pasting into `BuilderNPC.cs`.

**Q: Can I scan multiple buildings into one blueprint?**  
A: Yes. Make the selection box large enough to cover all buildings you want. They'll all be captured as one blueprint.

**Q: Does this work with modded building pieces?**  
A: Yes, as long as the piece prefab names are valid. The scanner captures whatever pieces exist in the selection box.

**Q: Can I share blueprints with friends?**  
A: Yes. Send them the `.txt` file. They paste it into their own `BuilderNPC.cs` and rebuild the mod.

---

**Enjoy building with the visual selector!**

*Like PlanBuild and Infinity Hammer, but for Builder NPCs!*
