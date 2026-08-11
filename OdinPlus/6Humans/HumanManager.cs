using System.Security.AccessControl;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using HarmonyLib;
using UnityEngine;
namespace OdinPlus
{

	public class HumanManager : MonoBehaviour
	{

		public static bool isInit = false;
		public static Dictionary<string, GameObject> PrefabList = new Dictionary<string, GameObject>();
		public static Dictionary<string, GameObject> HumanList = new Dictionary<string, GameObject>();
		public static GameObject BasicHuman;
		#region Presets
		public static string[] Weapons = { "AtgeirBlackmetal", "AtgeirBronze", "AtgeirIron", "Battleaxe", "KnifeBlackMetal", "KnifeChitin", "KnifeCopper", "KnifeFlint", "MaceBronze", "MaceIron", "MaceNeedle", "MaceSilve",
		 "SledgeIron", "SledgeStagbreaker", "SpearBronze", "SpearElderbark", "SpearFlint", "SpearWolfFang", "SwordBlackmetal", "SwordBronze","SwordIron", "SwordSilver", "AtgeirBlackmetal",
		 "AtgeirBronze", "AtgeirIron", "Battleaxe", "KnifeBlackMetal", "KnifeChitin", "KnifeCopper", "KnifeFlint", "MaceBronze", "MaceIron", "MaceNeedle", "MaceSilver" };
		public static string[] Armor = { "ArmorBronzeChest", "ArmorBronzeLegs", "ArmorIronChest", "ArmorIronLegs", "ArmorLeatherChest", "ArmorLeatherLegs", "ArmorPaddedCuirass", "ArmorPaddedGreaves", "ArmorRagsChest",
		 "ArmorRagsLegs", "ArmorTrollLeatherChest", "ArmorTrollLeatherLegs", "ArmorWolfChest", "ArmorWolfLegs", "CapeDeerHide", "CapeLinen", "CapeLox", "CapeTrollHide", "CapeWolf", "HelmetBronze", "HelmetDrake",
		 "HelmetIron", "HelmetLeather", "HelmetPadded", "HelmetTrollLeather", "HelmetYule" };
		public static string[] Shield = { "ShieldBanded", "ShieldBlackmetal", "ShieldBlackmetalTower", "ShieldBronzeBuckler", "ShieldIronSquare", "ShieldIronTower", "ShieldKnight", "ShieldSerpentscale", "ShieldSilver", "ShieldWood", "ShieldWoodTower" };
		public static string[] Tools = { "AxeIron", "PickAxeIron" };
		public static string[] GuardWeapons = { "SwordBronze", "SwordIron", "SwordSilver", "AxeIron", "AxeBronze", "AxeBlackMetal", "Battleaxe", "AtgeirIron", "AtgeirBronze" };
		public static string[] GuardShields = { "ShieldBanded", "ShieldIronSquare", "ShieldBronzeBuckler", "ShieldWood", "ShieldKnight", "ShieldSilver" };
		private class humanData
		{
			public string presetNAME = "MidEnemy1";
			public string prefab = "Goblin";
			public bool isFriend = false;
			public float m_randomMoveInterval = 30;
			public float m_randomMoveRange = 3;
			public float m_moveMinAngle = 30;
			public float health = 200;
			public float speed = 7;
			public string sets = "Troll";
			public string[] weapons = { "SwordBronze", "SwordIron", "AtgeirBronze", "AtgeirIron", "SpearBronze" };
			public string[] sheild = { "ShieldBanded", "ShieldBlackmetal", "ShieldBlackmetalTower", "ShieldBronzeBuckler", "ShieldIronSquare", "ShieldIronTower", "ShieldKnight", "ShieldSerpentscale", "ShieldSilver", "ShieldWood", "ShieldWoodTower" };
			//public string[] armor = { "ArmorBronzeChest", "ArmorBronzeLegs", "ArmorIronChest", "ArmorIronLegs", "CapeTrollHide", "CapeWolf", "HelmetBronze", "HelmetDrake", "HelmetIron" };
		}
			private static List<humanData> presets = new List<humanData>
		{
			new humanData(),
			new humanData(){presetNAME="LowEnemey1",health=300,
			weapons = new string[]{"Club","SpearFlint","KnifeFlint"},
			},
			new humanData(){presetNAME="Fighter1",health=500,
			sets="Troll0",
			weapons = new string[]{"Club","SpearFlint","KnifeFlint"},
			},
			new humanData(){presetNAME="Fighter2",health=500,
			sets="Brozen",
			},
			new humanData(){presetNAME="DumbNPC",health=300,
			sets="Troll0",
			weapons = new string[]{"SwordBronze", "SwordIron"},
			m_randomMoveRange=3,
			m_randomMoveInterval=20,
			isFriend=true,
			},
			new humanData(){presetNAME="DumbWorker",health=300,
			sets="Troll0",
			m_randomMoveRange=5,
			m_randomMoveInterval=15,
			weapons=new string[]{"AxeIron","AxeBronze","AxeFlint"},
			sheild=new string[]{""},
			isFriend=true,
			},
			new humanData(){presetNAME="BuilderWorker",health=300,
			sets="Leather",
			m_randomMoveRange=8,
			m_randomMoveInterval=15,
			weapons=new string[]{"Hammer"},
			sheild=new string[]{""},
			isFriend=true,
			},
			new humanData(){presetNAME="PriestNPC",health=200,
			sets="Robes",
			m_randomMoveRange=3,
			m_randomMoveInterval=30,
			weapons=new string[]{""},
			sheild=new string[]{""},
			isFriend=true,
			},
			new humanData(){presetNAME="MessengerNPC",health=250,
			sets="Troll0",
			m_randomMoveRange=5,
			m_randomMoveInterval=15,
			weapons=new string[]{""},
			sheild=new string[]{""},
			isFriend=true,
			},
			new humanData(){presetNAME="GuardNPC",health=400,
			sets="Padded0",
			m_randomMoveInterval=5,
			m_randomMoveRange=20,
			weapons=GuardWeapons,
			sheild=GuardShields,
			isFriend=true,
			}
		};
		#endregion Presets
		public static Dictionary<string, GameObject> HumanPreset = new Dictionary<string, GameObject>();
			public static Dictionary<string, string[]> ArmorSets = new Dictionary<string, string[]>
		{
			{"Troll",new string[]{"HelmetTrollLeather","CapeTrollHide","ArmorTrollLeatherChest","ArmorTrollLeatherLegs"}},
			{"Troll0",new string[]{"CapeTrollHide","ArmorTrollLeatherChest","ArmorTrollLeatherLegs"}},
			{"Brozen",new string[]{"ArmorBronzeChest","ArmorBronzeLegs","HelmetBronze","CapeTrollHide"}},
			{"Iron",new string[]{"ArmorIronChest","ArmorIronLegs","HelmetIron","CapeLinen"}},
			{"Silver",new string[]{"ArmorWolfChest","ArmorWolfLegs","HelmetDrake","CapeWolf"}},
			{"Padded",new string[]{"ArmorPaddedCuirass","ArmorPaddedGreaves","HelmetPadded","CapeLinen"}},
			{"Padded0",new string[]{"ArmorPaddedCuirass","ArmorPaddedGreaves","CapeLinen"}},
			{"Leather",new string[]{"ArmorLeatherChest","ArmorLeatherLegs","CapeDeerHide"}},
			{"Robes",new string[]{"ArmorDress7","HelmetPointyHat","CapeFeather"}}
		};
		public static void Init()
		{
			if (isInit) return;
			HackValHuman();
			HumanNpc();
			initSpawner();

			OdinPlus.OdinPostRegister(PrefabList);
			Plugin.posZone = (Action)Delegate.Combine(Plugin.posZone, (Action)PostZone);
			isInit = true;
		}

