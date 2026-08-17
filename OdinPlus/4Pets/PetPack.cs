using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace OdinPlus
{
	public class PetPack : MonoBehaviour, OdinInteractable, Hoverable
	{
		private Container container;
		private Tameable tame;
		private Inventory m_inventory;
		private Character m_character;
		private float m_baseSpeed;
		private float m_maxWeight = 500;

		private static readonly MethodInfo s_resetFeedingTimer = AccessTools.Method(typeof(Tameable), "ResetFeedingTimer");
		private static readonly AccessTools.FieldRef<Container, Inventory> s_inventoryRef =
			AccessTools.FieldRefAccess<Container, Inventory>("m_inventory");

		private void Awake()
		{
			container = GetComponent<Container>();
			tame = GetComponent<Tameable>();
			m_character = GetComponent<Character>();

			m_character.SetTamed(true);
			tame.m_fedDuration = 1800;
			s_resetFeedingTimer?.Invoke(tame, null);

			m_character.m_onDeath = (Action)Delegate.Combine(new Action(OnDeath), m_character.m_onDeath);
			m_baseSpeed = m_character.m_speed;

			string petName = gameObject.name.Replace("(Clone)", "").Trim();
			if (petName.Contains("Wolf")) PetManager.WolfIns = gameObject;
			else if (petName.Contains("Dverger")) PetManager.DvergerIns = gameObject;

			InvokeRepeating(nameof(CheckHunger), 2f, 2f);
			InvokeRepeating(nameof(UpdateSpeed), 1f, 1f);
		}

		private void Start()
		{
			m_inventory = s_inventoryRef(container);
		}

		private void CheckHunger()
		{
			if (tame != null && tame.IsHungry())
			{
				ZNetScene.instance.Destroy(gameObject);
			}
		}

		private void UpdateSpeed()
		{
			if (m_inventory == null || m_character == null) return;
			float weight = m_inventory.GetTotalWeight();
			if (weight <= 0)
			{
				m_character.m_speed = m_baseSpeed * 2f;
				return;
			}
			if (weight >= m_maxWeight)
			{
				m_character.m_speed = m_baseSpeed * 0.5f;
				return;
			}
			m_character.m_speed = m_baseSpeed * ((m_maxWeight - weight) / m_maxWeight * 1.5f + 0.5f);
		}

		public void SecondaryInteract(Humanoid user)
		{
			container.Interact(user, false, false);
		}

		public string GetHoverText()
		{
			var hum = GetComponent<Humanoid>();
			string name = hum != null ? hum.m_name : gameObject.name;
			string text = $"<color=#00FFFF>{name}</color>";
			text += $"\n[<color=yellow><b>E</b></color>] Pet (Follow)";
			text += $"\n[<color=yellow><b>Alt+E</b></color>] Open Inventory";
			if (m_inventory != null)
			{
				float weight = m_inventory.GetTotalWeight();
				text += $"\nCarrying: {weight:0.0}kg / {m_maxWeight}kg";
			}
			return Localization.instance.Localize(text);
		}

		public string GetHoverName()
		{
			var hum = GetComponent<Humanoid>();
			return hum != null ? Localization.instance.Localize(hum.m_name) : gameObject.name;
		}

		public void Teleport()
		{
			transform.position = Player.m_localPlayer.transform.position + Player.m_localPlayer.transform.forward * 2f + Vector3.up;
		}

		private void OnDeath()
		{
			if (m_inventory == null || m_inventory.SlotsUsedPercentage() == 0) return;
			List<ItemDrop.ItemData> allItems = m_inventory.GetAllItems();
			foreach (ItemDrop.ItemData item in allItems)
			{
				Vector3 position = transform.position + Vector3.up * 0.5f + UnityEngine.Random.insideUnitSphere * 0.3f;
				Quaternion rotation = Quaternion.Euler(0f, UnityEngine.Random.Range(0, 360), 0f);
				ItemDrop.DropItem(item, 0, position, rotation);
			}
		}

		private void OnDestroy()
		{
			if (PetManager.WolfIns == gameObject) PetManager.WolfIns = null;
			if (PetManager.DvergerIns == gameObject) PetManager.DvergerIns = null;
		}
	}
}
