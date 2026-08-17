using System;
using System.Collections.Generic;
using UnityEngine;
using System.Reflection;
using HarmonyLib;

namespace OdinPlus
{
	class OdinShaman : OdinNPC, Hoverable, Interactable, OdinInteractable
	{
		#region Data

		public Dictionary<string, GoodsDate> GoodsList = new()
		{
			{"TrophyFrostTroll", new GoodsDate { Good = "ScrollTroll", Value = 3 }},
			{"TrophyWolf", new GoodsDate { Good = "ScrollWolf", Value = 3 }},
			{"TrophyFenring", new GoodsDate { Good = "ScrollFenring", Value = 3 }},
			{"TrophyGoblinBruteBrosBrute", new GoodsDate { Good = "ScrollBrute", Value = 5 }},
			{"TrophyDvergr", new GoodsDate { Good = "ScrollDverger", Value = 4 }}
		};

		public struct GoodsDate
		{
			public string Good;
			public int Value;
		}

		public bool crealvl = true;

		#endregion Data

		#region Mono

		void Awake()
		{
			m_name = "$op_shaman";
			m_talker = gameObject;
		}

		#endregion Mono

		// NOTE: component stripping / ZNetView-ZDO cleanup / final placement used to happen
		// here in Start(), but that's a deferred MonoBehaviour lifecycle callback that ran
		// one phase too late - other original components (MonsterAI/Character/ZSyncTransform)
		// could run their own Awake/Start logic first and reposition/reorient the clone using
		// real world data before this cleanup ever got a chance to run. This is now done
		// synchronously in NpcManager.InitShaman() immediately after Instantiate, using the
		// same ZNetView.m_forceDisableInit guard pattern as InitOdinGod(), which also prevents
		// a stale ZNetView from ever being registered into ZNetScene.m_instances in the first
		// place (that dictionary is never cleaned up by ZNetView.OnDestroy() itself).

		#region Utilities

		//remvoe
		bool IsAssemblyExists(string assemblyName)
		{
			foreach(var assembly in AppDomain.CurrentDomain.GetAssemblies())
			{
				if(assembly.FullName.StartsWith(assemblyName))
					return true;
			}
			return false;
		}

		#endregion Utilities

		#region Valheim Interface

		public override bool Interact(Humanoid user, bool hold, bool alt)
		{
			if(hold) return false;
			return true;
		}

		public override void SecondaryInteract(Humanoid user)
		{
		}

		public override string GetHoverText()
		{
			var n = $"<color=lightblue><b>{m_name}</b></color>";
			n += "\n<color=#00FFFF>Summon 5 Creatures:</color>";
			n += "\n  Troll, Wolf, Fenring, Brute, Dverger";
			n += "\n[<color=yellow><b>1-8</b></color>]$op_shaman_offer";
			return Localization.instance.Localize(n);
		}

		public override string GetHoverName() => Localization.instance.Localize(m_name);

		public override bool UseItem(Humanoid user, ItemDrop.ItemData item)
		{
			var name = item.m_dropPrefab.name.Replace("(Clone)", "").Trim();
			DBG.blogInfo($"[OdinShaman] UseItem: '{name}' (original: '{item.m_dropPrefab.name}', shared: '{item.m_shared.m_name}')");
			if(GoodsList.ContainsKey(name))
			{
				var gd = GoodsList[name];
				if(item.m_stack >= gd.Value)
				{
					var goodItemData = OdinItem.GetItemData(gd.Good);
					if(user.GetInventory().AddItem(goodItemData))
					{
						user.GetInventory().RemoveItem(item, gd.Value);
						Say(goodItemData.m_shared.m_description);
						return true;
					}
					DBG.InfoCT("$op_inventory_full");
					return true;
				}
				Say("$op_shaman_notenough");
				return true;
			}
			DBG.blogWarning($"[OdinShaman] Item '{name}' not in GoodsList. Available: {string.Join(", ", GoodsList.Keys)}");
			Say("$op_shaman_no");
			return true;
		}

		#endregion Valheim Interface
	}
}