		#region Npcs
		private static void HackValHuman()
		{
			ZNetView.m_forceDisableInit = true;
			var go = Instantiate(Game.instance.m_playerPrefab, OdinPlus.PrefabParent.transform);
			ZNetView.m_forceDisableInit = false;
			go.GetComponent<ZNetView>().m_persistent = true;
			DestroyImmediate(go.GetComponent<PlayerController>());
			DestroyImmediate(go.GetComponent<Talker>());
			DestroyImmediate(go.GetComponent<Skills>());

			var oply = go.GetComponent<Player>();
			var vis = go.GetComponent<VisEquipment>();
			var hum = go.AddComponent<Humanoid>();

			go.AddComponent<HumanVis>();

			hum.CopySonComponet<Humanoid, Player>(oply);

			DestroyImmediate(go.GetComponent<Player>());

			BasicHuman = go;

			go.name = "BasicHuman";

		}
			public static void HumanNpc()
		{
			CreateNPC<HumanFighter>("Fighter1");
			CreateNPC<HumanFighter>("Fighter2");
			CreateNPC<MaterialVillager>("DumbWorker", "MatNPCHuman");
			CreateNPC<HumanMessager>("MessengerNPC", "MessageNPCHuman");
			CreateNPC<HumanWorker>("DumbWorker", "WorkerNPCHuman");
			CreateNPC<VillagePriest>("PriestNPC", "PriestNPCHuman");
			CreateNPC<GuardNPC>("GuardNPC", "GuardVillager");
			CreateNPC<BuilderNPC>("BuilderWorker", "BuilderNPCHuman");
		}
		public static void CreateNPC<T>(string pname, string goname) where T : Component
		{
			ZNetView.m_forceDisableInit = true;
			var go = Instantiate(BasicHuman, OdinPlus.PrefabParent.transform);
			ZNetView.m_forceDisableInit = false;
			CreatePreset(go, pname);
			go.AddComponent<T>();
			go.name = goname;
			PrefabList.Add(go.name, go.gameObject);
			HumanList.Add(go.name, go.gameObject);
			DBG.blogWarning("Create Human" + go.name);
		}
		public static void CreateNPC<T>(string pname) where T : Component
		{
			CreateNPC<T>(pname, pname);
		}
		#endregion Npcs

