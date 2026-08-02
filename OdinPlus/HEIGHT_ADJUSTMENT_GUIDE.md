# Height Adjustment - Visual Blueprint Selector

## NEW: Scroll to Adjust Box Height!

You can now **scroll your mouse wheel** to extend the selection box vertically, ensuring you capture entire multi-story buildings or just one floor.

---

## How It Works

### Step 1-2: Place Corners (Same as Before)

1. `/selectblueprint MyBuilding`
2. Click Corner 1 (green cylinder)
3. Click Corner 2 (red cylinder)
4. Transparent cyan box appears

### Step 3: **NEW** - Adjust Height

**Scroll mouse wheel UP/DOWN** to extend the box vertically:

- **Scroll UP** (away from you): Box gets **taller** (+2m per tick)
- **Scroll DOWN** (toward you): Box gets **shorter** (-2m per tick)

**The box updates in real-time!**

**Top-left shows:** `Height: +8.0m` (or whatever adjustment you've made)

### Step 4: Confirm

Click to scan with the adjusted height.

---

## Use Cases

### Capture Entire Multi-Story Building

**Problem:** Your building has 3 floors, but corners are on ground level.

**Solution:**
```
1. Place Corner 1 at ground floor corner
2. Place Corner 2 at opposite ground floor corner
3. Scroll UP several times (watch box extend upward)
4. Keep scrolling until box covers all 3 floors + roof
5. Confirm
```

**Result:** Scans all floors in one blueprint!

### Capture Only Ground Floor

**Problem:** Multi-story building, but you only want the ground floor.

**Solution:**
```
1. Place Corner 1 at ground floor corner
2. Place Corner 2 at opposite ground floor corner
3. Box auto-sized to those corners (might include some of floor 2)
4. Scroll DOWN to shrink box vertically
5. Box now only covers ground floor height
6. Confirm
```

**Result:** Scans only ground floor pieces!

### Capture Underground/Basement

**Problem:** You have a basement or pieces below ground.

**Solution:**
```
1. Place Corner 1 at basement floor
2. Place Corner 2 at opposite corner (maybe ground level)
3. Scroll DOWN to extend box below ground
4. Box now reaches basement floor
5. Confirm
```

**Result:** Includes basement pieces!

---

## Visual Indicators

### Cyan Box Extends

As you scroll, watch the box:
- Top face moves up/down
- Bottom face moves down/up
- Box stays centered on your corner markers

### Height Display

Top-left corner shows:
```
Blueprint: MyHouse
Height: +4.0m
[Scroll] Adjust Height
[LMB] Confirm
```

**Height value:**
- `+0.0m` = Default (just corner height difference)
- `+8.0m` = Box extended 8m taller (4m up, 4m down from corners)
- `-4.0m` = Box shrunk 4m shorter (2m down from top, 2m up from bottom)

---

## Tips

### Start Small, Then Expand

1. Place corners at opposite ends (horizontal)
2. Scroll UP gradually
3. Watch the box grow until it covers your structure
4. Stop when you see all pieces included

### Check from Different Angles

- **Walk around** while scrolling
- **Fly/jump** to see from above
- Make sure box isn't cutting off roof or basement

### Precision Scrolling

Each scroll tick = **±2 meters**

For a typical Valheim structure:
- **1 floor** = ~3m tall → Default box (no scroll)
- **2 floors** = ~6m tall → Scroll UP 2-3 times
- **3 floors** = ~9m tall → Scroll UP 4-5 times
- **Tower** = 15m+ tall → Scroll UP 7-10 times

---

## Comparison to Old Method

### Before (Console Scanner)

```
/scanblueprint Tower 15
```

**Problems:**
- Circular radius (not rectangular)
- Can't control height separately
- Must stand at exact center

### After (Visual Selector + Height Adjustment)

```
/selectblueprint Tower
(Click corners at base)
(Scroll UP to cover all floors)
(Confirm)
```

**Benefits:**
- ✅ Rectangular selection
- ✅ Independent height control
- ✅ Visual feedback
- ✅ Stand anywhere

---

## Examples

### Example 1: 3-Story House

```
Goal: Capture all 3 floors + roof

1. /selectblueprint BigHouse
2. Click front-left ground corner
3. Click back-right ground corner
4. Box appears (only covers ~1 floor)
5. Scroll UP 6 times
6. Box now extends to roof
7. Top-left shows "Height: +12.0m"
8. Click to confirm
9. "Scanned 180 pieces!"
```

### Example 2: Flat Ground-Floor Base

```
Goal: Just the ground floor, no loft

1. /selectblueprint Barracks
2. Click corner 1 (ground)
3. Click corner 2 (ground)
4. Box auto-sized (might include beams above)
5. Scroll DOWN 2 times
6. Box now only 2-3m tall
7. Top-left shows "Height: -4.0m"
8. Click to confirm
9. "Scanned 40 pieces!"
```

### Example 3: Tower with Deep Foundation

```
Goal: Include foundation stones below ground

1. /selectblueprint DefenseTower
2. Click corner 1 at base (on ground)
3. Click corner 2 at opposite base
4. Scroll DOWN 3 times (extends below ground)
5. Then scroll UP 8 times (extends to top of tower)
6. Box now covers foundation to battlements
7. Top-left shows "Height: +10.0m"
8. Click to confirm
9. "Scanned 95 pieces!"
```

---

## Troubleshooting

### Box doesn't extend when I scroll

**Cause:** You only have 1 corner placed, or no preview box yet.

**Fix:** Place both corners first (green + red cylinders), THEN scroll.

### Box extends in wrong direction

**Cause:** Height adjustment affects both top AND bottom of box equally.

**Fix:** Scroll the opposite direction. If box goes too low, scroll UP.

### Can't see height number

**Cause:** Looking in wrong part of screen.

**Fix:** Check **top-left corner** of screen, not center.

### Box is flickering/disappearing

**Cause:** Scrolling too fast, Unity rendering issue.

**Fix:** Scroll slower. If it disappears, press RMB to clear Corner 2, then replace it.

---

## BuilderNPC Material Requirements

**Good news:** Builder NPCs already show what they need!

When you talk to a Builder NPC:

```
Builder: "I can build once you give me:
Wood: 20/100
Stone: 0/40"
```

This shows:
- **Current resources** they have
- **Required resources** for the next blueprint

**The requirements come from the blueprint you scanned!**

When you scanned and it said:
```
Wood cost: 100
Stone cost: 40
```

That's what the Builder will ask for. No manual editing needed!

---

## Summary

### New Controls

| Action | Effect |
|--------|--------|
| **Scroll UP** | Extend box vertically (+2m per tick) |
| **Scroll DOWN** | Shrink box vertically (-2m per tick) |

### Workflow

1. Place 2 corners (defines horizontal area)
2. **Scroll** to adjust vertical height
3. Confirm (scans pieces in adjusted box)
4. Builder NPC shows requirements from scanned costs

---

**Now you have full 3D control over blueprint selection!**
