using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using HarmonyLib;

namespace OdinPlus
{
	internal class LocationManager : MonoBehaviour
	{
		private static Dictionary<Vector2i, ZoneSystem.LocationInstance> m_locationInstances = new Dictionary<Vector2i, ZoneSystem.LocationInstance>();
		private static readonly AccessTools.FieldRef<ZoneSystem, Dictionary<Vector2i, ZoneSystem.LocationInstance>> s_locationInstancesRef =
			AccessTools.FieldRefAccess<ZoneSystem, Dictionary<Vector2i, ZoneSystem.LocationInstance>>("m_locationInstances");
		public static List<string> BlackList = new List<string>();
		public static LocationManager instance;
		public static bool rpc = false;
		public static Vector3 OdinPostion = Vector3.zero;

		#region Main
		private void Awake()
		{
			instance = this;
			Plugin.RegRPC = (Action)Delegate.Combine(Plugin.RegRPC, (Action)initRPC);
		}
		public static void Init()
		{
			if (ZNet.instance.IsServer())
			{
				if (Plugin.OdinPosition == Vector3.zero)
				{
					ZoneSystem.LocationInstance temp;
					ZoneSystem.instance.FindClosestLocation("StartTemple", Vector3.zero, out temp);
					OdinPostion = temp.m_position + new Vector3(-6, 0, -8);
					if (OdinPostion == Vector3.zero)
					{
						OdinPostion += Vector3.forward * 0.0001f;
					}
				}
				else
				{
					OdinPostion = Plugin.OdinPosition;
				}
				BlackList = OdinData.Data.BlackList;
				GetValDictionary();
			}

		}
		public static void GetValDictionary()
		{
			var a = s_locationInstancesRef(ZoneSystem.instance);
			if (a == null) return;
			foreach (var item in a)
			{
				m_locationInstances.Add(item.Key, item.Value);
			}
		}

		public static void RemoveBlackList()
		{
			foreach (var item in BlackList)
			{
				m_locationInstances.Remove(item.ToV2I());
			}
		}
		public static void Clear()
		{
			BlackList.Clear();
			m_locationInstances.Clear();
		}
		#endregion Init

		public static int RevealAllLocations(string locationName)
		{
			int count = 0;
			foreach (var item in m_locationInstances)
			{
				if (item.Value.m_location.m_prefabName == locationName)
				{
					Minimap.instance.DiscoverLocation(item.Value.m_position, Minimap.PinType.Icon3, locationName, false);
					count++;
				}
			}
			return count;
		}

		#region Feature
		public static bool GetLocationInstance(string id, out ZoneSystem.LocationInstance li)
		{
			var a = s_locationInstancesRef(ZoneSystem.instance);
			if (a == null)
			{
				li = default(ZoneSystem.LocationInstance);
				return false;
			}
			var key = Tweakers.Pak(id);
			if (a.TryGetValue(key, out li))
			{
				return true;
			}
			li = default(ZoneSystem.LocationInstance);
			return false;
		}
		public static bool FindClosestLocation(string name, Vector3 point, out string id)
		{

			float num = 999999f;
			id = "0_0";
			bool result = false;
			foreach (var item in m_locationInstances)
			{
				float num2 = Vector3.Distance(item.Value.m_position, point);
				if (item.Value.m_location.m_prefabName == name && num2 < num)
				{
					num = num2;
					id = item.Key.Pak();
					result = true;
				}
			}
			return result;
		}
		public static bool FindClosestLocation(string name, Vector3 point, out Vector3 pos)
		{

			float num = 999999f;

			bool result = false;
			foreach (var item in m_locationInstances)
			{
				float num2 = Vector3.Distance(item.Value.m_position, point);
				if (item.Value.m_location.m_prefabName == name && num2 < num)
				{
					pos = item.Value.m_position;
					num = num2;
					result = true;
				}
			}
			pos = Vector3.zero;
			return result;
		}
		public static bool FindClosestLocation(string name, Vector3 point, out string id, out Vector3 pos)
		{

			float num = 999999f;
			pos = Vector3.zero;
			id = "0_0";
			bool result = false;
			foreach (var item in m_locationInstances)
			{
				float num2 = Vector3.Distance(item.Value.m_position, point);
				if (item.Value.m_location.m_prefabName == name && num2 < num)
				{
					pos = item.Value.m_position;
					id = item.Key.Pak();
					num = num2;
					result = true;
				}
			}
			return result;
		}
		#endregion Feature