		#region Tool
		public static GameObject[] RandomVis(string[] list)
		{
			if (list.Length == 0)
			{
				return new GameObject[0];
			}
			GameObject[] items = new GameObject[list.Length];
			int i = 0;
			foreach (var item in list)
			{
				items[i] = ZNetScene.instance.GetPrefab(item);
				i++;
			}

			return items;
		}
		public static Humanoid.ItemSet GetSet(string set_name)
		{
			Humanoid.ItemSet result = new Humanoid.ItemSet();
			string[] list = ArmorSets[set_name];
			result.m_name = set_name;
			var sets = RandomVis(list);
			result.m_items = sets;
			return result;
		}
		public static void PostZone()
		{
			// Tutorial.instance may not be set yet at ZoneSystem.Start time (race condition, see NpcManager.DoInit)
			// Don't let a missing raven prefab abort HackingLoc() below - that's what spawns village NPCs.
			if (Tutorial.instance == null || Tutorial.instance.m_ravenPrefab == null)
			{
				DBG.blogWarning("[HumanManager] Tutorial.instance or m_ravenPrefab null, skipping quest-marker icons this pass");
			}
			else
			{
				var exc_prb = Tutorial.instance.m_ravenPrefab.transform.Find("Munin").gameObject;
				foreach (var item in PrefabList.Values)
				{
					var comp = item.GetComponent<QuestVillager>();
					if (comp)
					{
						var go = comp.gameObject;
						var exc = Instantiate(exc_prb.GetComponentInChildren<Raven>().m_exclamation, Vector3.up *1.3f+go.transform.position, Quaternion.identity, go.transform);
						exc.name = "excOBJ";
						exc.transform.localScale = Vector3.one * 0.5f;
						comp.EXCobj = exc;
					}
				}
			}

			HackingLoc();
		}

		#endregion Tool		

