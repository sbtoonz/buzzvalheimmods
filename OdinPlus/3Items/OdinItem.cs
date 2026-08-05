using UnityEngine;
using System.Collections.Generic;

namespace OdinPlus
{
	class OdinItem : MonoBehaviour
	{
		//private static Dictionary<string, GameObject> MeadList = new Dictionary<string, GameObject>()
		private static GameObject MeadTasty;
		private static GameObject TrophyGoblinShaman;
		private static GameObject Hammer;
		public static Dictionary<string, Sprite> PetItemList = new Dictionary<string, Sprite>{
			{"ScrollTroll", OdinPlus.TrollHeadIcon},
			{"ScrollWolf", OdinPlus.WolfHeadIcon},
			{"ScrollFenring", OdinPlus.FenringHeadIcon},
			{"ScrollBrute", OdinPlus.BruteHeadIcon},
			{"ScrollDverger", OdinPlus.DvergerHeadIcon}
			};
		// Companion empty PieceTable for BlueprintTool - Hud.TogglePieceSelection is patched (Plugin.cs)
		// to recognize this exact instance and open the Blueprint Browser instead of the vanilla window.
		public static PieceTable BlueprintToolPieceTable;
			public static Dictionary<string, GameObject> ObjectList = new Dictionary<string, GameObject>();
		
		public static GameObject Root;

		#region Mono
		private void Awake()
		{
			Root = new GameObject("ObjectList");
			Root.transform.SetParent(OdinPlus.PrefabParent.transform);
			Root.SetActive(false);

			var objectDB = ObjectDB.instance;
			MeadTasty = objectDB.GetItemPrefab("MeadTasty");
			TrophyGoblinShaman = objectDB.GetItemPrefab("TrophyGoblinShaman");
			Hammer = objectDB.GetItemPrefab("Hammer");

			InitLegacy();
			InitPetItem();
			InitBlueprintTool();

			OdinPlus.OdinPreRegister(ObjectList, nameof(ObjectList));

		}
		#endregion Mono

		#region Legacy
		private static void InitLegacy()
		{
			string name = "OdinLegacy";
			GameObject go = Instantiate(TrophyGoblinShaman, Root.transform);
			go.name = "OdinLegacy";
			var id = go.GetComponent<ItemDrop>().m_itemData.m_shared;
			id.m_name = "$op_" + name + "_name";
			id.m_icons[0] = OdinPlus.OdinLegacyIcon;
			id.m_description = "$op_" + name + "_desc";
			id.m_itemType = ItemDrop.ItemData.ItemType.None;

			id.m_maxStackSize = 10;
			id.m_maxQuality = 5;

			ObjectList.Add(name, go);

		}

		#endregion Legacy

		#region PetItems
		private void InitPetItem()
		{
			foreach (var pet in PetItemList)
			{
				CreatePetItemPrefab(pet.Key, pet.Value);
			}
		}
		private void CreatePetItemPrefab(string name, Sprite icon)
		{
			GameObject go = Instantiate(MeadTasty, Root.transform);
			go.name = name;

			var id = go.GetComponent<ItemDrop>().m_itemData.m_shared;
			id.m_name = "$op_" + name + "_name";
			id.m_icons[0] = icon;
			id.m_description = "$op_" + name + "_desc";
			id.m_consumeStatusEffect = OdinSE.SElist[name];
			id.m_itemType = ItemDrop.ItemData.ItemType.Consumable;

			go.GetComponent<ItemDrop>().m_itemData.m_quality = 4;
			id.m_maxQuality = 5;

			ObjectList.Add(name, go);
		}
		#endregion PetItems

		#region BlueprintTool
		private static void InitBlueprintTool()
		{
			string name = "BlueprintTool";
			GameObject go = Instantiate(Hammer, Root.transform);
			go.name = name;

			var id = go.GetComponent<ItemDrop>().m_itemData.m_shared;
			id.m_name = "$op_" + name + "_name";
			id.m_description = "$op_" + name + "_desc";

			var tableGo = new GameObject("_BlueprintToolPieceTable");
			tableGo.transform.SetParent(Root.transform);
			BlueprintToolPieceTable = tableGo.AddComponent<PieceTable>();
			// This tool only clones whole blueprints (see BlueprintPlacer) - it must never behave like
			// a real hammer for removing/destroying existing world pieces or feasts.
			BlueprintToolPieceTable.m_canRemovePieces = false;
			BlueprintToolPieceTable.m_canRemoveFeasts = false;
			// Pre-populate the (empty) per-category piece lists via the same path vanilla's own
			// Player.UpdateEquipmentStatuses uses - m_pieces is empty so the player arg is never
			// dereferenced, but this avoids a window where m_availablePieces.Count == 0 causes an
			// IndexOutOfRangeException in PieceTable.GetAvailablePiecesInCategory/GetSelectedIndex
			// if anything queries the table before vanilla code populates it on its own.
			BlueprintToolPieceTable.UpdateAvailable(new HashSet<string>(), null, false, false);
			id.m_buildPieces = BlueprintToolPieceTable;

			ObjectList.Add(name, go);
		}
		#endregion BlueprintTool

		#region Tool
		public static ItemDrop.ItemData GetItemData(string name)
		{
			return ObjectList[name].GetComponent<ItemDrop>().m_itemData;
		}
		public static GameObject GetObject(string name)
		{
			return ObjectList[name];
		}

		#endregion Tool
	}
}