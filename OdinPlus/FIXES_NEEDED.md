# OdinPlus - Critical Fixes Needed

## ✅ COMPLETED: Clean up stale TODO/HACK comments
- [x] Remove TODO comment in Localization.cs:9
- [x] Remove dead `hasPIN` field in Quest.cs:20  
- [x] Remove HACK comments in Quest.cs:142,150

---

## 🔴 CRITICAL #1: NPC AI not working (NPCs standing still)

### Problem
NPCs spawned via `Instantiate(prefab, pos, Quaternion.identity)` don't have valid ZDOs, so MonsterAI doesn't function. NPCs stand frozen with no patrol/combat behavior.

### Root Cause
```csharp
// HumanManager.cs line 297-308
private static GameObject SpawnNPC(string prefabName, Vector3 pos)
{
    var prefab = GetNPCPrefab(prefabName);
    var go = Instantiate(prefab, pos, Quaternion.identity);  // ❌ No ZDO created
    go.name = prefabName;
    return go;
}
```

When `Instantiate()` is called:
1. GameObject is created with ZNetView component
2. ZNetView.Awake() tries to register with ZNetScene
3. Prefab IS registered in ZNetScene (via OdinPostRegister at line 94)
4. BUT the instantiated object doesn't get a valid ZDO because we bypass ZNetScene's spawn flow
5. MonsterAI.Awake() checks for ZDO and fails initialization

### Fix Option A: Use ZNetScene to spawn (RECOMMENDED)
```csharp
private static GameObject SpawnNPC(string prefabName, Vector3 pos)
{
    var prefab = GetNPCPrefab(prefabName);
    if (prefab == null)
    {
        DBG.blogWarning($"SpawnNPC: prefab '{prefabName}' not found");
        return null;
    }
    
    // Get the hash that was used to register the prefab
    int prefabHash = prefabName.GetStableHashCode();
    
    // Spawn through ZNetScene so it gets proper ZDO
    var go = ZNetScene.instance.CreateObject(pos, Quaternion.identity, prefabHash);
    if (go == null)
    {
        DBG.blogWarning($"SpawnNPC: ZNetScene failed to spawn '{prefabName}'");
        // Fallback to direct instantiate
        go = Instantiate(prefab, pos, Quaternion.identity);
        go.name = prefabName;
    }
    
    return go;
}
```

### Fix Option B: Force ZNetView registration (SIMPLER)
```csharp
private static GameObject SpawnNPC(string prefabName, Vector3 pos)
{
    var prefab = GetNPCPrefab(prefabName);
    if (prefab == null)
    {
        DBG.blogWarning($"SpawnNPC: prefab '{prefabName}' not found");
        return null;
    }
    
    var go = Instantiate(prefab, pos, Quaternion.identity);
    go.name = prefabName;
    
    // Ensure ZNetView has a valid ZDO
    var znv = go.GetComponent<ZNetView>();
    if (znv != null && znv.GetZDO() == null)
    {
        // Force registration - this should happen automatically but may need manual trigger
        go.SetActive(false);
        go.SetActive(true);
    }
    
    return go;
}
```

---

## 🔴 CRITICAL #2: Quest placement fails - "Cannot Place Quest: WoodHouse6"

### Problem
```
[Warning: OdinPlus] Dice Rolled
[Error  : OdinPlus] Cannot Place Quest : WoodHouse6
[Warning: OdinPlus] Location cant find location WoodHouse6 at (0.00, 0.00, 0.00)
```

LocationManager.FindClosestLocation() returns false because the requested location doesn't exist in the world.

### Root Causes
1. **Hardcoded legacy location names** - QuestRef.cs line 16-22 has location names from old Valheim versions
2. **Location may not spawn in world** - Even valid names may not exist in this world seed
3. **m_locationInstances might be empty** - If GetValDictionary() runs before ZoneSystem populates locations

### Diagnosis Steps
Add logging to see what locations ARE available:

```csharp
// In LocationManager.cs, in GetValDictionary() at line 46:
public static void GetValDictionary()
{
    var a = Traverse.Create(ZoneSystem.instance).Field<Dictionary<Vector2i, ZoneSystem.LocationInstance>>("m_locationInstances").Value;
    foreach (var item in a)
    {
        m_locationInstances.Add(item.Key, item.Value);
    }
    
    // ADD THIS:
    DBG.blogInfo($"[LocationManager] Loaded {m_locationInstances.Count} locations from ZoneSystem");
    if (m_locationInstances.Count > 0)
    {
        DBG.blogInfo("[LocationManager] Sample locations:");
        foreach (var loc in m_locationInstances.Values.Take(10))
        {
            DBG.blogInfo($"  - {loc.m_location.m_prefabName} at {loc.m_position}");
        }
    }
}
```