		#region Test
		public static void HumanMobA()
		{
			ZNetView.m_forceDisableInit = true;
			var go = Instantiate(BasicHuman, OdinPlus.PrefabParent.transform);
			ZNetView.m_forceDisableInit = false;

			var vis = go.GetComponent<VisEquipment>();
			var hum = go.GetComponent<Humanoid>();
			//vis.m_isPlayer = false;

			hum.m_health = 1000;
			hum.m_faction = Character.Faction.Players;

			var mai = go.AddComponentcc<MonsterAI>(ZNetScene.instance.GetPrefab("Goblin").GetComponent<MonsterAI>());
			var tame = go.AddComponent<Tameable>();

			hum.m_defaultItems = new GameObject[0];
			hum.m_randomSets = new Humanoid.ItemSet[1] { GetSet("Silver") };
			hum.m_unarmedWeapon = null;
			//hum.m_randomArmor = RandomVis(Armor);
			hum.m_randomWeapon = RandomVis(Weapons);
			hum.m_randomShield = RandomVis(Shield);
			go.name = "HumanMobA";
			PrefabList.Add(go.name, go.gameObject);
		}
		public static void HumanMobB()
		{
			ZNetView.m_forceDisableInit = true;
			var go = Instantiate(BasicHuman, OdinPlus.PrefabParent.transform);
			ZNetView.m_forceDisableInit = false;

			var vis = go.GetComponent<VisEquipment>();
			var hum = go.GetComponent<Humanoid>();
			vis.m_isPlayer = false;

			hum.m_health = 1000;
			hum.m_faction = Character.Faction.PlainsMonsters;

			var mai = go.AddComponentcc<MonsterAI>(ZNetScene.instance.GetPrefab("Goblin").GetComponent<MonsterAI>());
			var tame = go.AddComponent<Tameable>();

			hum.m_defaultItems = new GameObject[0];
			hum.m_unarmedWeapon = null;
			hum.m_randomSets = new Humanoid.ItemSet[1] { GetSet("Silver") };
			//hum.m_randomArmor = RandomVis(Armor);
			hum.m_randomWeapon = RandomVis(Weapons);
			hum.m_randomShield = RandomVis(Shield);

			go.name = "HumanMobB";
			PrefabList.Add(go.name, go.gameObject);
		}
		public static void HumanSpawner()
		{
			ZNetView.m_forceDisableInit = true;
			var go = Instantiate(ZNetScene.instance.GetPrefab("Spawner_Goblin"), OdinPlus.PrefabParent.transform);
			ZNetView.m_forceDisableInit = false;
			var a = go.GetComponent<CreatureSpawner>();
			go.name = "SpawnHuman";
			a.m_creaturePrefab = PrefabList["HumanMobA"];
			PrefabList.Add(go.name, go);
		}
		public static void HackSpawner()
		{
			var a = ZNetScene.instance.GetPrefab("Spawner_Goblin").GetComponent<CreatureSpawner>();
			a.m_creaturePrefab = PrefabList["HumanMobB"];
		}

		public static void HackCamp()
		{
			// Valheim 0.221.12: DungeonDB.RoomData structure changed
			// Old API: RoomData.m_room (direct GameObject reference)
			// New API: RoomData.m_prefab (SoftReference<GameObject>) + RoomInPrefab property
			// This is a test/debug method not used in production
			/*
			var list = DungeonDB.GetRooms();
			var go = list[0].m_prefab.Asset.transform.parent;
			var a = go.GetComponentsInChildren<CreatureSpawner>(true);
			foreach (var item in a)
			{
				if (item.name.StartsWith("Spawner_Goblin"))
				{
					var c = Instantiate(PrefabList["SpawnHuman"], item.transform.parent);
					c.transform.localPosition = item.transform.localPosition;
					item.m_creaturePrefab = PrefabList["HumanMobB"];
					c.name = "SpawnHuman";
				}
			}
			*/
		}

		#endregion Test

		#region Spawner
		public static void initSpawner()
		{
			foreach (var item in HumanList.Keys)
			{
				CreateSpawner(item);
			}
		}
		public static void CreateSpawner(string cname)
		{
			var go = new GameObject(cname + "Spawner");
			go.transform.SetParent(PrefabManager.Root.transform);
			var znv = go.AddComponent<ZNetView>();
			var spn = go.AddComponent<CreatureSpawner>();
			spn.m_creaturePrefab = PrefabList[cname];
			znv.m_persistent = true;

			spn.m_respawnTimeMinuts = 0;
			spn.m_levelupChance = 10;
			spn.m_setPatrolSpawnPoint = true;
			PrefabList.Add(go.name, go);
			DBG.blogWarning("Create Spawner " + go.name);
		}
		#endregion Spawner

