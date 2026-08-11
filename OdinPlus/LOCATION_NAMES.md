# Valheim 0.221.12 Location Names (from AssetRipper game dump)

Valid location prefab names for `VillageLocations` in `faction_config.yaml`.

---

## Meadows
```
WoodFarm1            # Farm with fields
WoodVillage1         # Ruined village
WoodHouse1-13        # Various houses (WoodHouse1 through WoodHouse13)
Dolmen01, Dolmen02, Dolmen03
ShipSetting01
Eikthyrnir           # Boss altar
Hildir_camp
Runestone_Boars
Runestone_Meadows
StartTemple          # Spawn stones
CombatRuin01
```

## Black Forest
```
Greydwarf_camp1      # Greydwarf camp
Ruin1, Ruin2         # Stone ruins
Crypt2, Crypt3, Crypt4
TrollCave02
Vendor_BlackForest   # Haldor
GDKing               # Boss altar
Hildir_crypt
BigRockClearing
StoneTowerRuins03, 07, 08, 09, 10
Runestone_BlackForest
Runestone_Greydwarfs
```

## Swamp
```
SwampHut1-5          # Draugr huts (SwampHut1 through SwampHut5)
SwampRuin1, SwampRuin2
SunkenCrypt4         # Iron crypt
SwampWell1
InfestedTree01
Grave1
Bonemass             # Boss altar
Runestone_Draugr
Runestone_Swamps
```

## Mountains
```
AbandonedLogCabin02, 03, 04
DrakeNest01
DrakeLorestone
MountainCave02
MountainGrave01
MountainWell1
Waymarker01, Waymarker02
StoneTowerRuins04, 05
Dragonqueen          # Boss altar
Hildir_cave
Runestone_Mountains
```

## Heath (Plains)
```
GoblinCamp2          # Fuling village
Ruin3
StoneHenge1-6        # (StoneHenge1 through StoneHenge6)
StoneTower1, StoneTower3
GoblinKing           # Boss altar
Runestone_Plains
```

## Plains (new content)
```
TarPit1, TarPit2, TarPit3
Hildir_plainsfortress
```

## Mistlands
```
Mistlands_Harbour1
Mistlands_DvergrTownEntrance1, 2
Mistlands_DvergrBossEntrance1
Mistlands_Excavation1, 2, 3
Mistlands_Giant1, 2
Mistlands_GuardTower1_new
Mistlands_GuardTower1_ruined_new, _new2
Mistlands_GuardTower2_new
Mistlands_GuardTower3_new
Mistlands_GuardTower3_ruined_new
Mistlands_Lighthouse1_new
Mistlands_RoadPost1
Mistlands_RockSpire1
Mistlands_Statue1, 2
Mistlands_StatueGroup1
Mistlands_Swords1, 2, 3
Mistlands_Viaduct1, 2
Runestone_Mistlands
```

## Ashlands
```
CharredFortress
CharredRuins1, 2, 3, 4
CharredTowerRuins1
CharredTowerRuins1_dvergr
CharredTowerRuins2, 3
CharredStone_Spawner
AshlandRuins
FortressRuins
FaderLocation        # Boss altar
LeviathanLava
MorgenHole1, 2, 3
PlaceofMystery1, 2, 3
SulfurArch
VoltureNest
Runestone_Ashlands
```

---

## Default Config (all biomes)

```yaml
VillageLocations:
  # Meadows
  - WoodFarm1
  - WoodVillage1
  # Black Forest
  - Greydwarf_camp1
  - Ruin1
  # Swamp
  - SwampHut1
  - SwampRuin1
  # Mountains
  - AbandonedLogCabin02
  - MountainGrave01
  # Heath/Plains
  - GoblinCamp2
  - StoneHenge1
  # Mistlands
  - Mistlands_Harbour1
  - Mistlands_DvergrTownEntrance1
  # Ashlands
  - CharredFortress
```

---

## REMOVED in 0.221.12 (do NOT use)

These were in older versions but no longer exist as location prefabs:
```
GoblinCamp1, Fort1, Castle, TrollCave (use TrollCave02)
StoneHouse1-5, StoneHouse1_heath, etc.
StoneTower2, StoneTower4, StoneTowerRuins10
SunkenCrypt1, 2, 3 (only SunkenCrypt4 remains)
ShipWreck01-04
xmastree, Hugintest
Meteorite, Pillar1, Pillar2
```

---

**Source:** AssetRipper export of Valheim 0.221.12 → `Assets/world/Locations/` folders
