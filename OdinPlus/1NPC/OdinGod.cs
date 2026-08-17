using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.Reflection;
using HarmonyLib;

//||X||Sell Value Don't Resolve Here!!!
namespace OdinPlus
{
	public class OdinGod : OdinNPC, Hoverable, Interactable, OdinInteractable
	{
		#region Fields

		internal static OdinGod m_instance;
		List<string> slist = new();
		List<Skills.SkillType> stlist = new();
		string cskill;
		int cskillIndex = 0;

		#endregion Fields

		#region Utilities

		Vector3 FindSpawnPoint()
		{
			var a = UnityEngine.Random.Range(10, 10);
			var b = UnityEngine.Random.Range(10, 10);
			var c = ZoneSystem.instance.GetGroundHeight(new Vector3(a, 500, b));
			if(ZoneSystem.instance.FindClosestLocation("StartTemple", Vector3.zero, out var locationInstance))
			{
				var p = locationInstance.m_position + new Vector3(-6, 0.2f, -8);
				return p;
			}
			DBG.blogWarning("Cant Find a point to Spawn Odin use /odin respawn");
			return new Vector3(a, c, b);
		}

		string randomName()
		{
			UnityEngine.Random.InitState(Mathf.FloorToInt(Time.realtimeSinceStartup));
			var l = OdinData.ItemSellValue;
			var i = UnityEngine.Random.Range(0, l.Count - 1);
			return l.ElementAt(i).Key.GetTransName();
		}

		public static bool IsInstantiated() => m_instance == null;

		public void RestTerrian()
		{
			//Terrain.ResetTerrain(this.transform.position, 10);
		}

		#endregion Utilities

		#region Mono

		void Awake()
		{
			m_instance = this;
			Summon();
			m_head = gameObject.transform.Find("visual/Armature/Hips/Spine0/Spine1/Spine2/Head");
			m_name = "$op_god";
			m_talker = gameObject;
			InvokeRepeating("requestOidnPosition", 1, 3);
			DBG.blogInfo("Client start to Calling Request Odin Location");
		}

		void requestOidnPosition()
		{
			if(NpcManager.Root.transform.position == Vector3.zero)
			{
				LocationManager.GetStartPos();
				DBG.blogWarning($"[OdinGod] Still waiting for position (polling every 3s)");
				return;
			}
			DBG.blogInfo($"[OdinGod] Position received: {NpcManager.Root.transform.position}, canceling repeating invoke");
			CancelInvoke("requestOidnPosition");
		}

		void Start()
		{
			Debug.LogWarning(gameObject.transform.parent.rotation);
			gameObject.transform.parent.Rotate(0, 42, 0);
			Debug.LogWarning(gameObject.transform.parent.rotation);
		}

		void OnDestroy()
		{
			if(m_instance == this)
				m_instance = null;
		}

		#endregion Mono

		#region Tool

		public bool Summon()
		{
			ReadSkill();
			return true;
		}

		#endregion Tool

		#region Valheim Interface

		public override bool Interact(Humanoid user, bool hold, bool alt)
		{
			if(hold) return false;
			if(!OdinData.RemoveCredits(Plugin.RaiseCost))
			{
				Say("$op_god_nocrd");
				return false;
			}

			user.GetSkills().RaiseSkill(stlist[cskillIndex], Plugin.RaiseFactor);
			Say("$op_raise");
			return true;
		}

		public override void SecondaryInteract(Humanoid user)
		{
			SwitchSkill();
		}

		public override string GetHoverText()
		{
			var n = "<color=lightblue><b>ODIN</b></color>";
			var s = $"\n<color=lightblue><b>$op_crd:{OdinData.Credits}</b></color>";
			var a = $"\n[<color=yellow><b>$KEY_Use</b></color>] $op_use[<color=green><b>{cskill}</b></color>]";
			var b = "\n[<color=yellow><b>1-8</b></color>]$op_offer";
			b += $"\n<color=yellow><b>[{Plugin.SecondInteractKey.MainKey}]</b></color>$op_switch";
			return Localization.instance.Localize(n + s + a + b);
		}

		public override bool UseItem(Humanoid user, ItemDrop.ItemData item)
		{
			var name = item.m_dropPrefab.name;
			var value = 1;
			if(!OdinData.ItemSellValue.ContainsKey(name))
			{
				Say($"$op_god_randomitem {randomName()}");
				return false;
			}
			value = OdinData.ItemSellValue[name];
			OdinData.AddCredits(value * item.m_stack * item.m_quality, m_head);
			user.GetInventory().RemoveItem(item.m_shared.m_name, item.m_stack);
			Say("$op_god_takeoffer");
			return true;
		}

		#endregion Valheim Interface

		#region Feature

		void ReadSkill()
		{
			slist.Clear();
			stlist.Clear();
			foreach(object obj in Enum.GetValues(typeof(Skills.SkillType)))
			{
				var skillType = (Skills.SkillType)obj;
				var s = skillType.ToString();
				if(s != "None" && s != "FrostMagic" && s != "All" && s != "FireMagic")
				{
					slist.Add(skillType.ToString());
					stlist.Add(skillType);
				}
			}
			cskill = slist[cskillIndex];
		}

		public void SwitchSkill()
		{
			cskillIndex += 1;
			if(cskillIndex + 1 > slist.Count())
				cskillIndex = 0;
			cskill = slist[cskillIndex];
		}

		#endregion Feature
	}
}