### Fix A: Fallback to ANY location if specific one not found
```csharp
// In LocationManager.cs, at end of FindClosestLocation() method (line 119-138):
public static bool FindClosestLocation(string name, Vector3 point, out string id, out Vector3 pos)
{
    float num = 999999f;
    pos = Vector3.zero;
    id = "0_0";
    bool result = false;
    
    foreach (var item in m_locationInstances)
    {
        float num2 = Vector3.Distance(item.Value.m_position, point);
        if (item.Value.m_location.m_prefabName == name && num2 < num)
        {
            pos = item.Value.m_position;
            id = item.Key.Pak();
            num = num2;
            result = true;
        }
    }
    
    // ADD THIS FALLBACK:
    if (!result && m_locationInstances.Count > 0)
    {
        // Pick ANY location as fallback
        var fallback = m_locationInstances.Values.OrderBy(l => Vector3.Distance(l.m_position, point)).First();
        pos = fallback.m_position;
        id = m_locationInstances.Keys.First(k => m_locationInstances[k].m_position == pos).Pak();
        result = true;
        DBG.blogWarning($"[LocationManager] '{name}' not found, using fallback '{fallback.m_location.m_prefabName}' at {pos}");
    }
    
    return result;
}
```

### Fix B: Make QuestRef use dynamic location discovery
Instead of hardcoded names, query what's actually available:

```csharp
// NEW method in LocationManager.cs:
public static List<string> GetLocationsByBiome(Heightmap.Biome biome)
{
    var locations = new List<string>();
    foreach (var loc in m_locationInstances.Values)
    {
        if (loc.m_location.m_biome == biome && 
            !loc.m_location.m_clearArea &&
            !loc.m_location.m_prefabName.Contains("Runestone"))
        {
            locations.Add(loc.m_location.m_prefabName);
        }
    }
    return locations;
}

// Then in QuestRef.cs, replace hardcoded arrays with:
public static string GetRandomQuestLocation(int level)
{
    Heightmap.Biome biome = level <= 1 ? Heightmap.Biome.Meadows : 
                           level <= 3 ? Heightmap.Biome.BlackForest : 
                           Heightmap.Biome.Swamp;
    
    var available = LocationManager.GetLocationsByBiome(biome);
    return available.Count > 0 ? available.GetRandomElement() : "WoodFarm1";  // Fallback
}
```

---

## 🟡 MEDIUM: Builder NPC should move to build location

### Problem
BuilderNPC stands still while building. Should walk to the build site.

### Current Code
```csharp
// BuilderNPC.cs - BuildBlueprint() just instantiates pieces remotely
foreach (var piece in blueprint.Pieces)
{
    var prefab = ZNetScene.instance.GetPrefab(piece.PrefabName);
    var go = Instantiate(prefab, piece.Position, piece.Rotation);
    // NPC never moves
}
```

### Fix: Make NPC walk to build center first
```csharp
private IEnumerator BuildBlueprint(Blueprint blueprint)
{
    // Calculate build center
    Vector3 buildCenter = Vector3.zero;
    foreach (var piece in blueprint.Pieces)
        buildCenter += piece.Position;
    buildCenter /= blueprint.Pieces.Count;
    
    // Walk to build site
    var ai = GetComponent<MonsterAI>();
    if (ai != null)
    {
        float startTime = Time.time;
        while (Vector3.Distance(transform.position, buildCenter) > 5f && Time.time - startTime < 30f)
        {
            ai.MoveTowards(buildCenter, 1f, false);
            yield return new WaitForSeconds(0.5f);
        }
    }
    
    // Now build
    foreach (var piece in blueprint.Pieces)
    {
        var prefab = ZNetScene.instance.GetPrefab(piece.PrefabName);
        var go = Instantiate(prefab, piece.Position, piece.Rotation);
        Say($"Building {piece.PrefabName}...");
        yield return new WaitForSeconds(3f);
    }
    
    Say("Construction complete!");
}
```

---

## 🟢 LOW: Make quest locations configurable via YAML

### Problem
QuestRef.cs has hardcoded location arrays that may not match current Valheim version.

### Fix: Create `quest_locations.yaml`
```yaml
Meadows:
  - WoodFarm1
  - Runestone_Meadows
  - ShipSetting01
BlackForest:
  - Runestone_BlackForest
  - TrollCave02
  - WoodHouse3
Swamp:
  - SwampRuin1
  - SwampRuin2
Mountain:
  - MountainCave02
  - DrakeNest01
```

Load in QuestRef similar to FactionQuestSystem's YAML loading.

---

## Priority Order

1. **Fix #1 (NPC AI)** - BLOCKS ALL NPC GAMEPLAY
   - Without this, NPCs are useless decorations
   - Try Fix Option B first (simpler), then Option A if needed

2. **Fix #2 (Quest placement)** - BLOCKS QUEST SYSTEM  
   - Start with diagnostic logging to see what locations exist
   - Implement Fix A (fallback) immediately
   - Fix B (dynamic discovery) is better long-term solution

3. **Fix #4 (Builder movement)** - QUALITY OF LIFE
   - Builder works but doesn't move - not blocking

4. **Fix #5 (Quest YAML)** - FUTURE-PROOFING
   - Current hardcoded approach can work with Fix #2A fallback
   - YAML is cleaner but not urgent
