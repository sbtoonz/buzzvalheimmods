# OdinPlus - Build Errors for Valheim 0.221.12

## Build Status: ❌ FAILED (Compilation Errors)

**Date**: 2026-07-30  
**Valheim Version**: 0.221.12  
**Compiler**: MSBuild 17.14.51

---

## Setup Completed ✅

1. ✅ Created `0libDep` folder
2. ✅ Copied BepInEx and Harmony DLLs
3. ✅ Copied Valheim game assemblies
4. ✅ Downloaded Newtonsoft.Json.dll
5. ✅ Copied all Unity modules
6. ✅ Updated target framework to .NET 4.8
7. ✅ Fixed Interact() signatures (3 parameters)
8. ✅ Fixed Skills filter (removed FrostMagic/FireMagic)
9. ✅ Fixed Console.InputText → Terminal.InputText

---

## Compilation Errors (14 errors)

### 1. Tameable.Tame() Method Removed ❌
**Files**: `4Pets/PetWolf.cs:28`, `4Pets/PetTroll.cs:18`

```
error CS1061: 'Tameable' does not contain a definition for 'Tame'
```

**Cause**: Valheim 0.221.12 removed or renamed the `Tame()` method in `Tameable` class.

**Need**: Check current Tameable API in [assem_valheim/Tameable.cs](c:\Users\zarboz\Desktop\valhoom\assem_valheim\Tameable.cs) for replacement method.

---

### 2. Missing gui_framework.dll Reference ❌
**Files**: `0Main/Plugin.cs:202,213,230,239`

```
error CS0012: The type 'GuiInputField' is defined in an assembly that is not referenced. 
You must add a reference to assembly 'gui_framework, Version=0.0.0.0'
```

**Cause**: `GuiInputField` type is in a separate assembly not in our dependencies.

**Need**: Copy `gui_framework.dll` from Valheim installation.

---

### 3. Missing SoftReferenceableAssets.dll Reference ❌
**Files**: `1NPC/NpcManager.cs:54`, `6Humans/HumanManager.cs:381`

```
error CS0012: The type 'SoftReference<>' is defined in an assembly that is not referenced. 
You must add a reference to assembly 'SoftReferenceableAssets, Version=0.0.0.0'
```

**Cause**: `SoftReference<T>` is used for async asset loading in Valheim 0.221.12.

**Need**: Copy `SoftReferenceableAssets.dll` from Valheim installation.

---

### 4. ZoneSystem.GetZone() Changed to Static ❌
**Files**: `7Locations/LocationMarker.cs:52`, `5Quest/Quest.cs:168`

```
error CS0176: Member 'ZoneSystem.GetZone(Vector3)' cannot be accessed with an instance reference
```

**Old Code**:
```csharp
ZoneSystem.instance.GetZone(pos)
```

**Fix**:
```csharp
ZoneSystem.GetZone(pos)  // Now static
```

---

### 5. Container.Interact() Missing 'alt' Parameter ❌
**File**: `4Pets/PetWolf.cs:59`

```
error CS7036: There is no argument given that corresponds to the required parameter 'alt'
```

**Old Code**:
```csharp
chest.Interact(user, false)
```

**Fix**:
```csharp
chest.Interact(user, false, false)  // Added 'alt' parameter
```

---

### 6. Minimap.DiscoverLocation() Missing 'showMap' Parameter ❌
**File**: `5Quest/Quest.cs:49`

```
error CS7036: There is no argument given that corresponds to the required parameter 'showMap'
```

**Old Code**:
```csharp
Minimap.DiscoverLocation(pos, pinType, label)
```

**Fix**:
```csharp
Minimap.DiscoverLocation(pos, pinType, label, true)  // Added showMap parameter
```

---

### 7. DungeonDB.RoomData Structure Changed ❌
**File**: `6Humans/HumanManager.cs:259`

```
error CS1061: 'DungeonDB.RoomData' does not contain a definition for 'm_room'
```

**Cause**: `DungeonDB.RoomData` structure changed in Valheim 0.221.12.