		#region OldPreset
		private static void CreaterPresets()
		{
			foreach (var item in presets)
			{
				CreatePreset(item);
			}
		}
		private static void CreatePreset(humanData dat)
		{
			var go = new GameObject();
			go.transform.SetParent(OdinPlus.PrefabParent.transform);
			var hum = go.AddComponentcc<Humanoid>(BasicHuman.GetComponent<Humanoid>());
			var mai = go.AddComponentcc<MonsterAI>(ZNetScene.instance.GetPrefab(dat.prefab).GetComponent<MonsterAI>());
			mai.m_alertedEffects.m_effectPrefabs = new EffectList.EffectData[0];
			mai.m_idleSound.m_effectPrefabs = new EffectList.EffectData[0];
			//hum.m_runSpeed = dat.speed;
			hum.m_health = dat.health;
			hum.m_defaultItems = new GameObject[0];
			hum.m_randomSets = new Humanoid.ItemSet[1] { GetSet(dat.sets) };
			hum.m_unarmedWeapon = null;
			hum.m_randomWeapon = RandomVis(dat.weapons);
			hum.m_randomShield = RandomVis(dat.sheild);

			mai.m_randomMoveInterval = dat.m_randomMoveInterval;
			mai.m_randomMoveRange = dat.m_randomMoveRange;
			mai.m_moveMinAngle = dat.m_moveMinAngle;

			go.name = dat.presetNAME;
			HumanPreset.Add(dat.presetNAME, go);
		}
		private static void CreatePreset(GameObject go, string s)
		{
			var dat = presets.Where(c => c.presetNAME == s).ToArray()[0];
			go.transform.SetParent(OdinPlus.PrefabParent.transform);
			var hum = go.GetComponent<Humanoid>();
			var mai = go.AddComponentcc<MonsterAI>(ZNetScene.instance.GetPrefab(dat.prefab).GetComponent<MonsterAI>());
			mai.m_alertedEffects.m_effectPrefabs = new EffectList.EffectData[0];
			mai.m_idleSound.m_effectPrefabs = new EffectList.EffectData[0];
			//hum.m_runSpeed = dat.speed;
			hum.m_health = dat.health;
			hum.m_defaultItems = new GameObject[0];
			hum.m_randomSets = new Humanoid.ItemSet[1] { GetSet(dat.sets) };
			hum.m_unarmedWeapon = null;
			hum.m_randomWeapon = RandomVis(dat.weapons);
			hum.m_randomShield = RandomVis(dat.sheild);

			mai.m_randomMoveInterval = dat.m_randomMoveInterval;
			mai.m_randomMoveRange = dat.m_randomMoveRange;
			mai.m_moveMinAngle = dat.m_moveMinAngle;
		}
		public static GameObject GetPreset(string prname)
		{
			return HumanPreset[prname];
		}
		public static void AddPreset(GameObject go, string prname)
		{
			go.GetComponent<Humanoid>().CopyOtherComonent(HumanPreset[prname].GetComponent<Humanoid>());
			go.AddComponentcc<MonsterAI>(HumanPreset[prname].GetComponent<MonsterAI>());
		}

		#endregion OldPreset

		#region  HackingLocation	
		public static void HackingLoc()
		{
			HackingRuneStones();
		}

		// Regression root cause (Valheim 0.221.12 / Unity 6 migration):
		// Location prefabs (e.g. "WoodFarm1") are NOT registered in ZNetScene.m_namedPrefabs - only
		// piece/creature/item prefabs are. Locations are streamed via SoftReferenceableAssets and are
		// only ever instantiated by ZoneSystem itself (ZoneSystem.SpawnLocation), which clones each
		// ZNetView-bearing child of the location prefab INDIVIDUALLY into world space - there is no
		// single "village root" GameObject we could parent spawners under even if we found the prefab.
		// The old HackingFarm() code silently fell back to attaching spawners under
		// PrefabManager.Root.transform (a mod-internal utility root, nowhere near any real village),
		// which is why villages generated with zero NPCs.
		//
		// Fix: hook LocationProxy - the small persistent ZNetView marker ZoneSystem spawns at the
		// real world position of every placed location (see LocationProxy.SetLocation/.Awake in
		// assem_valheim). Its ZDO stores the location's name (hashed, ZDOVars.s_location) and is
		// available both the very first time a location is generated (via SetLocation) and every
		// subsequent time the zone streams back in (via Awake alone, reading the persisted ZDO) - so
		// patching both LocationProxy.Awake and LocationProxy.SetLocation (see Plugin.cs) guarantees we
		// never miss the spawn event. TrySeedVillage() below is idempotent (a custom
		// "OdinVillageSeeded" ZDO flag on that same persistent ZDO) so it's safe to call from both hooks
		// and safe across many zone reloads / repeated sessions without ever duplicating NPCs.

