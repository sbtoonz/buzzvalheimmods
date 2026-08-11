using System;
using UnityEngine;
using System.Collections.Generic;

namespace OdinPlus
{
	public class HumanMessager : QuestVillager, Hoverable, Interactable, OdinInteractable
	{
		private static Dictionary<string, Vector3> s_questPinPositions = new Dictionary<string, Vector3>();

		protected override void Awake()
		{
			base.Awake();
			m_name = "Messenger";
			ChoiceList = new string[2] { "$op_talk", "Take Message Quest" };
		}
		public override void Choice0()
		{
			Say("I need some help, can you take a message for me?");
		}
		public void Choice1()
		{
			if (!IsQuestReady())
			{
				Say("I don't have any messages right now, come back later");
				return;
			}
			var key = HumanVis.NPCnames.GetRandomElement();
			OdinData.AddKey(key);
			bool placed = PlaceRandom(key);
			if (!placed)
			{
				Say("I can't find anyone to deliver to right now...");
				OdinData.RemoveKey(key);
				return;
			}
			string localName = Localization.instance.Localize(key);
			Say($"Thanks! Deliver my message to <color=yellow><b>{localName}</b></color>. I've marked them on your map.");
			ResetQuestCD();
		}

		private void PlaceQuestHuman(string key, Vector3 pos)
		{
			var pgo = ZNetScene.instance.GetPrefab("WorkerNPCHuman");
			if (pgo == null)
			{
				DBG.blogWarning("[HumanMessager] WorkerNPCHuman prefab not found");
				return;
			}
			float y;
			ZoneSystem.instance.FindFloor(pos, out y);
			pos = new Vector3(pos.x, y + 2, pos.z);

			var go = Instantiate(pgo, pos, Quaternion.identity);
			var vis = go.GetComponent<HumanVis>();
			if (vis != null) vis.m_name = key;

			var znv = go.GetComponent<ZNetView>();
			if (znv != null) znv.GetZDO().Set("npcname", key);

			// Add map pin so the player can find the delivery target
			string localName = Localization.instance.Localize(key);
			Minimap.instance.DiscoverLocation(pos, Minimap.PinType.Icon3, $"Deliver to: {localName}", false);
			s_questPinPositions[key] = pos;
		}

		public static void RemoveQuestPin(string key)
		{
			if (s_questPinPositions.TryGetValue(key, out var pos))
			{
				Minimap.instance.RemovePin(pos, 5f);
				s_questPinPositions.Remove(key);
			}
		}

		private bool PlaceRandom(string key)
		{
			foreach (var item in LocationMarker.MarkList.Values)
			{
				var dis = Utils.DistanceXZ(item.GetPosition(), transform.position);
				if (dis > 100)
				{
					PlaceQuestHuman(key, item.GetPosition());
					return true;
				}
			}
			return false;
		}
	}
}
