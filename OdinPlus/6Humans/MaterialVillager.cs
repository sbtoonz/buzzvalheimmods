using System;
using UnityEngine;
using System.Collections.Generic;

namespace OdinPlus
{
	public class MaterialVillager : QuestVillager, Hoverable, Interactable, OdinInteractable
	{
		public readonly string[] m_materials = new string[] { "Wood", "Stone" };
		public string m_item = "";
		protected override void Awake()
		{
			base.Awake();
			m_name = "Supplier";
			var zdo = m_nview.GetZDO();
			m_item = zdo.GetString("Qmat","");
			if (m_item=="")
			{
				m_item=m_materials.GetRandomElement();
				zdo.Set("Qmat",m_item);
			}
		}
		public override void Choice0()
		{
			var builder = FindNearestBuilder();
			if (builder != null && builder.GetNextTarget() != null)
			{
				Say($"Bring materials for the builder! They need:\n{builder.FormatRemainingPublic()}");
				return;
			}
			string n = string.Format("I could use some <color=yellow><b>{0}</b></color> to build our home", m_item);
			Say(n);
		}
		public override bool UseItem(Humanoid user, ItemDrop.ItemData item)
		{
			string key = item.m_dropPrefab != null ? item.m_dropPrefab.name : null;
			if (string.IsNullOrEmpty(key)) return false;

			// Find nearest builder and deliver materials to them
			var builder = FindNearestBuilder();
			if (builder != null && builder.IsNeededResourcePublic(key))
			{
				int count = Mathf.Min(item.m_stack, 50);
				user.GetInventory().RemoveItem(item.m_shared.m_name, count);
				builder.ReceiveMaterials(key, count);
				OdinData.AddCredits(Mathf.Max(1, count / 5), true);
				Say($"I'll bring this {key} to the builder!");
				return true;
			}

			// Fallback: original behavior
			if (!IsQuestReady()) return false;
			var inv = Player.m_localPlayer.GetInventory();
			string iname = Tweakers.GetItemData(m_item).m_shared.m_name;
			int maxStack = Tweakers.GetItemData(m_item).m_shared.m_maxStackSize;
			if (inv.CountItems(iname) >= maxStack)
			{
				inv.RemoveItem(iname, maxStack);
				OdinData.AddCredits(30, true);
				Say("$op_human_thx");
				ResetQuestCD();
				return true;
			}
			Say("$op_human_noteought");
			return true;
		}

		private BuilderNPC FindNearestBuilder()
		{
			BuilderNPC best = null;
			float bestDist = 50f;
			foreach (var villager in HumanVillager.Villagers)
			{
				var builder = villager as BuilderNPC;
				if (builder == null) continue;
				float dist = Vector3.Distance(transform.position, builder.transform.position);
				if (dist < bestDist)
				{
					bestDist = dist;
					best = builder;
				}
			}
			return best;
		}
	}
}
