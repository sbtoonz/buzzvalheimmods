using System;
using UnityEngine;

namespace OdinPlus
{
	public class VillagePriest : QuestVillager, Hoverable, Interactable, OdinInteractable
	{
		protected override void Awake()
		{
			base.Awake();
			m_name = "$op_priest_name";
			ChoiceList = new string[2] { "$op_talk", "$op_priest_train" };
		}

		public override void Choice0()
		{
			Say("$op_priest_greet");
		}

		public void Choice1()
		{
			Say("$op_priest_skills");
		}

		public override bool UseItem(Humanoid user, ItemDrop.ItemData item)
		{
			if (item.m_shared.m_name == "$item_coins")
			{
				var player = user as Player;
				if (player == null) return false;

				int cost = 100;
				if (player.GetInventory().CountItems("$item_coins") >= cost)
				{
					Say("$op_priest_ready");
					return false;
				}
				else
				{
					Say("$op_priest_notenough");
					return true;
				}
			}

			return false;
		}

		public override void SecondaryInteract(Humanoid user)
		{
			var player = user as Player;
			if (player == null) return;

			int cost = 100;
			if (player.GetInventory().CountItems("$item_coins") < cost)
			{
				Say("$op_priest_notenough");
				return;
			}

			Skills.SkillType skillToRaise = Skills.SkillType.None;
			float raiseAmount = 5f; // Raise skill by 5 levels

			if (Input.GetKeyDown(KeyCode.Alpha1)) skillToRaise = Skills.SkillType.Swords;
			else if (Input.GetKeyDown(KeyCode.Alpha2)) skillToRaise = Skills.SkillType.Axes;
			else if (Input.GetKeyDown(KeyCode.Alpha3)) skillToRaise = Skills.SkillType.Bows;
			else if (Input.GetKeyDown(KeyCode.Alpha4)) skillToRaise = Skills.SkillType.Blocking;
			else if (Input.GetKeyDown(KeyCode.Alpha5)) skillToRaise = Skills.SkillType.Run;
			else if (Input.GetKeyDown(KeyCode.Alpha6)) skillToRaise = Skills.SkillType.Jump;
			else if (Input.GetKeyDown(KeyCode.Alpha7)) skillToRaise = Skills.SkillType.Sneak;
			else if (Input.GetKeyDown(KeyCode.Alpha8)) skillToRaise = Skills.SkillType.Swim;

			if (skillToRaise != Skills.SkillType.None)
			{
				// Remove payment
				player.GetInventory().RemoveItem("$item_coins", cost);

				// Raise skill
				player.GetSkills().CheatRaiseSkill(skillToRaise.ToString(), raiseAmount);

				Say("$op_priest_success");

				// Reputation bonus
				string playerID = player.GetZDOID().ToString();
				if (ZNet.instance.IsServer())
				{
					FactionManager.ModifyReputation(playerID, FactionName, 5, true); // Small rep bonus
				}
				else
				{
					ZRoutedRpc.instance.InvokeRoutedRPC(0L, "ReputationChange", playerID, FactionName, 5);
				}
			}
		}

		public override string GetHoverText()
		{
			if (m_hum.m_faction != Character.Faction.Players) return "";

			string n = string.Format("<color=lightblue><b>{0}</b></color>", Localization.instance.Localize(m_name));
			n += "\n<color=white>$op_priest_cost</color>";
			n += "\n[<color=yellow><b>$KEY_Use</b></color>] $op_talk";
			n += String.Format("\n[<color=yellow><b>{0}</b></color>] $op_priest_train", Plugin.SecondInteractKey.MainKey);
			return Localization.instance.Localize(n);
		}
	}
}
