using System;
using UnityEngine;

namespace OdinPlus
{
	public class VillagePriest : QuestVillager, Hoverable, Interactable, OdinInteractable
	{
		protected override void Awake()
		{
			base.Awake();
			ChoiceList = new string[2] { "$op_talk", "Train Skills" };
		}

		public override void Choice0()
		{
			Say("Greetings, traveler. I am the village priest. I can help you improve your skills... for a price.");
		}

		public void Choice1()
		{
			Say("Press [1-8] to train a skill:\n" +
				"[1] Swords (100 gold)\n" +
				"[2] Axes (100 gold)\n" +
				"[3] Bows (100 gold)\n" +
				"[4] Blocking (100 gold)\n" +
				"[5] Running (100 gold)\n" +
				"[6] Jumping (100 gold)\n" +
				"[7] Sneak (100 gold)\n" +
				"[8] Swimming (100 gold)");
		}

		public override bool UseItem(Humanoid user, ItemDrop.ItemData item)
		{
			// Accept gold coins for skill training
			if (item.m_shared.m_name == "$item_coins")
			{
				var player = user as Player;
				if (player == null) return false;

				int cost = 100;
				if (player.GetInventory().CountItems("$item_coins") >= cost)
				{
					Say("Give me the gold, then press a number [1-8] to choose your skill!");
					return false; // Don't consume yet - wait for skill choice
				}
				else
				{
					Say($"Training costs {cost} gold coins. You don't have enough!");
					return true;
				}
			}

			return false;
		}

		public override void SecondaryInteract(Humanoid user)
		{
			// Handle skill selection via number keys
			var player = user as Player;
			if (player == null) return;

			int cost = 100;
			if (player.GetInventory().CountItems("$item_coins") < cost)
			{
				Say($"You need {cost} gold coins for training!");
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

				Say($"Your {skillToRaise} skill has improved! (+{raiseAmount})");

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

			string text = $"<color=#ADD8E6FF>{m_name} (Priest)</color>\n";
			text += $"<color=white>Skill Training: 100 gold per skill</color>\n";
			text += "[<color=yellow><b>$KEY_Use</b></color>] Talk\n";
			text += $"[<color=yellow><b>{Plugin.KS_SecondInteractkey.Value.MainKey}</b></color>] Train Skills";
			return text;
		}
	}
}
