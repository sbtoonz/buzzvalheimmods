# OdinPlus - Valheim 0.221.12 Upgrade COMPLETE ✅

**Date**: 2026-07-30  
**Status**: ✅ **BUILD SUCCESSFUL - READY FOR TESTING**  
**Output**: `OdinPlus.dll` (353 KB) deployed to `BepInEx\plugins\`

---

## Summary

Successfully updated OdinPlus mod from Valheim 0.217.x to **Valheim 0.221.12** (Unity 6000.0.61).

**Build Result**: ✅ Compiled successfully with 0 errors, 19 warnings (non-breaking)

---

## All Fixed Issues

### 1. ✅ Console.InputText → Terminal.InputText
**File**: [0Main/Plugin.cs:195](0Main/Plugin.cs)  
**Change**: Patch target changed from `Console.InputText` to `Terminal.InputText`  
**Reason**: Console no longer overrides InputText in Valheim 0.221.12

### 2. ✅ Skills Filter (Removed FrostMagic/FireMagic)
**File**: [1NPC/OdinGod.cs:147-156](1NPC/OdinGod.cs)  
**Change**: Removed filter for non-existent `FrostMagic` and `FireMagic` skill types  
**Reason**: Replaced with `ElementalMagic` and `BloodMagic` in new API

### 3. ✅ Interact() Signature (3 Parameters)
**Files**: 8 files updated  
**Change**: Added `bool alt` parameter to all `Interact()` method signatures  
**Reason**: Valheim 0.221.12 added alt-interact support

### 4. ✅ Tameable.Tame() Removed
**Files**: [4Pets/PetWolf.cs](4Pets/PetWolf.cs), [4Pets/PetTroll.cs](4Pets/PetTroll.cs)  
**Change**: Replaced `tame.Tame()` with `character.SetTamed(true)`  
**Reason**: Tame() method removed from Tameable class

### 5. ✅ Container.Interact() Parameter
**File**: [4Pets/PetWolf.cs:59](4Pets/PetWolf.cs)  
**Change**: Added `alt` parameter: `container.Interact(user, false, false)`  
**Reason**: Signature changed to 3 parameters

### 6. ✅ ZoneSystem.GetZone() Now Static
**Files**: 3 files updated  
**Change**: Changed from `ZoneSystem.instance.GetZone(pos)` to `ZoneSystem.GetZone(pos)`  
**Reason**: Method is now static

### 7. ✅ Minimap.DiscoverLocation() Parameter
**File**: [5Quest/Quest.cs:49](5Quest/Quest.cs)  
**Change**: Added `showMap` parameter: `Minimap.instance.DiscoverLocation(pos, pinType, label, true)`  
**Reason**: New parameter added to control map display

### 8. ✅ DungeonDB.RoomData Structure
**File**: [6Humans/HumanManager.cs:259](6Humans/HumanManager.cs)  
**Change**: Changed from `m_room` to `RoomInPrefab` property  
**Reason**: Field renamed/restructured

### 9. ✅ ZoneLocation SoftReference Access
**Files**: 3 files updated  
**Change**: Changed from `item.m_prefab.transform` to `item.m_prefab.Asset.transform`  
**Reason**: m_prefab is now a SoftReference<GameObject>

### 10. ✅ AddStatusEffect() Takes Hash
**Files**: 2 files updated  
**Change**: Added `.GetStableHashCode()` to string keys  
**Reason**: Method now takes `int` hash instead of `string`

### 11. ✅ Missing Dependencies
**Added DLLs**:
- gui_framework.dll
- SoftReferenceableAssets.dll
- Unity.TextMeshPro.dll
- netstandard.dll (v2.1)
- System.Memory.dll
- System.Buffers.dll
- System.Runtime.CompilerServices.Unsafe.dll
- Newtonsoft.Json.dll (downloaded from NuGet)

### 12. ✅ .NET Framework Version
**Change**: Updated from 4.7.1 to 4.8  
**Reason**: 4.7.1 targeting pack not available

### 13. ✅ ReadOnlySpan Compilation Issue
**File**: [9Misc/Util.cs:49-67](9Misc/Util.cs)  
**Change**: Used reflection workaround to call `Texture2D.LoadImage` at runtime  
**Reason**: ReadOnlySpan<T> signature incompatible with .NET Framework 4.8 at compile time

---

## Files Modified

| File | Changes | Type |
|------|---------|------|
| [OdinPlus.csproj](OdinPlus.csproj) | Added 9 new DLL references, changed target framework | Project |
| [0Main/Plugin.cs](0Main/Plugin.cs) | Terminal.InputText patch, version bump to 0.2.6 | Core |
| [1NPC/OdinNPC.cs](1NPC/OdinNPC.cs) | Interact signature | Base class |
| [1NPC/OdinGod.cs](1NPC/OdinGod.cs) | Interact signature, skills filter | NPC |
| [1NPC/OdinTrader.cs](1NPC/OdinTrader.cs) | Interact signature | NPC |
| [1NPC/HumanNPC.cs](1NPC/HumanNPC.cs) | Interact signature | NPC |
| [1NPC/OdinMunin.cs](1NPC/OdinMunin.cs) | Interact signature | NPC |
| [1NPC/OdinGoblin.cs](1NPC/OdinGoblin.cs) | Interact signature | NPC |
| [1NPC/OdinShaman.cs](1NPC/OdinShaman.cs) | Interact signature | NPC |
| [1NPC/NpcManager.cs](1NPC/NpcManager.cs) | SoftReference.Asset access | System |
| [4Pets/PetWolf.cs](4Pets/PetWolf.cs) | SetTamed, Container.Interact, Interact signature | Pet |
| [4Pets/PetTroll.cs](4Pets/PetTroll.cs) | SetTamed | Pet |
| [5Quest/Quest.cs](5Quest/Quest.cs) | GetZone static, DiscoverLocation param | Quest |
| [5Quest/HuntTarget.cs](5Quest/HuntTarget.cs) | AddStatusEffect hash | Quest |
| [6Humans/HumanManager.cs](6Humans/HumanManager.cs) | RoomInPrefab, SoftReference.Asset | Human |
| [7Locations/LocationMarker.cs](7Locations/LocationMarker.cs) | GetZone static | Location |
| [9Misc/DevTool.cs](9Misc/DevTool.cs) | GetZone static, AddStatusEffect hash | Utility |
| [9Misc/Util.cs](9Misc/Util.cs) | LoadImage reflection workaround | Utility |

**Total**: 18 files modified

---

## Build Output

```
DLL: c:\game\steamapps\common\Valheim\BepInEx\plugins\OdinPlus.dll
Size: 353 KB (361,472 bytes)
Build: Release
Target: .NET Framework 4.8
Warnings: 19 (non-breaking, mostly unused fields)
Errors: 0
```

---

## Dependencies Location

All dependencies copied to: `c:\Users\zarboz\Desktop\valhoom\buzzvalheimmods\0libDep\`

**Total files**: 95 DLLs including:
- BepInEx core
- Harmony
- Valheim game assemblies
- 75+ Unity modules
- System libraries (Memory, Buffers, Unsafe)
- TextMeshPro
- Newtonsoft.Json

---

## Testing Required

### Critical Features
- [ ] Mod loads without errors
- [ ] Odin NPC spawns at StartTemple
- [ ] Can interact with Odin (E key)
- [ ] Trophy offering works (1-8 keys)
- [ ] Credits system functional
- [ ] Skill selection works (G key)
- [ ] Skills raise correctly (including new ElementalMagic/BloodMagic)

### Store System
- [ ] Trader shop opens
- [ ] Shows Credits instead of Coins
- [ ] Can purchase items
- [ ] Icon displays correctly

### NPCs
- [ ] All NPCs interact properly
- [ ] Munin quest system
- [ ] Human NPCs spawn
- [ ] No Interact() errors

### Pets
- [ ] Troll pet summons and tames
- [ ] Wolf pet summons and tames
- [ ] Wolf inventory accessible (secondary interact)

### Quests
- [ ] Can create quests
- [ ] Quest markers appear
- [ ] Quest completion works
- [ ] Location tracking functional

### UI & Assets
- [ ] Custom mead icons load
- [ ] OdinLegacy icon loads
- [ ] Status effect icons load
- [ ] No texture loading errors

### Chat Commands
- [ ] `/odinhere` works
- [ ] `/whereami` works
- [ ] `/whereodin` works
- [ ] DevTool commands (if enabled)

---

## Known Issues / Notes

### Texture Loading
- Uses reflection workaround for `Texture2D.LoadImage` due to ReadOnlySpan compilation issues
- Icons should load correctly at runtime
- If icons fail to load, check BepInEx log for "Failed to load texture" warnings

### Warnings (Non-Breaking)
- 11 warnings about unused fields in OdinSE.cs (StatusEffect data fields)
- 5 warnings about unnecessary `new` keywords in OdinTrader.cs
- 1 warning about unused field in DevTool.cs
- **These do not affect functionality**

### Runtime Considerations
1. **Skills**: New magic types (ElementalMagic, BloodMagic) will appear in Odin's skill list
2. **Crossbows & Ride**: New skill types added in 0.221.12 are now available
3. **SoftReference**: Asset loading may be slightly slower due to async loading system
4. **GetZone**: Now static, performance should be identical

---

## Deployment Verified

✅ DLL deployed to: `c:\game\steamapps\common\Valheim\BepInEx\plugins\OdinPlus.dll`  
✅ Size: 353 KB  
✅ Timestamp: 2026-07-30 09:33:03  
✅ Ready for in-game testing

---

## Version History

- **0.2.5** - Last version for Valheim 0.217.x
- **0.2.6** - Updated for Valheim 0.221.12 (Unity 6000.0.61)

---

## If Issues Occur

### Check BepInEx Log
Location: `C:\Users\zarboz\AppData\LocalLow\IronGate\Valheim\Player.log`

**Look for**:
- `[OdinPlus]` entries
- Harmony patch failures
- NullReferenceException errors
- "Failed to load texture" warnings

### Common Issues

**Mod doesn't load**:
- Check BepInEx version (need 5.4.21+)
- Verify all DLLs are in BepInEx\plugins\
- Check for Harmony patch errors in log

**NPCs don't spawn**:
- Check for ZNetScene registration errors
- Verify StartTemple location exists
- Try `/odinhere` command

**Skills don't appear**:
- Check for enum parsing errors
- Verify Skills.SkillType changes applied correctly

**Textures are missing/broken**:
- Check "Failed to load texture" in log
- Verify embedded resources in DLL
- May need to update LoadTextureRaw implementation

---

## Next Steps

1. **Test in-game** with checklist above
2. **Monitor BepInEx log** for any errors
3. **Report issues** with:
   - Exact error message from log
   - Steps to reproduce
   - Which feature is broken

---

## Success Criteria

✅ Build compiles without errors  
✅ All API changes implemented  
✅ DLL deployed to correct location  
⏳ In-game testing pending

**Status**: Ready for testing!
