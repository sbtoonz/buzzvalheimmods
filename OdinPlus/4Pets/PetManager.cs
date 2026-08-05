using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEngine;

namespace OdinPlus
{
	class PetManager : MonoBehaviour
	{
		#region var
		private static ZNetScene zns;
		private static GameObject Root;
		private static Dictionary<string, GameObject> PetList = new Dictionary<string, GameObject>();
		public static GameObject TrollIns;
		public static GameObject WolfIns;
		public static GameObject FenringIns;
		public static GameObject BruteIns;
		public static GameObject DvergerIns;
		public static GameObject Indicator;
		public static GameObject excObj;
		public static bool isInit = false;
		#endregion

		#region Main
		private void Awake()
		{
			initIndicator();
		}
		public static void Clear()
		{
			TrollIns = null;
			WolfIns = null;
			FenringIns = null;
			BruteIns = null;
			DvergerIns = null;
			DBG.blogInfo("PetList Clear");
		}
		public static void Init()
		{
			zns = ZNetScene.instance;
			Root = new GameObject("PetPrefab");
			Root.transform.SetParent(OdinPlus.PrefabParent.transform);

			InitTroll();
			InitWolf();
			InitFenring();
			InitBrute();
			InitDverger();

			OdinPlus.OdinPostRegister(PetList);
			isInit = true;
		}
		#endregion Main

		#region Troll
		private static void InitTroll()
		{
			CreateFighterPet("Troll", "TrollPet");
		}
		#endregion

		#region Fenring
		private static void InitFenring()
		{
			CreateFighterPet("Fenring", "FenringPet");
		}
		#endregion

		#region Brute
		private static void InitBrute()
		{
			CreateFighterPet("GoblinBruteBros_nochest", "BrutePet");
		}
		#endregion

		#region Wolf
		private static void InitWolf()
		{
			CreatePackPet("Wolf", "WolfPet");
		}
		#endregion

		#region Dverger
		private static void InitDverger()
		{
			CreatePackPet("Dverger", "DvergerPet");
		}
		#endregion

		#region Factory
		private static void CreateFighterPet(string prefabName, string petName)
		{
			if (zns.GetPrefab(prefabName) == null)
			{
				DBG.blogWarning("Can't find prefab in ZNetScene: " + prefabName);
				return;
			}
			ZNetView.m_forceDisableInit = true;
			var go = Instantiate(zns.GetPrefab(prefabName), Root.transform);
			ZNetView.m_forceDisableInit = false;
			go.name = petName;

			Tameable tame;
			if (!go.TryGetComponent<Tameable>(out tame))
				tame = go.AddComponent<Tameable>();

			go.AddComponent<PetFighter>();

			var hd = go.GetComponent<Humanoid>();
			var mai = go.GetComponent<MonsterAI>();

			var drop = go.GetComponent<CharacterDrop>();
			if (drop != null) DestroyImmediate(drop);

			if (hd != null)
			{
				hd.m_name = hd.m_name + " Pet";
				hd.m_faction = Character.Faction.Players;
			}

			if (mai != null)
				mai.m_consumeItems.Clear();

			SetColor(go);

			if (hd != null && hd.m_randomSets != null && hd.m_randomSets.Length > 1)
				hd.m_randomSets = hd.m_randomSets.Skip(hd.m_randomSets.Length - 1).ToArray();

			PetList.Add(petName, go);
		}

		private static void CreatePackPet(string prefabName, string petName)
		{
			if (zns.GetPrefab(prefabName) == null)
			{
				DBG.blogWarning("Can't find prefab in ZNetScene: " + prefabName);
				return;
			}
			ZNetView.m_forceDisableInit = true;
			var go = Instantiate(zns.GetPrefab(prefabName), Root.transform);
			ZNetView.m_forceDisableInit = false;
			go.name = petName;

			var hum = go.GetComponent<Humanoid>();
			var mai = go.GetComponent<MonsterAI>();
			Tameable tame;
			if (!go.TryGetComponent<Tameable>(out tame))
				tame = go.AddComponent<Tameable>();

			var proc = go.GetComponent<Procreation>();
			if (proc != null) DestroyImmediate(proc);
			var drop = go.GetComponent<CharacterDrop>();
			if (drop != null) DestroyImmediate(drop);

			go.AddComponent<PetPack>();

			SetColor(go);

			if (hum != null)
			{
				hum.m_name = "$op_" + petName.ToLower() + "_name";
				hum.m_faction = Character.Faction.Players;
			}
			if (mai != null)
			{
				mai.m_consumeItems.Clear();
				mai.m_randomMoveInterval = 10000;
				mai.m_randomCircleInterval = 10000;
				mai.m_alertRange = 30;
				mai.m_viewRange = 30;
				mai.m_hearRange = 30;
			}

			var ctn = go.AddComponent<Container>();
			ctn.m_width = 2;
			ctn.m_height = 2;
			ctn.m_name = petName + "Pack";
			var cargoPrefab = zns.GetPrefab("CargoCrate");
			if (cargoPrefab != null)
			{
				var cargoContainer = cargoPrefab.GetComponent<Container>();
				if (cargoContainer != null) ctn.m_bkg = cargoContainer.m_bkg;
			}

			PetList.Add(petName, go);
		}
		#endregion Factory

		#region Feature
		public static GameObject SummonPet(string name)
		{
			var ppfb = ZNetScene.instance.GetPrefab(name);
			if (ppfb == null)
			{
				DBG.blogWarning("Can't summon pet - prefab not found: " + name);
				return null;
			}
			var go = Instantiate(ppfb, Player.m_localPlayer.transform.position + Player.m_localPlayer.transform.forward * 2f + Vector3.up, Quaternion.identity);
			go.GetComponent<Character>().SetLevel(4);
			DBG.InfoCT("You summoned a " + name);
			return go;
		}
		#endregion Feature

		#region Tool
		public static void initIndicator()
		{
			Indicator = new GameObject("Indicator");
			Indicator.transform.SetParent(Plugin.OdinPlusRoot.transform);
			Indicator.AddComponent<StaticTarget>();
			Indicator.AddComponent<CapsuleCollider>();
			Indicator.SetActive(false);
		}
		public static void SetColor(GameObject go)
		{
			var renderer = go.GetComponentInChildren<Renderer>();
			if (renderer == null) return;
			var mat = renderer.material;
			mat.SetFloat("_Hue", 0.3f);
			mat.SetFloat("_Saturation", 0.5f);
			mat.EnableKeyword("_EMISSION");
			mat.SetColor("_EmissionColor", Color.HSVToRGB(0.3f, 0.5f, 0.3f) * 0.1f);
			mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
		}
		#endregion Tool
	}
}