**Need**: Check current `DungeonDB.RoomData` structure in decompiled source.

---

### 8. ZoneSystem.ZoneLocation Structure Changed ❌
**File**: `6Humans/HumanManager.cs:410`

```
error CS1061: 'ZoneSystem.ZoneLocation' does not contain a definition for 'm_location'
```

**Cause**: `ZoneLocation` structure changed in Valheim 0.221.12.

**Need**: Verify correct field name (possibly renamed to something else).

---

### 9. Skills.SkillType Parse Error ❌
**File**: `9Misc/DevTool.cs:328`

```
error CS1503: Argument 1: cannot convert from 'string' to 'int'
```

**Cause**: `Skills.SkillType` enum parsing changed.

**Need**: Check line 328 in DevTool.cs for the exact code.

---

### 10. netstandard Version Mismatch ⚠️
```
error CS1705: Assembly 'UnityEngine.ImageConversionModule' uses 'netstandard, Version=2.1.0.0' 
which has a higher version than referenced assembly 'netstandard' with identity 'netstandard, Version=2.0.0.0'
```

**Cause**: Unity 6 modules require .NET Standard 2.1, but project references 2.0.

**Possible Fix**: May need to add explicit netstandard 2.1 reference.

---

## Missing Dependencies

Need to copy from Valheim installation:
- `gui_framework.dll`
- `SoftReferenceableAssets.dll`
- Possibly `netstandard.dll` (version 2.1)

**Search Command**:
```powershell
Get-ChildItem "C:\Program Files (x86)\Steam\steamapps\common\Valheim\" -Recurse -Filter "gui_framework.dll"
Get-ChildItem "C:\Program Files (x86)\Steam\steamapps\common\Valheim\" -Recurse -Filter "SoftReferenceableAssets.dll"
```

---

## Code Changes Required

### Priority 1 - Critical API Changes

1. **Tameable.Tame()** - Find replacement method
2. **ZoneSystem.GetZone()** - Change to static call (2 locations)
3. **Container.Interact()** - Add 'alt' parameter (1 location)
4. **Minimap.DiscoverLocation()** - Add 'showMap' parameter (1 location)

### Priority 2 - Structure Changes

5. **DungeonDB.RoomData** - Find correct field name
6. **ZoneSystem.ZoneLocation** - Find correct field name  
7. **Skills.SkillType** - Fix parse/conversion issue

### Priority 3 - Missing References

8. Add `gui_framework.dll` to project references
9. Add `SoftReferenceableAssets.dll` to project references

---

## Warnings (Non-Breaking)

- 11 warnings about unused fields and unnecessary `new` keywords
- These don't prevent compilation but should be cleaned up

---

## Next Steps

1. **Copy missing DLLs** to `0libDep`:
   - gui_framework.dll
   - SoftReferenceableAssets.dll

2. **Research API changes** in decompiled assembly_valheim:
   - Read [assem_valheim/Tameable.cs](c:\Users\zarboz\Desktop\valhoom\assem_valheim\Tameable.cs) for Tame() replacement
   - Read [assem_valheim/DungeonDB.cs](c:\Users\zarboz\Desktop\valhoom\assem_valheim\DungeonDB.cs) for RoomData structure
   - Read [assem_valheim/ZoneSystem.cs](c:\Users\zarboz\Desktop\valhoom\assem_valheim\ZoneSystem.cs) for ZoneLocation structure

3. **Fix compilation errors** in order of priority

4. **Rebuild** and test

---

## Build Command

```powershell
cd "c:\Users\zarboz\Desktop\valhoom\buzzvalheimmods\OdinPlus"
& "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe" OdinPlus.csproj -p:Configuration=Release -t:Build
```

---

## Current State

- ✅ Project structure set up
- ✅ Dependencies folder created
- ✅ Most DLLs copied
- ✅ Basic API fixes applied (Interact, Skills, Terminal)
- ❌ Missing 2 DLLs (gui_framework, SoftReferenceableAssets)
- ❌ 14 compilation errors remain
- ⏳ Ready for API research and fixes
