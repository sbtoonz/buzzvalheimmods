# OdinPlus - Valheim 0.221.12 Upgrade Notes

## API Changes Summary

### ✅ No Changes Required (Still Compatible)
- `Trader.TradeItem` - Same structure (m_prefab, m_stack, m_price, m_requiredGlobalKey)
- `Game.instance.m_playerPrefab` - Still exists (line 38)
- `Skills.RaiseSkill(SkillType, float)` - Still exists (line 278)
- `Humanoid` fields - `m_randomSets`, `m_randomWeapon`, `m_randomShield`, `m_defaultItems`, `m_unarmedWeapon` all still exist
- `MonsterAI.m_alertRange`, `m_consumeItems` - Still exist
- `CreatureSpawner.m_creaturePrefab`, `m_respawnTimeMinuts`, `m_levelupChance`, `m_setPatrolSpawnPoint` - All still exist
- `VisEquipment.m_isPlayer` - Still exists (line 43)
- `Tutorial.instance.m_ravenPrefab` - Still exists (line 39)
- `ZRoutedRpc.InvokeRoutedRPC()` - Still exists, same signature
- `Raven.IsInstantiated()`, `Raven.AddTempText()` - Confirmed present

### ⚠️ Potentially Breaking (Need Verification)
1. **Interact() signature** - Now has 3 parameters: `(Humanoid character, bool hold, bool alt)`
   - **Impact**: All `Interact()` implementations in OdinNPC, OdinGod, OdinTrader need the `alt` parameter
   - **Status**: Already compatible in decompiled code

2. **Console.InputText()** - Console class does NOT override `InputText()` anymore
   - **Impact**: Patch target `Console.InputText` should be changed to `Terminal.InputText`
   - **Fix**: Change patch target from `Console` to `Terminal` class

3. **Skills.SkillType enum** - Changed from old mod expectations
   - **Old (removed)**: `FrostMagic`, `FireMagic`
   - **New**: `ElementalMagic` (9), `BloodMagic` (10), `Crossbows` (14), `Ride` (110)
   - **Impact**: OdinGod.cs filters out "FrostMagic" and "FireMagic" (line 152) - these won't exist

4. **BaseAI fields** - Need to check if MonsterAI fields moved to BaseAI
   - `m_randomMoveInterval`, `m_viewRange`, `m_hearRange` might be in BaseAI base class
   - **Status**: Not visible in MonsterAI.cs excerpt, need to verify existence

5. **ZoneSystem.LocationInstance** - Structure may have changed
   - Need to verify `.m_position`, `.m_location.m_prefabName` still exist

6. **Localization.SetupLanguage()** - Not found in assembly_valheim
   - Likely in `assembly_guiutils` or `assembly_utils`
   - **Status**: Unknown if signature changed

### 🔍 Runtime Verification Needed
- Private field access via `Traverse` (m_namedPrefabs, m_itemByHash, m_locationInstances, m_trader, m_selectedItem)
- ZRoutedRpc.Register<T> generic signature
- DungeonDB.GetRooms() method
- Gogan.LogEvent analytics logging

## Required Code Changes

### 1. Change Console.InputText Patch to Terminal.InputText
**File**: `0Main/Plugin.cs` line 195

**Before**:
```csharp
[HarmonyPatch(typeof(Console), "InputText")]
[HarmonyPrefix]
private static class Patch_Console_InputText
```

**After**:
```csharp
[HarmonyPatch(typeof(Terminal), "InputText")]
[HarmonyPrefix]
private static class Patch_Terminal_InputText
```

### 2. Update Skills Filter in OdinGod.cs
**File**: `1NPC/OdinGod.cs` line 152

**Before**:
```csharp
if (skill.ToString() == "None" || skill.ToString() == "FrostMagic" || skill.ToString() == "All" || skill.ToString() == "FireMagic")
    continue;
```

**After**:
```csharp
if (skill.ToString() == "None" || skill.ToString() == "All")
    continue;
```

### 3. Verify All Interact() Implementations Have 3 Parameters
**Files**: `1NPC/OdinNPC.cs`, `1NPC/OdinGod.cs`, `1NPC/OdinTrader.cs`

All should have:
```csharp
public bool Interact(Humanoid character, bool hold, bool alt)
```

### 4. Add Null Checks for Traverse Field Access
Add defensive null checks after all `Traverse.Create(...).Field<T>(...).Value` calls to handle potential field renames.

## Testing Checklist

- [ ] Mod loads without errors in BepInEx log
- [ ] Odin NPC spawns at StartTemple
- [ ] Can interact with Odin (E key)
- [ ] Trader shop opens with custom items
- [ ] Credits system works (shows credits instead of coins)
- [ ] Skills can be raised via Odin
- [ ] Pets can be spawned (Troll, Wolf)
- [ ] Quests can be created and tracked
- [ ] Human NPCs spawn in dungeons
- [ ] LocationMarker tracking works
- [ ] Chat commands work (/odinhere, /whereami, etc)
- [ ] Data persistence (save/load credits and quests)

## Build Requirements

- .NET Framework 4.7.1 (already set in .csproj)
- Updated reference: `assembly_valheim.dll` from Valheim 0.221.12 managed folder
- BepInEx 5.4.21+
- 0Harmony 2.x
- Newtonsoft.Json

## Deployment Path

**Debug**: `c:\game\steamapps\common\Valheim\BepInEx\scripts\`
**Release**: `c:\game\steamapps\common\Valheim\BepInEx\plugins\`

Output DLL: `OdinPlus.dll`

## Known Issues to Monitor

1. Private field access via Harmony Traverse may break if internal field names changed
2. ZoneSystem.m_locations indexing (line 54 in NpcManager) assumes StartTemple is at index 85 - may have changed
3. DungeonDB.GetRooms() API not verified in decompiled source
4. Localization patch target unknown (Localization.cs not in assembly_valheim)

## Compatibility Notes

- Valheim Version: 0.221.12 (Unity 6000.0.61)
- BepInEx: 5.4.23.3 (BepInExPack Valheim 5.4.2333)
- Mod Version: 0.2.5 → 0.2.6 (after upgrade)
