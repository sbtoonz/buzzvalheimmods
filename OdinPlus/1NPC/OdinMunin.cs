using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace OdinPlus
{
	class OdinMunin : OdinNPC
	{
		#region Fields

		string[] choice = new string[] { "$op_munin_c1", "$op_munin_c2", "$op_munin_c3", "$op_munin_c4" };
		int index = 0;
		string currentChoice = "";
		float timer = 0f;
		float questCD = 60f;
		Animator m_animator;
		internal static OdinMunin instance;

		#endregion Fields

		#region Mono

		void Awake()
		{
			instance = this;
			m_name = "$op_munin_name";
			m_talker = gameObject;
			currentChoice = choice[index];
			m_animator = GetComponentInChildren<Animator>();
		}

		void Start()
		{
			gameObject.transform.Rotate(0, -30f, 0);
			m_animator.SetTrigger("teleportin");
			m_animator.SetTrigger("talk");
		}

		void Update()
		{
			if(timer > 0)
				timer -= Time.deltaTime;
		}

		void OnDestroy()
		{
			instance = null;
		}

		#endregion Mono

		#region Feature

		void CreatSideQuest()
		{
			if(timer > 0)
			{
				var n = $"<color=yellow><b>{Mathf.CeilToInt(timer)}</b></color>";
				Say($"$op_munin_cd {n}");
				return;
			}
			if(QuestManager.instance.Count() >= 10)
			{
				Say("$op_munin_questfulll");
				return;
			}
			QuestManager.instance.CreateRandomQuest();
			Say("$op_munin_wait_hug");
			timer = questCD;
		}

		void GiveUpQuest()
		{
			if(QuestManager.instance.HasQuest())
			{
				var n = Localization.instance.Localize("$op_munin_giveup");
				TextInput.instance.RequestText(new TR_Giveup(), n, 3);
				ResetTimer();
				return;
			}
			Say("$op_munin_noquest");
		}

		void ChangeLevel()
		{
			if(QuestManager.instance.Level == QuestManager.MaxLevel)
			{
				QuestManager.instance.Level = 1;
				return;
			}
			QuestManager.instance.Level++;
		}

		#endregion Feature

		#region Valheim Interface

		public override bool Interact(Humanoid user, bool hold, bool alt)
		{
			if(hold) return false;
			switch(index)
			{
				case 0:
					CreatSideQuest();
					break;
				case 1:
					GiveUpQuest();
					break;
				case 2:
					ChangeLevel();
					break;
				case 3:
					if(QuestManager.instance.HasQuest())
					{
						QuestManager.instance.PrintQuestList();
						Say("$op_munin_wait_hug");
						break;
					}
					Say("$op_munin_noquest");
					break;
			}
			return true;
		}

		public override void SecondaryInteract(Humanoid user)
		{
			index += 1;
			if(index + 1 > choice.Length)
				index = 0;
			currentChoice = choice[index];
		}

		public override string GetHoverText()
		{
			var n = $"<color=lightblue><b>{m_name}</b></color>";
			n += $"\n<color=lightblue><b>$op_munin_quest_lvl :{QuestManager.instance.Level}</b></color>";
			n += $"\n$op_munin_questnum_b <color=lightblue><b>{QuestManager.instance.Count()}</b></color> $op_munin_questnum_a";
			n += "\n[<color=yellow><b>1-8</b></color>]$op_offer";
			n += $"\n[<color=yellow><b>$KEY_Use</b></color>]{currentChoice}";
			// Show full keybind (Alt+E, not just E)
			var modifiers = Plugin.SecondInteractKey.Modifiers;
			var keyText = modifiers.Any()
				? string.Join("+", modifiers) + "+" + Plugin.SecondInteractKey.MainKey
				: Plugin.SecondInteractKey.MainKey.ToString();
			n += $"\n<color=yellow><b>[{keyText}]</b></color>$op_switch";
			return Localization.instance.Localize(n);
		}

		public override string GetHoverName() => Localization.instance.Localize(m_name);

		public override bool UseItem(Humanoid user, ItemDrop.ItemData item)
		{
			if(!SearchQuestProcesser.CanOffer(item.m_dropPrefab.name))
				return false;
			if(SearchQuestProcesser.CanFinish(item.m_dropPrefab.name))
			{
				Say("$op_munin_takeoffer");
				return true;
			}
			Say("$op_munin_notenough");
			return true;
		}

		#endregion Valheim Interface

		#region Tool

		public static void Reward(int key, int level)
		{
			var a = Instantiate(ZNetScene.instance.GetPrefab("OdinLegacy"), instance.transform.position + Vector3.up * 2f + Vector3.forward, Quaternion.identity);
			var id = a.GetComponent<ItemDrop>().m_itemData;
			id.m_stack = key;
			id.m_quality = level;
			ResetTimer();
		}

		public static void ResetTimer()
		{
			instance.timer = 0f;
		}

		#endregion Tool

		#region TextGUI

		class TR_Giveup : TextReceiver
		{
			public string GetText() => "";

			public void SetText(string text)
			{
				if(int.TryParse(text, out var num))
				{
					if(!QuestManager.instance.GiveUpQuest(num))
					{
						DBG.InfoCT($"$op_munin_noq {num}");
						return;
					}
					return;
				}
				DBG.InfoCT("$op_wrong_num");
			}
		}

		#endregion TextGUI
	}
}
