using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace OdinPlus
{
	public class PerformanceManager : MonoBehaviour
	{
		private static PerformanceManager _instance;

		private struct ScheduledAction
		{
			public Action action;
			public float interval;
			public float nextTime;
		}

		private List<ScheduledAction> _scheduled = new List<ScheduledAction>();
		private struct NPCData
	{
		public HumanNPC npc;
		public MonsterAI ai;
	}
	private List<NPCData> _trackedNPCs = new List<NPCData>();
		private const float NPC_CULL_DISTANCE = 100f;

		public static PerformanceManager Instance
		{
			get
			{
				if (_instance == null)
				{
					var go = new GameObject("OdinPlusPerformanceManager");
					_instance = go.AddComponent<PerformanceManager>();
					DontDestroyOnLoad(go);
				}
				return _instance;
			}
		}

		private void Awake()
		{
			if (_instance != null && _instance != this)
			{
				Destroy(gameObject);
				return;
			}
			_instance = this;
			ScheduleUpdate(CullDistantNPCs, 2f);
		}

		private void Update()
		{
			float time = Time.time;
			for (int i = 0; i < _scheduled.Count; i++)
			{
				var s = _scheduled[i];
				if (time >= s.nextTime)
				{
					s.action.Invoke();
					s.nextTime = time + s.interval;
					_scheduled[i] = s;
				}
			}
		}

		public void ScheduleUpdate(Action update, float intervalSeconds)
		{
			if (update == null) return;
			for (int i = 0; i < _scheduled.Count; i++)
			{
				if (_scheduled[i].action == update)
				{
					var s = _scheduled[i];
					s.interval = intervalSeconds;
					_scheduled[i] = s;
					return;
				}
			}
			_scheduled.Add(new ScheduledAction { action = update, interval = intervalSeconds, nextTime = Time.time + intervalSeconds });
		}

		public void UnscheduleUpdate(Action update)
		{
			if (update == null) return;
			for (int i = _scheduled.Count - 1; i >= 0; i--)
			{
				if (_scheduled[i].action == update)
				{
					_scheduled.RemoveAt(i);
					return;
				}
			}
		}

		public void RegisterNPC(HumanNPC npc)
		{
			if (npc == null) return;
			if (_trackedNPCs.Any(d => d.npc == npc)) return;
			var ai = npc.GetComponent<MonsterAI>();
			_trackedNPCs.Add(new NPCData { npc = npc, ai = ai });
		}

		public void UnregisterNPC(HumanNPC npc)
		{
			if (npc == null) return;
			_trackedNPCs.RemoveAll(d => d.npc == npc);
		}

		private void CullDistantNPCs()
		{
			if (Player.m_localPlayer == null) return;
			Vector3 playerPos = Player.m_localPlayer.transform.position;

			for (int i = _trackedNPCs.Count - 1; i >= 0; i--)
			{
				var data = _trackedNPCs[i];
				if (data.npc == null)
				{
					_trackedNPCs.RemoveAt(i);
					continue;
				}

				float dist = Vector3.Distance(data.npc.transform.position, playerPos);
				bool shouldBeActive = dist < NPC_CULL_DISTANCE;

				if (data.ai != null && data.ai.enabled != shouldBeActive)
					data.ai.enabled = shouldBeActive;
			}
		}

		private void OnDestroy()
		{
			_scheduled.Clear();
			_trackedNPCs.Clear();
			if (_instance == this) _instance = null;
		}
	}
}
