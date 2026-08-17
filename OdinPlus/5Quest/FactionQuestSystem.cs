using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace OdinPlus
{
	[Serializable]
	public class FactionQuestDef
	{
		public string ID { get; set; }
		public string Name { get; set; }
		public string FactionName { get; set; }
		public int RequiredReputation { get; set; } = 10; // Friendly tier default
		public string Description { get; set; }
		public QuestObjective Objective { get; set; }
		public QuestReward Reward { get; set; }
	}

	[Serializable]
	public class QuestObjective
	{
		public string Type { get; set; } // "Kill", "Collect", "Deliver", "Explore"
		public string Target { get; set; } // Monster name, item name, location type
		public int Amount { get; set; } = 1;
		public string Biome { get; set; } = "Any";
	}

	[Serializable]
	public class QuestReward
	{
		public int ReputationGain { get; set; } = 35;
		public int OdinCredits { get; set; } = 50;
		public List<ItemReward> Items { get; set; } = new();
	}

	[Serializable]
	public class ItemReward
	{
		public string ItemName { get; set; }
		public int Amount { get; set; } = 1;
		public int Quality { get; set; } = 1;
	}

	[Serializable]
	public class FactionQuestConfig
	{
		public List<FactionQuestDef> Quests { get; set; } = new();
	}

	public static class FactionQuestManager
	{
		internal static Dictionary<string, FactionQuestDef> AvailableQuests = new();
		static Dictionary<string, Dictionary<string, int>> _playerProgress = new();
		static string _configPath;
		static string _cachedYaml;

		public static void LoadQuests(string yamlPath)
		{
			_configPath = yamlPath;
			try
			{
				if(!File.Exists(yamlPath))
				{
					Plugin.logger.LogWarning($"[FactionQuests] Config not found at {yamlPath}, creating defaults");
					CreateDefaultQuests(yamlPath);
					return;
				}

				var yaml = File.ReadAllText(yamlPath);
				_cachedYaml = yaml;
				ParseYaml(yaml);

				if(ZNet.instance != null && ZNet.instance.IsServer())
					Plugin.SyncedQuestConfig.Value = yaml;
			}
			catch(Exception ex)
			{
				Plugin.logger.LogError($"[FactionQuests] Failed to load: {ex.Message}");
				CreateDefaultQuests(yamlPath);
			}
		}

		public static void ApplyYaml(string yaml)
		{
			_cachedYaml = yaml;
			ParseYaml(yaml);
			Plugin.logger.LogInfo("[FactionQuests] Client received config from server via ConfigSync");
		}

		static void ParseYaml(string yaml)
		{
			try
			{
				var deserializer = new DeserializerBuilder()
					.WithNamingConvention(PascalCaseNamingConvention.Instance)
					.Build();

				var config = deserializer.Deserialize<FactionQuestConfig>(yaml);
				if(config?.Quests == null) return;
				AvailableQuests.Clear();

				foreach(var quest in config.Quests)
				{
					AvailableQuests[quest.ID] = quest;
				}

				EnrichWithGemRewards();
				Plugin.logger.LogInfo($"[FactionQuests] Loaded {AvailableQuests.Count} quests");
			}
			catch(Exception e)
			{
				DBG.blogWarning($"[FactionQuests] Failed to parse YAML: {e.Message}");
			}
		}

		static void EnrichWithGemRewards()
		{
			if(!JewelcraftingCompat.IsActive) return;

			foreach(var quest in AvailableQuests.Values)
			{
				var gemRewards = JewelcraftingCompat.GetGemRewardsForTier(quest.RequiredReputation);
				if(gemRewards.Count > 0)
					quest.Reward.Items.AddRange(gemRewards);
			}

			Plugin.logger.LogInfo($"[FactionQuests] Enriched {AvailableQuests.Count} quests with Jewelcrafting gem rewards");
		}

		static void CreateDefaultQuests(string path)
		{
			var yaml = @"Quests:
  - ID: redteam_hunt_01
    Name: Greydwarf Menace
    FactionName: RedTeam
    RequiredReputation: 10
    Description: Hunt down 5 Greydwarfs threatening our village
    Objective:
      Type: Kill
      Target: Greydwarf
      Amount: 5
      Biome: BlackForest
    Reward:
      ReputationGain: 35
      OdinCredits: 50
      Items:
        - ItemName: OdinLegacy
          Amount: 3
          Quality: 1

  - ID: redteam_gather_01
    Name: Stone Collection
    FactionName: RedTeam
    RequiredReputation: 0
    Description: Gather 50 stone for our construction project
    Objective:
      Type: Collect
      Target: Stone
      Amount: 50
      Biome: Any
    Reward:
      ReputationGain: 15
      OdinCredits: 30
      Items:
        - ItemName: Coins
          Amount: 100
          Quality: 1

  - ID: blueteam_hunt_01
    Name: Wolf Pack Elimination
    FactionName: BlueTeam
    RequiredReputation: 10
    Description: Eliminate the wolf pack in the mountains
    Objective:
      Type: Kill
      Target: Wolf
      Amount: 10
      Biome: Mountain
    Reward:
      ReputationGain: 50
      OdinCredits: 75
      Items:
        - ItemName: OdinLegacy
          Amount: 5
          Quality: 2

  - ID: blueteam_deliver_01
    Name: Iron Shipment
    FactionName: BlueTeam
    RequiredReputation: 30
    Description: Deliver 30 iron bars to our smithy
    Objective:
      Type: Deliver
      Target: Iron
      Amount: 30
      Biome: Any
    Reward:
      ReputationGain: 40
      OdinCredits: 100
      Items:
        - ItemName: OdinLegacy
          Amount: 10
          Quality: 2

  - ID: greenteam_explore_01
    Name: Scout the Swamp
    FactionName: GreenTeam
    RequiredReputation: 10
    Description: Explore and mark 3 locations in the swamp
    Objective:
      Type: Explore
      Target: Swamp
      Amount: 3
      Biome: Swamp
    Reward:
      ReputationGain: 30
      OdinCredits: 60
      Items:
        - ItemName: OdinLegacy
          Amount: 4
          Quality: 1

  - ID: greenteam_collect_01
    Name: Ancient Seed Harvest
    FactionName: GreenTeam
    RequiredReputation: 30
    Description: Collect 5 Ancient Seeds from the Black Forest
    Objective:
      Type: Collect
      Target: AncientSeed
      Amount: 5
      Biome: BlackForest
    Reward:
      ReputationGain: 45
      OdinCredits: 80
      Items:
        - ItemName: OdinLegacy
          Amount: 8
          Quality: 2
        - ItemName: Coins
          Amount: 200
          Quality: 1
";
			File.WriteAllText(path, yaml);
			_cachedYaml = yaml;
			ParseYaml(yaml);
		}

		public static List<FactionQuestDef> GetAvailableQuestsForPlayer(string playerID, string factionName)
		{
			var available = new List<FactionQuestDef>();
			var playerRep = FactionManager.GetReputation(playerID, factionName);

			foreach(var quest in AvailableQuests.Values)
			{
				if(quest.FactionName == factionName && playerRep >= quest.RequiredReputation)
				{
					// Check if not already completed (simple check)
					var progressKey = $"{playerID}_{quest.ID}";
					if(!_playerProgress.ContainsKey(progressKey) ||
					    !_playerProgress[progressKey].ContainsKey("completed"))
					{
						available.Add(quest);
					}
				}
			}

			return available;
		}

		public static void StartQuest(string playerID, string questID)
		{
			var progressKey = $"{playerID}_{questID}";
			if(!_playerProgress.ContainsKey(progressKey))
			{
				_playerProgress[progressKey] = new();
			}
			_playerProgress[progressKey]["progress"] = 0;
			_playerProgress[progressKey]["started"] = 1;
			Plugin.logger.LogInfo($"[FactionQuests] Player {playerID} started quest {questID}");
		}

		public static void UpdateProgress(string playerID, string questID, int amount)
		{
			var progressKey = $"{playerID}_{questID}";
			if(!_playerProgress.ContainsKey(progressKey))
			{
				_playerProgress[progressKey] = new();
			}

			var current = _playerProgress[progressKey].ContainsKey("progress")
				? _playerProgress[progressKey]["progress"]
				: 0;
			_playerProgress[progressKey]["progress"] = current + amount;
		}

		public static void CompleteQuest(string playerID, string questID)
		{
			if(!AvailableQuests.ContainsKey(questID)) return;

			var quest = AvailableQuests[questID];
			var progressKey = $"{playerID}_{questID}";

			// Mark completed
			if(!_playerProgress.ContainsKey(progressKey))
			{
				_playerProgress[progressKey] = new();
			}
			_playerProgress[progressKey]["completed"] = 1;

			// Apply rewards - server-authoritative
			if(ZNet.instance.IsServer())
			{
				FactionManager.ModifyReputation(playerID, quest.FactionName, quest.Reward.ReputationGain, true);
			}
			else
			{
				ZRoutedRpc.instance.InvokeRoutedRPC(0L, "ReputationChange", playerID, quest.FactionName, quest.Reward.ReputationGain);
			}

			// Credits handled by calling code (needs Player reference)
			Plugin.logger.LogInfo($"[FactionQuests] Player {playerID} completed quest {questID}");
		}

		public static int GetProgress(string playerID, string questID)
		{
			var progressKey = $"{playerID}_{questID}";
			if(!_playerProgress.ContainsKey(progressKey)) return 0;
			return _playerProgress[progressKey].ContainsKey("progress")
				? _playerProgress[progressKey]["progress"]
				: 0;
		}

		public static bool IsQuestCompleted(string playerID, string questID)
		{
			var progressKey = $"{playerID}_{questID}";
			return _playerProgress.ContainsKey(progressKey) &&
			       _playerProgress[progressKey].ContainsKey("completed");
		}

		public static Dictionary<string, Dictionary<string, int>> GetAllProgress() => _playerProgress;

		public static void LoadProgress(Dictionary<string, Dictionary<string, int>> progress)
		{
			_playerProgress = progress ?? new();
		}
	}
}
