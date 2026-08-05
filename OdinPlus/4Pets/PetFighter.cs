using System;
using HarmonyLib;
using UnityEngine;

namespace OdinPlus
{
	class PetFighter : MonoBehaviour
	{
		private Tameable tame;
		private MonsterAI mai;

		private static readonly AccessTools.FieldRef<MonsterAI, StaticTarget> s_targetStaticRef =
			AccessTools.FieldRefAccess<MonsterAI, StaticTarget>("m_targetStatic");

		void Awake()
		{
			tame = GetComponent<Tameable>();
			mai = GetComponent<MonsterAI>();
			tame.m_commandable = true;
			tame.m_fedDuration = 300;
			GetComponent<Character>().SetTamed(true);

			var resetMethod = AccessTools.Method(typeof(Tameable), "ResetFeedingTimer");
			resetMethod?.Invoke(tame, null);

			Character character = GetComponent<Character>();
			character.m_onDeath = (Action)Delegate.Combine(new Action(OnDestroyed), character.m_onDeath);

			string petName = gameObject.name.Replace("(Clone)", "").Trim();
			if (petName.Contains("Troll")) PetManager.TrollIns = gameObject;
			else if (petName.Contains("Fenring")) PetManager.FenringIns = gameObject;
			else if (petName.Contains("Brute")) PetManager.BruteIns = gameObject;

			InvokeRepeating(nameof(CheckHunger), 2f, 2f);
		}

		private void CheckHunger()
		{
			if (tame != null && tame.IsHungry())
			{
				ZNetScene.instance.Destroy(gameObject);
			}
		}

		void Update()
		{
			if (!Plugin.KS_SecondInteractkey.Value.IsDown()) return;
			ForceAttack();
		}

		void OnDestroyed()
		{
			PetManager.Indicator.SetActive(false);
			ClearInstance();
			var hum = GetComponent<Humanoid>();
			string name = hum != null ? hum.m_name : gameObject.name;
			DBG.InfoCT(Localization.instance.Localize(name + " died"));
		}

		private void OnDestroy()
		{
			PetManager.Indicator.SetActive(false);
			ClearInstance();
		}

		private void ClearInstance()
		{
			if (PetManager.TrollIns == gameObject) PetManager.TrollIns = null;
			if (PetManager.FenringIns == gameObject) PetManager.FenringIns = null;
			if (PetManager.BruteIns == gameObject) PetManager.BruteIns = null;
		}

		public void ForceAttack()
		{
			if (Player.m_localPlayer == null) return;

			Ray ray = new Ray(GameCamera.instance.transform.position, GameCamera.instance.transform.forward);
			int layerMask = Pathfinding.instance.m_layers | Pathfinding.instance.m_waterLayers;
			if (!Physics.Raycast(ray, out RaycastHit hit, 500f, layerMask)) return;

			if (PetManager.Indicator.activeSelf)
			{
				PetManager.Indicator.SetActive(false);
				s_targetStaticRef(mai) = null;
				DBG.InfoCT("Stop pet attack");
				return;
			}

			PetManager.Indicator.SetActive(true);
			PetManager.Indicator.transform.position = hit.point;
			ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.Everybody, "ChatMessage", new object[] { hit.point, 3, "attack here!", "" });
			s_targetStaticRef(mai) = PetManager.Indicator.GetComponent<StaticTarget>();
			DBG.InfoCT("Pet force attack");
		}
	}
}
