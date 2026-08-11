using System;
using UnityEngine;
using System.Collections.Generic;

namespace OdinPlus
{
	public class HumanVillager : HumanNPC, Hoverable, Interactable, OdinInteractable
	{
		public static List<HumanVillager> Villagers = new List<HumanVillager>();
		protected readonly float QuestCD = 1800;
		public float timer = 0;
		public GameObject EXCobj;
		protected override void Awake()
		{
			if (Villagers == null)
			{
				Villagers = new List<HumanVillager>();
			}
			Villagers.Add(this);
			base.Awake();
			var zdo = m_nview.GetZDO();
			m_hum.m_onDamaged = (Action<float, Character>)Delegate.Combine(m_hum.m_onDamaged, (Action<float, Character>)(Damage));

		}

		private void OnDestroy()
		{
			Villagers.Remove(this);
			if (m_hum != null)
				m_hum.m_onDamaged = (Action<float, Character>)Delegate.Remove(m_hum.m_onDamaged, new Action<float, Character>(Damage));
		}
		private void Damage(float hit, Character character)
		{
			if (character == null)
			{
				return;
			}
			if (character.IsPlayer())
			{
				foreach (var item in Villagers)
				{
					item.ChangeFaction(Player.m_localPlayer);
				}
			}
		}
		public bool IsQuestReady()
		{
			if (m_nview == null || m_nview.GetZDO() == null)
			{
				DBG.blogWarning("[QuestVillager] IsQuestReady: ZNetView or ZDO is null");
				return false;
			}
			long questTimeTicks = m_nview.GetZDO().GetLong("QuestTime", 0);
			DBG.blogInfo($"[QuestVillager] IsQuestReady check: questTimeTicks={questTimeTicks}, QuestCD={QuestCD}");

			if (questTimeTicks == 0)
			{
				DBG.blogInfo("[QuestVillager] First time quest - ready");
				return true;
			}

			DateTime lastQuestTime = new DateTime(questTimeTicks);
			DateTime currentTime = ZNet.instance.GetTime();
			double secondsSince = (currentTime - lastQuestTime).TotalSeconds;
			bool result = secondsSince > (double)QuestCD;

			DBG.blogInfo($"[QuestVillager] Last quest: {lastQuestTime}, Now: {currentTime}, Seconds since: {secondsSince:F0}, Required: {QuestCD}, Ready: {result}");

			if (EXCobj != null) EXCobj.SetActive(result);
			return result;
		}
		public void ResetQuestCD()
		{
			m_nview.GetZDO().Set("QuestTime",ZNet.instance.GetTime().Ticks);
		}
	}
}
