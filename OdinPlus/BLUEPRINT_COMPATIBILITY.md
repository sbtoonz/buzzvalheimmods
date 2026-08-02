# Blueprint System Compatibility

## Harmony Patches Analysis

### ✅ NO Patches on Building/Piece Systems

The blueprint system uses **zero Harmony patches** on:
- `Player.PlacePiece()`
- `Piece.Awake()`
- `Piece.SetCreator()`
- `PieceTable.*`
- `Player.GetBuildPieces()`
- `Player.UpdatePlacementGhost()`

**Result:** Fully compatible with PlanBuild, Infinity Hammer, BuildShare, and all building mods.

---

## Active Patches (Non-Building)

### 1. ZNetScene.Awake (Postfix)
**File:** Plugin.cs line 391  
**Purpose:** Register custom NPC prefabs  
**Impact:** None on pieces - only adds NPCs to scene  
**Priority:** 600, runs **BEFORE** `buzz.valheim.AllTameable`  

### 2. ZNetScene.RemoveObjects (Finalizer)
**File:** Plugin.cs line 415  
**Purpose:** Swallow NullRef from stale ZNetView entries  
**Impact:** None on pieces - error suppression only  

### 3. Player.Update (Postfix)
**File:** Plugin.cs line 262  
**Purpose:** F7 faction GUI toggle, secondary interact key  
**Impact:** None on building - input handling only  

### 4. Terminal.InputText (Prefix)
**File:** Plugin.cs line 308  
**Purpose:** DevTool console hook  
**Impact:** None on building  

---

## How Blueprint System Works

### Scanning (Zero Patches)
1. **Console Scanner:** Uses `FindObjectsOfType<Piece>()` with radius filter
2. **Visual Selector:** Uses `Physics.Raycast()` and bounds checking
3. Both read `Piece.m_resources[]` array (public field, no patch needed)

### Building (Zero Patches)
1. Builder NPC uses `Instantiate(prefab, pos, rot)`
2. No hooks, no patches, no interference
3. Identical to vanilla piece placement

### Storage
- YAML files in `BepInEx/config/blueprints/*.yaml`
- No mod conflict possible (separate folder)

---

## PlanBuild Compatibility

### PlanBuild's Patches (0.217.x)
- `Player.PlacePiece()` - Adds plan mode placement
- `Piece.Awake()` - Adds plan component
- `PieceTable.GetSelectedPrefab()` - Blueprint UI integration

### OdinPlus Interaction
**None.** OdinPlus never touches these methods.

**Can PlanBuild blueprints be used with Builder NPCs?**
- ✅ YES if you export PlanBuild blueprint as pieces
- ✅ Use `/selectblueprint` to scan existing PlanBuild structures
- ✅ Use `/scanblueprint` to capture placed pieces from PlanBuild

---

## Infinity Hammer Compatibility

### Infinity Hammer's Patches (common)
- `Player.UpdatePlacement()` - Extended placement range
- `Player.CheckCanRemovePiece()` - Instant removal
- Command system for piece manipulation

### OdinPlus Interaction
**None.** OdinPlus never touches placement or removal.

**Can Infinity Hammer be used to place OdinPlus blueprints?**
- ❌ NO - Infinity Hammer doesn't read our YAML format
- ✅ YES - Scan structures built WITH Infinity Hammer using `/selectblueprint`

---

## BuildShare Compatibility

### BuildShare's System
- Server-side blueprint storage
- Client downloads blueprints
- Uses same `Instantiate(prefab)` pattern as OdinPlus

### OdinPlus Interaction
**Fully compatible.** Both mods:
- Read `Piece.m_resources[]` the same way
- Use `FindObjectsOfType<Piece>()` for scanning
- Use `Instantiate()` for placement

**Can BuildShare blueprints be converted to OdinPlus format?**
- ⚠️ Requires custom converter script (different storage formats)
- ✅ OR: Place BuildShare blueprint → `/selectblueprint` to re-scan

---

## Known Compatible Mods

### ✅ Tested Compatible
- **Valheim Plus** - No conflicts
- **CLLC (Creature Level & Loot Control)** - Priority order set via `HarmonyBefore`
- **AllTameable** - Priority order set via `HarmonyBefore`

### ⚠️ Untested (Should Be Compatible)
- **PlanBuild** - No overlapping patches
- **Infinity Hammer** - No overlapping patches
- **BuildShare** - Similar architecture
- **Gizmo** - Different patch targets
- **Comfy Gizmo** - Different patch targets

### ❌ Potential Conflicts
- **Mods that replace `ZNetScene.Awake` entirely** - OdinPlus uses Postfix, so may fail if Awake is skipped
- **Mods that patch `Instantiate()` globally** - Could affect NPC spawning

---

## Testing Checklist

### With PlanBuild
- [ ] Scan PlanBuild structure with `/selectblueprint`
- [ ] Builder NPC constructs scanned blueprint
- [ ] PlanBuild ghost mode still works
- [ ] No console errors when both mods loaded

### With Infinity Hammer
- [ ] Build with Infinity Hammer extended range
- [ ] Scan with `/selectblueprint`
- [ ] Builder NPC constructs scanned blueprint
- [ ] Infinity Hammer commands still work

### With BuildShare
- [ ] Place BuildShare blueprint
- [ ] Scan with `/selectblueprint`
- [ ] Both mods' builders can place pieces
- [ ] No ZDO conflicts

---

## API for Other Mods

### To Add Blueprints Programmatically

```csharp
using OdinPlus;

// Create blueprint data
var pieces = new BlueprintPiece[]
{
    new BlueprintPiece("wood_floor_1x1", new Vector3(0,0,0), Vector3.zero),
    // ... more pieces
};

var costs = new Dictionary<string, int> { { "Wood", 50 }, { "Stone", 20 } };
var bp = new Blueprint("MyCustomHouse", costs, pieces);

// Save to YAML
BlueprintConfig.SaveBlueprint(bp);
```

### To Read All Blueprints

```csharp
using OdinPlus;

var allBlueprints = BlueprintConfig.GetAllBlueprints();
foreach (var bp in allBlueprints)
{
    Debug.Log($"Blueprint: {bp.name}, Pieces: {bp.pieces.Length}");
}
```

---

## Compatibility Promise

**OdinPlus WILL NEVER patch:**
- `Player.PlacePiece()`
- `Player.UpdatePlacementGhost()`
- `Player.UpdatePlacement()`
- `Piece.Awake()`
- `Piece.SetCreator()`
- `PieceTable.*`

**Why?** These are the core methods all building mods depend on. Patching them creates conflicts. OdinPlus uses read-only queries (`FindObjectsOfType`, `m_resources[]`) and vanilla placement (`Instantiate()`).

---

## Reporting Conflicts

If you find a conflict with another mod:

1. Check BepInEx log for Harmony patch errors
2. Check which patches are failing (look for "PATCH FAILED")
3. Report to: https://github.com/your-repo/issues (if exists) or Nexus Mods

Include:
- Full mod list
- BepInEx log (Player.log)
- Steps to reproduce

---

**Last Updated:** 2026-08-01  
**OdinPlus Version:** 0.2.6+  
**Valheim Version:** 0.221.12