		public static void TrySeedVillage(LocationProxy proxy)
		{
			if (ZNet.instance == null || !ZNet.instance.IsServer())
			{
				return;
			}
			var nview = proxy.GetComponent<ZNetView>();
			if (nview == null)
			{
				return;
			}
			var zdo = nview.GetZDO();
			if (zdo == null)
			{
				return;
			}

			int locationHash = zdo.GetInt(ZDOVars.s_location);
			if (locationHash == 0) return;

			string matchedLocation = null;
			foreach (var locationName in FactionManager.VillageLocations)
			{
				if (locationHash == locationName.GetStableHashCode())
				{
					matchedLocation = locationName;
					break;
				}
			}

			if (matchedLocation == null)
			{
				DBG.blogWarning($"[HumanManager] TrySeedVillage NO MATCH hash={locationHash} pos={proxy.transform.position} listCount={FactionManager.VillageLocations.Count}");
				return;
			}
			DBG.blogWarning($"[HumanManager] TrySeedVillage matched: {matchedLocation} at {proxy.transform.position}");
			if (zdo.GetBool("OdinVillageSeeded", false))
			{
				return;
			}
			zdo.Set("OdinVillageSeeded", true);
			// This runs from a Postfix on LocationProxy.Awake, which fires *inside* ZNetScene.CreateObject's
			// own Instantiate() call - ZNetView.m_useInitZDO is still true there (reset only after that
			// Instantiate returns) and m_initZDO was already consumed by the proxy's own ZNetView.Awake.
			// Spawning our ZNetView-bearing spawners synchronously here makes every one of them log a
			// false-positive "Double ZNetview" warning. Deferring one frame runs after CreateObject has
			// returned and reset the flag, so each spawner gets a clean, correctly-created ZDO.
			var pos = proxy.transform.position;
			var rot = proxy.transform.rotation;
			ZNetScene.instance.StartCoroutine(SpawnVillageNextFrame(pos, rot, matchedLocation));
		}

		private static IEnumerator SpawnVillageNextFrame(Vector3 pos, Quaternion rot, string locationName)
		{
			yield return null;
			SpawnVillageAt(pos, rot, locationName);
		}

		private static bool IsFullVillage(string locationName)
		{
			return locationName.StartsWith("WoodFarm") || locationName.StartsWith("WoodVillage") ||
				locationName.StartsWith("WoodHouse") || locationName.StartsWith("GoblinCamp") ||
				locationName.StartsWith("Mistlands_Harbour") || locationName.StartsWith("Mistlands_DvergrTown") ||
				locationName.StartsWith("CharredFortress");
		}

