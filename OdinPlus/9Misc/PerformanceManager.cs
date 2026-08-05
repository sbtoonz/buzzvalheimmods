using System;
using System.Collections.Generic;
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
		private List<HumanNPC> _trackedNPCs = new List<HumanNPC>();
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
			if (!_trackedNPCs.Contains(npc))
				_trackedNPCs.Add(npc);
		}

		public void UnregisterNPC(HumanNPC npc)
		{
			_trackedNPCs.Remove(npc);
		}

		private void CullDistantNPCs()
		{
			if (Player.m_localPlayer == null) return;
			Vector3 playerPos = Player.m_localPlayer.transform.position;

			for (int i = _trackedNPCs.Count - 1; i >= 0; i--)
			{
				var npc = _trackedNPCs[i];
				if (npc == null)
				{
					_trackedNPCs.RemoveAt(i);
					continue;
				}

				float dist = Vector3.Distance(npc.transform.position, playerPos);
				bool shouldBeActive = dist < NPC_CULL_DISTANCE;

				var ai = npc.GetComponent<MonsterAI>();
				if (ai != null && ai.enabled != shouldBeActive)
					ai.enabled = shouldBeActive;
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
