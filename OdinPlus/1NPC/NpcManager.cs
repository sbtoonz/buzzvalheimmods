using System.Reflection;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace OdinPlus
{

	class NpcManager : MonoBehaviour
	{
		public static bool IsInit = false;
		public static GameObject Root;
		public static GameObject terrain;
		public static OdinGod m_odinGod;
		public static OdinTrader m_odinPot;
		public static OdinShaman m_odinShaman;
		public static GameObject RavenPrefab;
		public static OdinMunin m_odinMunin;

		//public static ZDO PlayerZDO;
		/* public static OdinTrader m_odinChest;
		public static OdinTrader m_shamanChest;
		public static OdinGoblin m_odinGoblin; */

		#region Main
		private void Awake()
		{
			if (Tutorial.instance == null || Tutorial.instance.m_ravenPrefab == null)
			{
				DBG.blogWarning("[NpcManager] Tutorial.instance or m_ravenPrefab null, deferring init");
				Invoke(nameof(DeferredInit), 1f);
				return;
			}
			DoInit();
		}
		private void DeferredInit()
		{
			if (IsInit) return;
			if (Tutorial.instance == null || Tutorial.instance.m_ravenPrefab == null)
			{
				DBG.blogWarning("[NpcManager] Tutorial still null after defer, skipping excObj init");
				Init();
				return;
			}
			DoInit();
		}
		private void DoInit()
		{
			RavenPrefab = Tutorial.instance.m_ravenPrefab.transform.Find("Munin").gameObject;
			if (PetManager.excObj == null)
			{
				PetManager.excObj = Instantiate(RavenPrefab.GetComponentInChildren<Raven>().m_exclamation, Vector3.zero, Quaternion.identity, PetManager.Indicator.transform);
				PetManager.excObj.gameObject.GetComponentInChildren<Renderer>().material.SetColor("_EmissionColor", Color.red);
				PetManager.excObj.gameObject.GetComponentInChildren<Renderer>().material.color = Color.red;
			}
			Init();
		}
		private void OnDestroy()
		{
			Clear();
		}
		public static void Init()
		{
			Root = new GameObject("OdinNPCs"); ;
			Root.SetActive(false);
			Root.transform.SetParent(OdinPlus.Root.transform);
			Root.transform.position = Vector3.zero;
			//InitTerrain();
			InitOdinGod();
			InitOdinPot();
			InitOdinChest();
			InitShaman();
			InitMunin();

			Root.SetActive(true);

			// TODO: 0.221.12 - m_prefab is SoftReference, skip ForceField setup
			// var pfab = ZoneSystem.instance.m_locations[85].m_prefab.transform.Find("ForceField");
			// var nmz = Instantiate(pfab, Root.transform);
			// nmz.transform.localScale = Vector3.one * 10;
		}
		public static void test()
		{
			m_odinShaman.gameObject.transform.Rotate(0, 30f, 0);
		}
		public static void Clear()
		{
			m_odinGod.RestTerrian();
			IsInit = false;
			Destroy(Root);
		}

		#endregion Main	
		#region NPCs
		private static void InitTerrain()
		{
			if (terrain == null)
			{
				terrain = new GameObject("terrain");
				//terrain.AddComponent<ZNetView>();
				//terrain.AddComponent<Piece>();
				var tm = terrain.AddComponent<TerrainModifier>();
				terrain.gameObject.transform.SetParent(Root.transform);
				terrain.gameObject.transform.localPosition = new Vector3(0, 0, 0);
				tm.m_playerModifiction = false;
				tm.m_levelOffset = 0.01f;

				tm.m_level = true;
				tm.m_levelRadius = 4f;
				tm.m_square = false;

				tm.m_smooth = false;

				tm.m_smoothRadius = 9.5f;
				tm.m_smoothPower = 3f;


				tm.m_paintRadius = 3.5f;
				tm.m_paintCleared = true;
				tm.m_paintType = TerrainModifier.PaintType.Dirt;
			}
		}
		private static void InitOdinGod()
		{
			var podin = ZNetScene.instance.GetPrefab("odin");
			// Prevent ZNetView.Awake() from ever registering a ZDO/m_instances entry for this
			// decorative clone - ZNetView.OnDestroy() does NOT clean up ZNetScene.m_instances,
			// so destroying it after the fact (previous approach) can leave null entries that
			// make ZNetScene.RemoveObjects() throw and abort every call (kills distant-object
			// culling -> tanks FPS). m_useInitZDO/m_forceDisableInit are static fields also used
			// by the game's own concurrent spawn calls, so keep this window as small as possible.
			ZNetView.m_forceDisableInit = true;
			var odin = Instantiate(podin, Root.transform);
			ZNetView.m_forceDisableInit = false;
			var ani = odin.GetComponentInChildren<Animator>();

			// m_forceDisableInit above prevents ZNetView.Awake() from registering,
			// but strip the component entirely so it doesn't interfere later.
			var znv = odin.GetComponent<ZNetView>();
			if (znv != null) DestroyImmediate(znv);
			DestroyImmediate(odin.GetComponent<ZSyncTransform>());
			DestroyImmediate(odin.GetComponent<Odin>());
			DestroyImmediate(odin.GetComponent<Rigidbody>());
			Aoe[] aoes = odin.GetComponentsInChildren<Aoe>();
			EffectArea[] fxas = odin.GetComponentsInChildren<EffectArea>();
			foreach (var item in aoes)
			{
				DestroyImmediate(item);
			}
			foreach (var item in fxas)
			{
				DestroyImmediate(item);
			}

			//ani.runtimeAnimatorController=ZNetScene.instance.GetPrefab("Haldor").GetComponentInChildren<Animator>().runtimeAnimatorController;
			//var stf = odin.transform.Find("staff");
			//var hand = odin.transform.Find("RightHand");
			//stf.SetParent(hand);


			m_odinGod = odin.AddComponent<OdinGod>();
			odin.transform.localPosition = new Vector3(0f, 0, 0f);
		}
		private static void InitOdinPot()
		{
			// CopyChildren only clones the prefab's CHILDREN (not its root), so the piece's own
			// root-level ZNetView/Piece components are never copied - no m_instances entry risk.
			var pfire = ZNetScene.instance.GetPrefab("fire_pit");
			var pcaul = ZNetScene.instance.GetPrefab("piece_cauldron");
			var fire = CopyChildren(pfire);
			var caul = CopyChildren(pcaul);
			fire.transform.SetParent(Root.transform);
			caul.transform.SetParent(Root.transform);

			fire.transform.localPosition = new Vector3(1.5f, 0, -0.5f);
			caul.transform.localPosition = new Vector3(1.5f, 0, -0.5f);

			Destroy(fire.transform.Find("PlayerBase").gameObject);
			fire.transform.Find("_enabled_high").gameObject.SetActive(true);
			caul.transform.Find("HaveFire").gameObject.SetActive(true);

			m_odinPot = caul.AddComponent<OdinTrader>();
			m_odinPot.m_name = "$op_pot_name";
			OdinPlus.traderNameList.Add(m_odinPot.m_name);
			m_odinPot.m_talker = m_odinGod.gameObject;

			foreach (var item in OdinMeads.MeadList)
			{
				m_odinPot.m_items.Add(new Trader.TradeItem
				{
					m_prefab = item.Value.GetComponent<ItemDrop>(),
					m_stack = 1,
					m_price = OdinData.MeadsValue[item.Key]
				});
			}
		}
		private static void InitOdinChest()
		{
		}
		private static void InitShaman()
		{
			var prefab = ZNetScene.instance.GetPrefab("GoblinShaman");

			// See InitOdinGod() for why this guard matters: prevents ZNetView.Awake() from
			// registering a ZDO/m_instances entry for this decorative clone in the first place,
			// instead of destroying components after the fact (which was the previous approach
			// in OdinShaman.Start() - a deferred MonoBehaviour callback that ran too late,
			// letting the original creature's MonsterAI/Character/ZSyncTransform components run
			// at least one Awake/Start pass first and reposition/rotate the clone using real
			// world/ZDO data before cleanup ever got a chance to strip them).
			ZNetView.m_forceDisableInit = true;
			var go = Instantiate(prefab, Root.transform);
			ZNetView.m_forceDisableInit = false;

			var znv = go.GetComponent<ZNetView>();
			if (znv != null) DestroyImmediate(znv);

			// Bug fix: this used to destroy RandomAnimation on 'prefab' (the shared template
			// used for every future GoblinShaman spawn in the world), not on 'go' (our clone).
			var randomAnim = go.GetComponent<RandomAnimation>();
			if (randomAnim != null) DestroyImmediate(randomAnim);

			// CharacterAnimEvent lives on a CHILD (the model/rig object), not the root, and its
			// Awake() does `GetComponentInParent<Character>().GetComponent<ZNetView>()` with no
			// null-check. It must be stripped before Root.SetActive(true) fires Awake() on the
			// whole subtree, otherwise it NREs once Humanoid/Character below is gone.
			foreach (var animEvent in go.GetComponentsInChildren<CharacterAnimEvent>(true))
			{
				DestroyImmediate(animEvent);
			}

			DestroyImmediate(go.GetComponent<ZSyncAnimation>());
			DestroyImmediate(go.GetComponent<ZSyncTransform>());
			DestroyImmediate(go.GetComponent<MonsterAI>());
			DestroyImmediate(go.GetComponent<VisEquipment>());
			DestroyImmediate(go.GetComponent<CharacterDrop>());
			DestroyImmediate(go.GetComponent<Humanoid>());
			DestroyImmediate(go.GetComponent<FootStep>());
			DestroyImmediate(go.GetComponent<Rigidbody>());
			foreach (var comp in go.GetComponents<Component>())
			{
				if (!(comp is Transform) && !(comp is CapsuleCollider))
				{
					DestroyImmediate(comp);
				}
			}

			var npc = go.AddComponent<OdinShaman>();
			npc.m_name = "$op_shaman";
			m_odinShaman = npc;

			// Set final placement AFTER all stripping/cleanup so nothing can move it afterward.
			go.transform.localPosition = new Vector3(-1.6f, 0, -0.6f);
			go.transform.Rotate(0, 30f, 0);
		}
		private static void InitMunin()
		{
			var go = Instantiate(RavenPrefab, Root.transform);

			DestroyImmediate(go.transform.Find("exclamation").gameObject);
			DestroyImmediate(go.transform.GetComponentInChildren<Light>());
			DestroyImmediate(go.GetComponent<Raven>());

			m_odinMunin = go.AddComponent<OdinMunin>();

			//var ani = go.GetComponentInChildren<Animator>();
			//DestroyImmediate(ani);

			go.transform.localPosition = new Vector3(2.7f, 0, 1.6f);
		}
		private static void InitGoblin()
		{

		}

		#endregion NPCs
		#region Utilities
		public static GameObject CopyChildren(GameObject prefab)
		{
			int cc = prefab.transform.childCount;
			GameObject r = new GameObject(prefab.name);
			for (int i = 0; i < cc; i++)
			{
				var o = prefab.transform.GetChild(i).gameObject;
				var a = Instantiate(o, r.transform);
				a.name = o.name;
			}
			return r;
		}
		public static void CopyComponent(Component original, GameObject destination)
		{
			System.Type type = original.GetType();
			Component copy = destination.AddComponent(type);
			// Copied fields can be restricted with BindingFlags
			FieldInfo[] fields = type.GetFields();
			PropertyInfo[] props = type.GetProperties();
			foreach (FieldInfo field in fields)
			{
				field.SetValue(copy, field.GetValue(original));
			}
			//foreach(PropertyInfo p in props)
			//{
			//    props.SetValue(copy, props.GetValue(original));
			//}
			return;
		}

		#endregion Utilities
	}
}