		public static void SpawnVillageAt(Vector3 pos, Quaternion rot, string locationName)
		{
			if (UnityEngine.Random.value > FactionManager.NpcConfig.SpawnChance) return;

			string faction = PickRandomFaction();
			bool fullVillage = IsFullVillage(locationName);

			if (fullVillage)
			{
				PlaceSpawner("MatNPCHumanSpawner", pos + rot * new Vector3(5, 0, 5), rot, faction, FactionManager.NpcConfig.VillagerPatrolRadius);
				PlaceSpawner("MessageNPCHumanSpawner", pos + rot * new Vector3(5.5f, 0, 5.5f), rot, faction, FactionManager.NpcConfig.MessengerPatrolRadius);
				PlaceSpawner("BuilderNPCHumanSpawner", pos + rot * new Vector3(6f, 0, 6f), rot, faction, FactionManager.NpcConfig.BuilderPatrolRadius);
				PlaceSpawner("PriestNPCHumanSpawner", pos + rot * new Vector3(4f, 0, 7f), rot, faction, FactionManager.NpcConfig.PriestPatrolRadius);

				for (int i = 0; i < FactionManager.NpcConfig.GuardsPerVillage; i++)
					PlaceSpawner("GuardVillagerSpawner", pos + rot * new Vector3(10.RollDices(), 0, 10.RollDices()), rot, faction, FactionManager.NpcConfig.GuardPatrolRadius);

				DBG.blogInfo($"[HumanManager] Seeded VILLAGE ({faction}) at {pos} [{locationName}]");
			}
			else
			{
				for (int i = 0; i < FactionManager.NpcConfig.GuardsPerCamp; i++)
					PlaceSpawner("GuardVillagerSpawner", pos + rot * new Vector3(10.RollDices(), 0, 10.RollDices()), rot, faction, FactionManager.NpcConfig.GuardPatrolRadius);

				DBG.blogInfo($"[HumanManager] Seeded CAMP ({faction}) at {pos} [{locationName}]");
			}
		}

		private static void PlaceSpawner(string spawnerName, Vector3 pos, Quaternion rot, string faction, float patrolRadius)
		{
			var prefab = ZNetScene.instance.GetPrefab(spawnerName);
			if (prefab == null)
			{
				DBG.blogWarning($"[HumanManager] Spawner prefab '{spawnerName}' not found");
				return;
			}
			var go = Instantiate(prefab, pos, rot);
			go.name = spawnerName;

			var spawner = go.GetComponent<CreatureSpawner>();
			if (spawner != null)
			{
				spawner.m_setPatrolSpawnPoint = true;
				spawner.m_spawnAtDay = true;
				spawner.m_spawnAtNight = true;
				spawner.m_requireSpawnArea = false;
				spawner.m_spawnInPlayerBase = true;
				spawner.m_triggerDistance = 0f;
			}

			var znv = go.GetComponent<ZNetView>();
			if (znv != null && znv.GetZDO() != null)
			{
				znv.GetZDO().Set("npc_faction", faction);
				znv.GetZDO().Set("npc_patrol_radius", patrolRadius);
			}
		}

		private static void ApplyPatrolRadius(GameObject go, float radius)
		{
			var ai = go.GetComponentInChildren<MonsterAI>(true);
			if (ai != null) ai.m_randomMoveRange = radius;
		}

		private static string PickRandomFaction()
		{
			var candidates = new List<string>();
			foreach (var kv in FactionManager.Factions)
			{
				if (kv.Key == "Neutral" || kv.Key == "Villagers") continue;
				candidates.Add(kv.Key);
			}
			if (candidates.Count == 0) return "Villagers";
			return candidates[UnityEngine.Random.Range(0, candidates.Count)];
		}

		private static void SetNpcFaction(GameObject go, string faction)
		{
			var npc = go.GetComponentInChildren<HumanNPC>(true);
			if (npc != null) npc.FactionName = faction;
			var znv = go.GetComponentInChildren<ZNetView>(true);
			if (znv != null && znv.GetZDO() != null)
				znv.GetZDO().Set("npc_faction", faction);
		}

		private static readonly string[] rstones = new string[] { "Runestone_Meadows", "Runestone_Swamps", "Runestone_BlackForest" };
		public static void HackingRuneStones()
		{
			// TODO: Valheim 0.221.12 - ZoneSystem.ZoneLocation API changed, m_location property no longer exists
			// This is a test/debug method not used in production
			/*
			Transform t = PrefabManager.Root.transform;
			var a = ZoneSystem.instance.m_locations;
			foreach (var item in rstones)
			{
				foreach (var item2 in a)
				{
					if (item2.m_prefabName==item)
					{
						t=item2.m_location.gameObject.transform;
					}
				}
				var go = Instantiate(ZNetScene.instance.GetPrefab("Fighter1" + "Spawner"), t.position, Quaternion.identity, t);
				go.name="Fighter1" + "Spawner";
				var rnd = go.AddComponent<RandomSpawn>();
				rnd.m_chanceToSpawn = 90;
				DBG.blogWarning("hacking " + item);
			}
			*/

		}
		#endregion  HackingLocation

	}
}