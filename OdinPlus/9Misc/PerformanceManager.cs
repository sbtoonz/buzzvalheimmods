using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace OdinPlus
{
	public class PerformanceManager : MonoBehaviour
	{
		static PerformanceManager _instance;

		struct ScheduledAction
		{
			public Action action;
			public float interval;
			public float nextTime;
		}

		List<ScheduledAction> _scheduled = new();
		struct NPCData
		{
			public HumanNPC npc;
			public MonsterAI ai;
		}
		List<NPCData> _trackedNPCs = new();
		const float NPC_CULL_DISTANCE = 100f;

		public static PerformanceManager Instance
		{
			get
			{
				if(_instance == null)
				{
					var go = new GameObject("OdinPlusPerformanceManager");
					_instance = go.AddComponent<PerformanceManager>();
					DontDestroyOnLoad(go);
				}
				return _instance;
			}
		}

		void Awake()
		{
			if(_instance != null && _instance != this)
			{
				Destroy(gameObject);
				return;
			}
			_instance = this;
			ScheduleUpdate(CullDistantNPCs, 2f);
		}

		void Update()
		{
			var time = Time.time;
			for(int i = 0; i < _scheduled.Count; i++)
			{
				var s = _scheduled[i];
				if(time >= s.nextTime)
				{
					s.action.Invoke();
					s.nextTime = time + s.interval;
					_scheduled[i] = s;
				}
			}
		}

		public void ScheduleUpdate(Action update, float intervalSeconds)
		{
			if(update == null) return;
			for(int i = 0; i < _scheduled.Count; i++)
			{
				if(_scheduled[i].action == update)
				{
					var s = _scheduled[i];
					s.interval = intervalSeconds;
					_scheduled[i] = s;
					return;
				}
			}
			_scheduled.Add(new() { action = update, interval = intervalSeconds, nextTime = Time.time + intervalSeconds });
		}

		public void UnscheduleUpdate(Action update)
		{
			if(update == null) return;
			for(int i = _scheduled.Count - 1; i >= 0; i--)
			{
				if(_scheduled[i].action == update)
				{
					_scheduled.RemoveAt(i);
					return;
				}
			}
		}

		public void RegisterNPC(HumanNPC npc)
		{
			if(npc == null) return;
			if(_trackedNPCs.Any(d => d.npc == npc)) return;
			var ai = npc.GetComponent<MonsterAI>();
			_trackedNPCs.Add(new() { npc = npc, ai = ai });
		}

		public void UnregisterNPC(HumanNPC npc)
		{
			if(npc == null) return;
			_trackedNPCs.RemoveAll(d => d.npc == npc);
		}

		void CullDistantNPCs()
		{
			if(Player.m_localPlayer == null) return;
			var playerPos = Player.m_localPlayer.transform.position;

			for(int i = _trackedNPCs.Count - 1; i >= 0; i--)
			{
				var data = _trackedNPCs[i];
				if(data.npc == null)
				{
					_trackedNPCs.RemoveAt(i);
					continue;
				}

				var dist = Vector3.Distance(data.npc.transform.position, playerPos);
				var shouldBeActive = dist < NPC_CULL_DISTANCE;

				if(data.ai != null && data.ai.enabled != shouldBeActive)
					data.ai.enabled = shouldBeActive;
			}
		}

		void OnDestroy()
		{
			_scheduled.Clear();
			_trackedNPCs.Clear();
			if(_instance == this) _instance = null;
		}
	}
}
