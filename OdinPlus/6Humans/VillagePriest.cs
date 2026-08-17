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
			ChoiceList = new[] { "$op_talk", "$op_priest_train" };
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
			if(item.m_shared.m_name == "$item_coins")
			{
				var player = user as Player;
				if(player == null) return false;

				var cost = 100;
				if(player.GetInventory().CountItems("$item_coins") >= cost)
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
			if(player == null) return;

			var cost = 100;
			if(player.GetInventory().CountItems("$item_coins") < cost)
			{
				Say("$op_priest_notenough");
				return;
			}

			var skillToRaise = Skills.SkillType.None;
			var raiseAmount = 5f; // Raise skill by 5 levels

			if(Input.GetKeyDown(KeyCode.Alpha1)) skillToRaise = Skills.SkillType.Swords;
			else if(Input.GetKeyDown(KeyCode.Alpha2)) skillToRaise = Skills.SkillType.Axes;
			else if(Input.GetKeyDown(KeyCode.Alpha3)) skillToRaise = Skills.SkillType.Bows;
			else if(Input.GetKeyDown(KeyCode.Alpha4)) skillToRaise = Skills.SkillType.Blocking;
			else if(Input.GetKeyDown(KeyCode.Alpha5)) skillToRaise = Skills.SkillType.Run;
			else if(Input.GetKeyDown(KeyCode.Alpha6)) skillToRaise = Skills.SkillType.Jump;
			else if(Input.GetKeyDown(KeyCode.Alpha7)) skillToRaise = Skills.SkillType.Sneak;
			else if(Input.GetKeyDown(KeyCode.Alpha8)) skillToRaise = Skills.SkillType.Swim;

			if(skillToRaise != Skills.SkillType.None)
			{
				// Remove payment
				player.GetInventory().RemoveItem("$item_coins", cost);

				// Raise skill
				player.GetSkills().CheatRaiseSkill(skillToRaise.ToString(), raiseAmount);

				Say("$op_priest_success");

				// Reputation bonus
				var playerID = player.GetZDOID().ToString();
				if(ZNet.instance.IsServer())
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
			if(m_hum.m_faction != Character.Faction.Players) return "";

			var n = $"<color=lightblue><b>{Localization.instance.Localize(m_name)}</b></color>";
			n += "\n$op_priest_cost";
			n += "\n[<color=yellow><b>$KEY_Use</b></color>] $op_talk";
			n += $"\n[<color=yellow><b>{Plugin.SecondInteractKey.MainKey}</b></color>] $op_priest_train";
			return Localization.instance.Localize(n);
		}
	}
}
