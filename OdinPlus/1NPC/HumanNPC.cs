using System.Reflection;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using BepInEx.Logging;
using UnityEngine;

namespace OdinPlus
{
	public class HumanNPC : OdinNPC, Hoverable, Interactable, OdinInteractable
	{
		#region Fields

		#region References
		protected ZNetView m_nview;
		protected VisEquipment m_vis;
		protected Animator m_ani;
		protected Humanoid m_hum;
		protected MonsterAI monsterAI;
		#endregion References

		#region Internal
		public string[] ChoiceList = { "$op_talk" };
		int index = 0;
		string currentChoice = "";
		// Faction this NPC belongs to (e.g. for blueprint AllowedFactions gating, reputation, etc).
		// Defaults to the shared village faction; HumanManager can override per-instance at spawn time.
		public string FactionName = "Villagers";
		#endregion Internal

		#endregion Fields

		#region Mono

		protected virtual void Awake()
		{
			monsterAI = GetComponent<MonsterAI>();
			m_talker = gameObject;
			m_nview = GetComponent<ZNetView>();
			m_ani = GetComponentInChildren<Animator>();
			m_hum = GetComponent<Humanoid>();
			m_vis = GetComponent<VisEquipment>();
			currentChoice = ChoiceList[index];
			if(m_nview != null && m_nview.GetZDO() != null)
			{
				var zdoFaction = m_nview.GetZDO().GetString("npc_faction", "");
				if(!string.IsNullOrEmpty(zdoFaction))
					FactionName = zdoFaction;
			}
			ApplyFactionCape();
			PerformanceManager.Instance.RegisterNPC(this);
		}

		void OnDestroy()
		{
			PerformanceManager.Instance.UnregisterNPC(this);
		}

		#endregion Mono

		#region Faction

		protected void ApplyFactionCape()
		{
			if(m_nview == null || m_nview.GetZDO() == null) return;
			if(!FactionManager.Factions.TryGetValue(FactionName, out var def)) return;
			if(string.IsNullOrEmpty(def.Cape)) return;
			if(m_vis != null)
				m_vis.SetShoulderItem(def.Cape, 0);
		}

		public void ChangeFaction(Character target)
		{
			m_hum.m_faction = Character.Faction.PlainsMonsters;
		}

		public void ChangeFaction(Character.Faction f)
		{
			m_hum.m_faction = f;
		}

		#endregion Faction

		#region Valheim Interface

		void RemoveUnusedComp()
		{
			foreach(var comp in gameObject.GetComponents<UnityEngine.Component>())
			{
				if(!(comp is Transform) && !(comp is HumanNPC) && !(comp is CapsuleCollider) && !(comp is ZNetView) && !(comp is VisEquipment) && !(comp is MonsterAI) && !(comp is Humanoid))
					DestroyImmediate(comp);
			}
		}

		public override void Say(string text)
		{
			Say(text, "emote_wave");
		}

		public void Say(string text, string emote)
		{
			if(m_hum.m_faction != Character.Faction.Players) return;
			text = Localization.instance.Localize(text);
			var tname = Localization.instance.Localize(m_name);
			Chat.instance.SetNpcText(m_talker, Vector3.up * 1.5f, 60f, 5, tname, text, false);
			m_ani.SetTrigger(emote);
		}

		public override bool Interact(Humanoid user, bool hold, bool alt)
		{
			if(hold) return false;
			Invoke($"Choice{index}", 0f);
			return true;
		}

		public virtual void Choice0()
		{
			Say("Greeting");
		}

		public override void SecondaryInteract(Humanoid user)
		{
			index += 1;
			if(index + 1 > ChoiceList.Length)
				index = 0;
			currentChoice = ChoiceList[index];
		}

		public override string GetHoverText()
		{
			if(m_hum.m_faction != Character.Faction.Players) return "";
			var n = $"<color=lightblue><b>{m_name}</b></color>";
			n += $"\n[<color=yellow><b>$KEY_Use</b></color>]{currentChoice}";
			n += "\n[<color=yellow><b>1-8</b></color>]$op_offer";
			n += $"\n<color=yellow><b>[{Plugin.SecondInteractKey.MainKey}]</b></color>$op_switch";
			return Localization.instance.Localize(n);
		}

		public override string GetHoverName() => Localization.instance.Localize(m_name);

		public override bool UseItem(Humanoid user, ItemDrop.ItemData item) => false;

		#endregion Valheim Interface

		#region Debug
		#endregion Debug
	}
}
