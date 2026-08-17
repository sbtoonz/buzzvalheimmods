using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace OdinPlus
{
	public static class JewelcraftingCompat
	{
		private static bool? _cached;

		public static bool IsActive
		{
			get
			{
				if (_cached == null)
				{
					try { _cached = Jewelcrafting.API.IsLoaded(); }
					catch { _cached = false; }
				}
				return _cached.Value;
			}
		}

		private static readonly string[] ShardPrefabs =
		{
			"Shattered_Black_Crystal",
			"Shattered_Blue_Crystal",
			"Shattered_Green_Crystal",
			"Shattered_Red_Crystal",
			"Shattered_Yellow_Crystal",
			"Shattered_Purple_Crystal",
			"Shattered_Orange_Crystal"
		};

		private static readonly string[] SimpleGemPrefabs =
		{
			"Simple_Black_Socket",
			"Simple_Blue_Socket",
			"Simple_Green_Socket",
			"Simple_Red_Socket",
			"Simple_Yellow_Socket",
			"Simple_Purple_Socket",
			"Simple_Orange_Socket"
		};

		private static readonly string[] AdvancedGemPrefabs =
		{
			"Advanced_Black_Socket",
			"Advanced_Blue_Socket",
			"Advanced_Green_Socket",
			"Advanced_Red_Socket",
			"Advanced_Yellow_Socket",
			"Advanced_Purple_Socket",
			"Advanced_Orange_Socket"
		};

		private static readonly string[] PerfectGemPrefabs =
		{
			"Perfect_Black_Socket",
			"Perfect_Blue_Socket",
			"Perfect_Green_Socket",
			"Perfect_Red_Socket",
			"Perfect_Yellow_Socket",
			"Perfect_Purple_Socket",
			"Perfect_Orange_Socket"
		};

		private static readonly string[] UncutPrefabs =
		{
			"Uncut_Black_Stone",
			"Uncut_Blue_Stone",
			"Uncut_Green_Stone",
			"Uncut_Red_Stone",
			"Uncut_Yellow_Stone",
			"Uncut_Purple_Stone",
			"Uncut_Orange_Stone"
		};

		public static List<ItemReward> GetGemRewardsForTier(int reputationRequired)
		{
			if (!IsActive) return new List<ItemReward>();

			var rewards = new List<ItemReward>();
			var rng = new System.Random();

			if (reputationRequired <= 0)
			{
				string shard = ShardPrefabs[rng.Next(ShardPrefabs.Length)];
				rewards.Add(new ItemReward { ItemName = shard, Amount = 2, Quality = 1 });
			}
			else if (reputationRequired <= 10)
			{
				string uncut = UncutPrefabs[rng.Next(UncutPrefabs.Length)];
				rewards.Add(new ItemReward { ItemName = uncut, Amount = 1, Quality = 1 });
			}
			else if (reputationRequired <= 30)
			{
				string gem = SimpleGemPrefabs[rng.Next(SimpleGemPrefabs.Length)];
				rewards.Add(new ItemReward { ItemName = gem, Amount = 1, Quality = 1 });
			}
			else
			{
				string gem = AdvancedGemPrefabs[rng.Next(AdvancedGemPrefabs.Length)];
				rewards.Add(new ItemReward { ItemName = gem, Amount = 1, Quality = 1 });
			}

			return rewards;
		}

		public struct StoreEntry
		{
			public string PrefabName;
			public string DisplayName;
			public int CreditCost;
		}

		public static List<StoreEntry> GetStoreItems()
		{
			if (!IsActive) return new List<StoreEntry>();

			var items = new List<StoreEntry>();

			foreach (var shard in ShardPrefabs)
			{
				string color = shard.Replace("Shattered_", "").Replace("_Crystal", "");
				items.Add(new StoreEntry { PrefabName = shard, DisplayName = $"{color} Shard", CreditCost = 10 });
			}

			foreach (var uncut in UncutPrefabs)
			{
				string color = uncut.Replace("Uncut_", "").Replace("_Stone", "");
				items.Add(new StoreEntry { PrefabName = uncut, DisplayName = $"{color} Gemstone", CreditCost = 25 });
			}

			foreach (var gem in SimpleGemPrefabs)
			{
				string color = gem.Replace("Simple_", "").Replace("_Socket", "");
				items.Add(new StoreEntry { PrefabName = gem, DisplayName = $"Simple {color} Gem", CreditCost = 50 });
			}

			foreach (var gem in AdvancedGemPrefabs)
			{
				string color = gem.Replace("Advanced_", "").Replace("_Socket", "");
				items.Add(new StoreEntry { PrefabName = gem, DisplayName = $"Advanced {color} Gem", CreditCost = 150 });
			}

			foreach (var gem in PerfectGemPrefabs)
			{
				string color = gem.Replace("Perfect_", "").Replace("_Socket", "");
				items.Add(new StoreEntry { PrefabName = gem, DisplayName = $"Perfect {color} Gem", CreditCost = 400 });
			}

			return items;
		}

		public static bool TryGiveGemReward(Humanoid player, int reputationTier)
		{
			if (!IsActive) return false;
			if (ObjectDB.instance == null) return false;

			var rewards = GetGemRewardsForTier(reputationTier);
			bool gave = false;

			foreach (var reward in rewards)
			{
				var prefab = ObjectDB.instance.GetItemPrefab(reward.ItemName);
				if (prefab == null)
				{
					DBG.blogWarning($"[JewelcraftingCompat] Gem prefab '{reward.ItemName}' not found in ObjectDB");
					continue;
				}

				var itemDrop = prefab.GetComponent<ItemDrop>();
				if (itemDrop == null) continue;

				var itemData = itemDrop.m_itemData.Clone();
				itemData.m_stack = reward.Amount;
				itemData.m_quality = reward.Quality;

				if (player.GetInventory().AddItem(itemData))
				{
					gave = true;
					DBG.blogInfo($"[JewelcraftingCompat] Gave {reward.Amount}x {reward.ItemName} to player");
				}
			}

			return gave;
		}

		public static bool TryBuyFromStore(Humanoid player, string prefabName, int cost)
		{
			if (!IsActive) return false;
			if (ObjectDB.instance == null) return false;

			if (OdinData.Credits < cost)
			{
				DBG.InfoCT("$op_god_nocrd");
				return false;
			}

			var prefab = ObjectDB.instance.GetItemPrefab(prefabName);
			if (prefab == null)
			{
				DBG.blogWarning($"[JewelcraftingCompat] Store item '{prefabName}' not found");
				return false;
			}

			var itemDrop = prefab.GetComponent<ItemDrop>();
			if (itemDrop == null) return false;

			var itemData = itemDrop.m_itemData.Clone();
			itemData.m_stack = 1;

			if (player.GetInventory().AddItem(itemData))
			{
				OdinData.RemoveCredits(cost);
				DBG.blogInfo($"[JewelcraftingCompat] Player bought {prefabName} for {cost} credits");
				return true;
			}

			DBG.InfoCT("$op_inventory_full");
			return false;
		}
	}
}
