using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace OdinPlus
{
	public class OdinData : MonoBehaviour
	{
		#region Var
		#region serialization
		[Serializable]
		public class DataTable
		{
			public int Credits = 100;
			public bool HasWolf = false;
			public bool HasTroll = false;
			public List<string> BlackList = new List<string>();
			public int QuestCount = 0;
			public Dictionary<string, int> SearchTaskList = new Dictionary<string, int>();
			public List<Quest> Quests = new List<Quest>();
			public List<string> BuzzKeys = new List<string>();
		}


		#endregion serialization
		#region interl
		public static int Credits;
		public static Dictionary<string, int> ItemSellValue = new Dictionary<string, int>();
		public static DataTable Data;
		#endregion interl

		#region GameCfgData
		public static Dictionary<string, int> MeadsValue = new Dictionary<string, int>(){
{"ExpMeadS",5},
{"ExpMeadM",10},
{"ExpMeadL",20},
{"WeightMeadS",20},
{"WeightMeadM",30},
{"WeightMeadL",40},
{"InvisibleMeadS",30},
{"InvisibleMeadM",60},
{"InvisibleMeadL",90},
{"PickaxeMeadS",20},
{"PickaxeMeadM",30},
{"PickaxeMeadL",60},
{"BowsMeadS",20},
{"BowsMeadM",30},
{"BowsMeadL",60},
{"SwordsMeadS",20},
{"SwordsMeadM",30},
{"SwordsMeadL",60},
{"SpeedMeadsL",20},
{"AxeMeadS",20},
{"AxeMeadM",30},
{"AxeMeadL",60}
		};
		#endregion GameCfgData

		#endregion Var

		#region Mono
		private void Awake()
		{
			if (DevTool.DisableSaving)
			{
				Credits = 1000;
			}
			Data = new DataTable();
			Data.Quests = new List<Quest>();
			foreach (var kvp in FactionManager.Economy.ItemValues)
			{
				ItemSellValue[kvp.Key] = kvp.Value;
			}
		}
		#endregion Mono

		#region Credits
		public static void AddCredits(int val, Transform m_head)
		{
			AddCredits(val);
			Player.m_localPlayer.m_skillLevelupEffects.Create(m_head.position, m_head.rotation, m_head, 1f);
		}
		public static bool RemoveCredits(int s)
		{
			if (Credits - s < 0)
			{
				return false;
			}
			Credits -= s;
			return true;
		}
		public static void AddCredits(int val)
		{
			Credits += val;
		}
		public static void AddCredits(int val, bool _notice)
		{
			AddCredits(val);
			if (_notice)
			{
				string n = String.Format("Odin Credits added : <color=lightblue><b>[{0}]</b></color>", Credits);
				MessageHud.instance.ShowBiomeFoundMsg(n, true);//trans
			}

		}
		#endregion Credits

		#region Feature
		public static void AddKey(string key)
		{
			Data.BuzzKeys.Add(key);
		}
		public static void RemoveKey(string key)
		{
			Data.BuzzKeys.Remove(key);
		}
		public static bool GetKey(string key)
		{
			if (Data.BuzzKeys.Contains(key))
			{
				return true;
			}
			return false;
		}
		#endregion Feature

		#region Save And Load
		public static void saveOdinData(string name)
		{
			if (DevTool.DisableSaving)
			{
				OdinPlus.m_instance.isLoaded = true;
				return;
			}
			#region Save
			QuestManager.instance.Save();
			Data.Credits = Credits;
			#endregion Save

			#region Serialize
			try
			{
				string file = Path.Combine(Application.persistentDataPath, (name + ".odinplus"));
				var serializer = new SerializerBuilder()
					.WithNamingConvention(PascalCaseNamingConvention.Instance)
					.Build();
				string yaml = serializer.Serialize(Data);
				File.WriteAllText(file, yaml);
			}
			catch (Exception e)
			{
				DBG.blogWarning("Failed to save OdinData: " + e.Message);
			}
			#endregion Serialize

			DBG.blogWarning("OdinDataSaved:" + name);
		}
		public static void loadOdinData(string name)
		{
			DBG.blogWarning("Starting loding data");
			if (DevTool.DisableSaving)
			{
				OdinPlus.m_instance.isLoaded = true;
				return;
			}
			#region Serial
			string file = Path.Combine(Application.persistentDataPath, (name + ".odinplus"));
			if (!File.Exists(@file))
			{
				OdinPlus.m_instance.isLoaded = true;
				Credits = 100;
				DBG.blogWarning("Profile not exists:" + name);
				return;
			}
			
			try
			{
				string yaml = File.ReadAllText(file);
				var deserializer = new DeserializerBuilder()
					.WithNamingConvention(PascalCaseNamingConvention.Instance)
					.IgnoreUnmatchedProperties()
					.Build();
				Data = deserializer.Deserialize<DataTable>(yaml);
			}
			catch (Exception e)
			{
				DBG.blogWarning("Failed to load OdinData (may be old format): " + e.Message);
				Credits = 100;
				OdinPlus.m_instance.isLoaded = true;
				return;
			}
			#endregion Serial

			#region Load
			Credits = Data.Credits;
			QuestManager.instance.Load();
			LocationManager.BlackList = Data.BlackList;
			LocationManager.RemoveBlackList();
			#endregion Load

			OdinPlus.m_instance.isLoaded = true;
			DBG.blogWarning("OdinDataLoaded:" + name);
		}
		#endregion Save And Load

	}
}