		#region Tool
		public static GameObject FindDungeon(Vector3 pos)
		{
			var loc = Location.GetLocation(pos);
			if (loc == null)
			{
				return null;
			}
			var dunPos = loc.transform.Find("Interior").transform.position;
			Collider[] array = Physics.OverlapBox(dunPos, new Vector3(60, 60, 60));
			DungeonGenerator comp;
			foreach (var item in array)
			{
				var c = item.transform;
				while (c.transform.parent != null)
				{
					if (c.TryGetComponent<DungeonGenerator>(out comp))
					{
						if (c.name.Contains("Clone"))
						{
							return c.gameObject;
						}
					}
					c = c.transform.parent;
				}

			}
			return null;
		}
		#endregion Tool

		#region RPC
		public void initRPC()//RPC
		{
			ZRoutedRpc.instance.Register<Vector3>("RPC_SetStartPos", new Action<long, Vector3>(this.RPC_SetStartPos));
			ZRoutedRpc.instance.Register<bool>("RPC_ReceiveServerFOP", new Action<long, bool>(RPC_ReceiveServerFOP));
			if (ZNet.instance.IsServer())
			{
				ZRoutedRpc.instance.Register("Rpc_GetStartPos", new Action<long>(this.Rpc_GetStartPos));
				ZRoutedRpc.instance.Register("RPC_SendServerFOP", new Action<long>(RPC_SendServerFOP));
				ZRoutedRpc.instance.Register<string,Vector3>("RPC_ServerFindLocation", new Action<long,string,Vector3>(RPC_ServerFindLocation));
			}

		}
		public static void GetStartPos()
		{
			ZRoutedRpc.instance.InvokeRoutedRPC("Rpc_GetStartPos", new object[] { });
		}
		private void Rpc_GetStartPos(long sender)
		{
			if (!ZNet.instance.IsServer()) return;
			DBG.blogWarning("Server got odin postion request");
			if (Plugin.OdinPosition != Vector3.zero)
			{
				OdinPostion = Plugin.OdinPosition;
			}
			ZRoutedRpc.instance.InvokeRoutedRPC(sender, "RPC_SetStartPos", new object[] { OdinPostion });
		}
		private void RPC_SetStartPos(long sender, Vector3 pos)
		{
			DBG.blogWarning("client  got odin postion " + pos);
			NpcManager.Root.transform.localPosition = pos;
		}
		public static void RequestServerFop()
		{
			ZRoutedRpc.instance.InvokeRoutedRPC("RPC_SendServerFOP", new object[] { });
		}
		public static void RPC_SendServerFOP(long sender)
		{
			if (!ZNet.instance.IsServer()) return;
			ZRoutedRpc.instance.InvokeRoutedRPC(sender, "RPC_ReceiveServerFOP", new object[] { Plugin.ForceOdinPosition });
			DBG.blogWarning("Server Sent FOP:" + Plugin.ForceOdinPosition);
		}
		public static void RPC_ReceiveServerFOP(long sender, bool result)
		{
			if (ZNet.instance.IsServer()) return;
			DBG.blogWarning("Client Got FOP:" + result);
			Plugin.Set_FOP = result;
		}

		#endregion RPC

		#region New

		public static void Remove(string id)
		{
			BlackList.Add(id);
			m_locationInstances.Remove(id.ToV2I());
			//upd add a new list for remove elements;
		}
		//OPT definitely need a thread pool !! struct[] quenee //notice
		public static void RPC_ServerFindLocation(long sender, string sender_locName, Vector3 sender_pos)
		{
			if (!ZNet.instance.IsServer()) return;
			var _id = "0_0";
			var _pos = Vector3.zero;
			if (FindClosestLocation(sender_locName, sender_pos, out _id, out _pos))
			{
				DBG.blogWarning(string.Format("Server Location found location {0} at {1}", sender_locName, _pos.ToString()));
				ZRoutedRpc.instance.InvokeRoutedRPC(sender, "RPC_CreateQuestSucced", new object[] { _id, _pos});
				Remove(_id);
				return;
			}
			ZRoutedRpc.instance.InvokeRoutedRPC(sender, "RPC_CreateQuestFailed", new object[]{});
			DBG.blogWarning(String.Format("Location cant find location {0} at {1}",sender_locName,sender_pos));
			
		}
		#endregion New

	}
}