using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace OdinPlus
{
	public enum ReputationTier
	{
		Hostile,      // < -30: attacks player, no interaction
		Unfriendly,   // -30 to -10: refuses trades, minimal dialogue
		Neutral,      // -10 to 10: default behavior
		Friendly,     // 10 to 30: discounts, more quests
		Honored       // > 30: best prices, unique quests
	}

	public class FactionDef
	{
		public string Name { get; set; }
		public List<string> Allies { get; set; } = new List<string>();
		public List<string> Enemies { get; set; } = new List<string>();
		public int HostileThreshold { get; set; } = -30;
		public int UnfriendlyThreshold { get; set; } = -10;
		public int NeutralThreshold { get; set; } = 10;
		public int FriendlyThreshold { get; set; } = 30;
		// Blueprint names this faction's BuilderNPCs should build, in priority order. Empty/omitted
		// falls back to every blueprint whose own AllowedFactions permits this faction (or any
		// blueprint at all if that's also empty) - see BuilderNPC.GetEligibleBlueprints.
		public List<string> AssignedBlueprints { get; set; } = new List<string>();
	}

	public class FactionConfig
	{
		public Dictionary<string, FactionDef> Factions { get; set; } = new Dictionary<string, FactionDef>();
		public ReputationEvents ReputationEvents { get; set; } = new ReputationEvents();
	}

	public class ReputationEvents
	{
		public int NPCKilled { get; set; } = -50;
		public int NPCDamaged { get; set; } = -10;
		public int ItemGiven { get; set; } = 15;
		public int QuestCompleted { get; set; } = 35;
	}

	public static class FactionManager
	{
		public static Dictionary<string, FactionDef> Factions = new Dictionary<string, FactionDef>();
		public static ReputationEvents EventValues = new ReputationEvents();

		// Per-player reputation: [factionName][playerZDOID] = score
		private static Dictionary<string, Dictionary<string, int>> _reputation = new Dictionary<string, Dictionary<string, int>>();

		// Config sync via RPC
		private static FileSystemWatcher _fileWatcher;
		private static string _configPath;
		private static string _cachedYaml;
		private static DateTime _lastReloadTime = DateTime.MinValue;

		// None of these RPCs were ever registered with ZRoutedRpc before this pass - InvokeRoutedRPC
		// calls to "FactionConfigSync"/"ReputationChange"/etc silently went nowhere. Combined into
		// Plugin.RegRPC (see OdinPlus.Awake), same pattern QuestManager/LocationManager already use.
		public static void RegisterRpc()
		{
			ZRoutedRpc.instance.Register<string>("FactionConfigSync", new Action<long, string>(RPC_FactionConfigSync));
			ZRoutedRpc.instance.Register<string, string, int>("ReputationChange", new Action<long, string, string, int>(RPC_ReputationChange));
			ZRoutedRpc.instance.Register<string, string, int>("ReputationUpdate", new Action<long, string, string, int>(RPC_ReputationUpdate));
			ZRoutedRpc.instance.Register<string>("ReputationSync", new Action<long, string>(RPC_ReputationSync));
			ZRoutedRpc.instance.Register("RequestFactionSync", new Action<long>(RPC_RequestFactionSync));
		}

		// Broadcast covers the common case (dedicated server boots before players join). A player
		// joining later requests sync themselves (see RequestSyncFromServer), covering the late-join case.
		public static void BroadcastSync()
		{
			if (ZNet.instance == null || !ZNet.instance.IsServer() || _cachedYaml == null) return;
			ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.Everybody, "FactionConfigSync", _cachedYaml);
		}

		public static void RequestSyncFromServer()
		{
			if (ZNet.instance == null || ZNet.instance.IsServer()) return;
			ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.Everybody, "RequestFactionSync");
		}

		private static void RPC_RequestFactionSync(long sender)
		{
			if (ZNet.instance == null || !ZNet.instance.IsServer() || _cachedYaml == null) return;
			ZRoutedRpc.instance.InvokeRoutedRPC(sender, "FactionConfigSync", _cachedYaml);
		}

		public static void LoadConfig(string yamlPath)
		{
			_configPath = yamlPath;
			LoadFromFile(yamlPath);
			SetupFileWatcher(yamlPath);
		}

		private static void LoadFromFile(string yamlPath)
		{
			try
			{
				if (!File.Exists(yamlPath))
				{
					Plugin.logger.LogWarning($"[FactionManager] Config not found at {yamlPath}, using defaults");
					CreateDefaultFactions();
					return;
				}

				string yaml = File.ReadAllText(yamlPath);
				_cachedYaml = yaml;
				ParseYaml(yaml);

				// Server syncs to clients via RPC
				if (ZNet.instance != null && ZNet.instance.IsServer())
				{
					ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.Everybody, "FactionConfigSync", yaml);
					Plugin.logger.LogInfo($"[FactionManager] Server syncing config to clients");
				}
			}
			catch (Exception ex)
			{
				Plugin.logger.LogError($"[FactionManager] Failed to load config: {ex.Message}");
				CreateDefaultFactions();
			}
		}

		private static void ParseYaml(string yaml)
		{
			try
			{
				var deserializer = new DeserializerBuilder()
					.WithNamingConvention(PascalCaseNamingConvention.Instance)
					.Build();

				var config = deserializer.Deserialize<FactionConfig>(yaml);
				if (config?.Factions == null) return;
				Factions = config.Factions;
				EventValues = config.ReputationEvents;

				Plugin.logger.LogInfo($"[FactionManager] Loaded {Factions.Count} factions");
			}
			catch (Exception e)
			{
				DBG.blogWarning("[FactionManager] Failed to parse faction YAML: " + e.Message);
			}
		}

		public static void RPC_FactionConfigSync(long sender, string yaml)
		{
			if (ZNet.instance.IsServer()) return; // Server doesn't receive its own broadcasts

			Plugin.logger.LogInfo($"[FactionManager] Client received config from server");
			_cachedYaml = yaml;
			ParseYaml(yaml);
		}

		// Server receives reputation change request from client
		public static void RPC_ReputationChange(long sender, string playerID, string faction, int delta)
		{
			if (!ZNet.instance.IsServer()) return;

			// Server-authoritative: apply change
			ModifyReputation(playerID, faction, delta, false); // false = no notification (will broadcast)

			// Get new reputation value
			int newRep = GetReputation(playerID, faction);

			// Broadcast new value to all clients
			ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.Everybody, "ReputationUpdate", playerID, faction, newRep);

			Plugin.logger.LogInfo($"[FactionManager] Server applied reputation change: {playerID} {faction} {delta} => {newRep}");
		}

		// All clients (including sender) receive authoritative reputation update from server
		public static void RPC_ReputationUpdate(long sender, string playerID, string faction, int reputation)
		{
			if (!_reputation.ContainsKey(faction))
				_reputation[faction] = new Dictionary<string, int>();

			_reputation[faction][playerID] = reputation;

			Plugin.logger.LogInfo($"[FactionManager] Reputation synced: {playerID} with {faction} = {reputation}");

			// Show notification to the player if it's their reputation
			if (Player.m_localPlayer != null && playerID == Player.m_localPlayer.GetZDOID().ToString())
			{
				ReputationTier tier = GetReputationTier(playerID, faction);
				MessageHud.instance.ShowBiomeFoundMsg($"{faction} reputation: {tier} ({reputation})", false);
			}
		}

		// Server sends full reputation state to joining player
		public static void RPC_ReputationSync(long sender, string yamlReputation)
		{
			if (ZNet.instance.IsServer()) return;

			try
			{
				var deserializer = new DeserializerBuilder().Build();
				_reputation = deserializer.Deserialize<Dictionary<string, Dictionary<string, int>>>(yamlReputation);
				Plugin.logger.LogInfo($"[FactionManager] Client received full reputation sync");
			}
			catch (Exception ex)
			{
				Plugin.logger.LogError($"[FactionManager] Failed to deserialize reputation: {ex.Message}");
			}
		}

		private static void SetupFileWatcher(string yamlPath)
		{
			if (_fileWatcher != null) return;

			try
			{
				string directory = Path.GetDirectoryName(yamlPath);
				string filename = Path.GetFileName(yamlPath);

				_fileWatcher = new FileSystemWatcher(directory, filename);
				_fileWatcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size;
				_fileWatcher.Changed += OnConfigFileChanged;
				_fileWatcher.EnableRaisingEvents = true;

				Plugin.logger.LogInfo($"[FactionManager] File watcher enabled for {filename}");
			}
			catch (Exception ex)
			{
				Plugin.logger.LogWarning($"[FactionManager] Could not setup file watcher: {ex.Message}");
			}
		}

		private static void OnConfigFileChanged(object sender, FileSystemEventArgs e)
		{
			// Debounce - file system fires multiple events, skip if changed within 0.5s
			if ((DateTime.Now - _lastReloadTime).TotalSeconds < 0.5)
				return;

			_lastReloadTime = DateTime.Now;
			Plugin.logger.LogInfo($"[FactionManager] Config file changed, reloading...");
			LoadFromFile(_configPath);
		}

		private static void CreateDefaultFactions()
		{
			Factions = new Dictionary<string, FactionDef>
			{
				["Neutral"] = new FactionDef { Name = "Neutral" },
				["RedTeam"] = new FactionDef { Name = "RedTeam", Allies = new List<string> { "GreenTeam" }, Enemies = new List<string> { "BlueTeam" } },
				["BlueTeam"] = new FactionDef { Name = "BlueTeam", Enemies = new List<string> { "RedTeam" } },
				["GreenTeam"] = new FactionDef { Name = "GreenTeam" }
			};
			EventValues = new ReputationEvents();
		}

		public static void Initialize(Dictionary<string, Dictionary<string, int>> savedRep)
		{
			_reputation = savedRep ?? new Dictionary<string, Dictionary<string, int>>();
		}

		public static Dictionary<string, Dictionary<string, int>> GetAllReputation()
		{
			return _reputation;
		}

		// For multiplayer sync: returns copy of reputation data
		public static Dictionary<string, Dictionary<string, int>> GetAllReputationData()
		{
			// Return a copy to avoid concurrent modification
			var copy = new Dictionary<string, Dictionary<string, int>>();
			foreach (var factionKvp in _reputation)
			{
				copy[factionKvp.Key] = new Dictionary<string, int>(factionKvp.Value);
			}
			return copy;
		}

		public static int GetReputation(string playerID, string faction)
		{
			if (string.IsNullOrEmpty(faction)) faction = "Neutral";
			if (!_reputation.ContainsKey(faction)) _reputation[faction] = new Dictionary<string, int>();
			if (!_reputation[faction].ContainsKey(playerID)) return 0;
			return _reputation[faction][playerID];
		}

		public static List<string> GetAllFactions()
		{
			return Factions.Keys.ToList();
		}

		public static void ModifyReputation(string playerID, string faction, int delta, bool applyRelations = true)
		{
			if (string.IsNullOrEmpty(faction)) faction = "Neutral";
			if (!_reputation.ContainsKey(faction)) _reputation[faction] = new Dictionary<string, int>();

			int current = _reputation[faction].ContainsKey(playerID) ? _reputation[faction][playerID] : 0;
			int newRep = Math.Max(-100, Math.Min(100, current + delta));
			_reputation[faction][playerID] = newRep;

			Plugin.logger.LogInfo($"[FactionManager] {playerID} reputation with {faction}: {current} -> {newRep} (delta: {delta})");

			// Apply faction relationships
			if (applyRelations && Factions.TryGetValue(faction, out var factionDef))
			{
				// Helping allies gives slight positive rep
				foreach (var ally in factionDef.Allies)
				{
					if (delta > 0) // Only positive actions help allies
						ModifyReputation(playerID, ally, delta / 3, applyRelations: false);
				}

				// Helping enemies angers this faction's enemies
				foreach (var enemy in factionDef.Enemies)
				{
					ModifyReputation(playerID, enemy, -delta / 2, applyRelations: false);
				}
			}
		}

		public static ReputationTier GetReputationTier(string playerID, string faction)
		{
			int rep = GetReputation(playerID, faction);
			FactionDef thresholds = Factions.ContainsKey(faction) ? Factions[faction] : new FactionDef();

			if (rep < thresholds.HostileThreshold) return ReputationTier.Hostile;
			if (rep < thresholds.UnfriendlyThreshold) return ReputationTier.Unfriendly;
			if (rep < thresholds.FriendlyThreshold) return ReputationTier.Neutral;
			if (rep < thresholds.FriendlyThreshold + 20) return ReputationTier.Friendly;
			return ReputationTier.Honored;
		}

		public static bool ShouldAttackPlayer(string faction, Player player)
		{
			string playerID = player.GetZDOID().ToString();
			return GetReputationTier(playerID, faction) == ReputationTier.Hostile;
		}

		public static float GetPriceModifier(string faction, string playerID)
		{
			ReputationTier tier = GetReputationTier(playerID, faction);
			switch (tier)
			{
				case ReputationTier.Honored: return 0.75f;
				case ReputationTier.Friendly: return 0.90f;
				case ReputationTier.Neutral: return 1.0f;
				case ReputationTier.Unfriendly: return 1.25f;
				case ReputationTier.Hostile: return 999f;
				default: return 1.0f;
			}
		}

		public static void Cleanup()
		{
			if (_fileWatcher != null)
			{
				_fileWatcher.EnableRaisingEvents = false;
				_fileWatcher.Dispose();
				_fileWatcher = null;
			}
		}
	}
}
