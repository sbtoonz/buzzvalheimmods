using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using UnityEngine;

namespace OdinPlus
{
	/// <summary>
	/// YAML-based blueprint storage and sync system
	/// </summary>
	[Serializable]
	public class BlueprintPieceData
	{
		public string PrefabName { get; set; }
		public float PosX { get; set; }
		public float PosY { get; set; }
		public float PosZ { get; set; }
		public float RotX { get; set; }
		public float RotY { get; set; }
		public float RotZ { get; set; }
	}

	[Serializable]
	public class BlueprintData
	{
		public string Name { get; set; }
		public Dictionary<string, int> ResourceCosts { get; set; } = new Dictionary<string, int>();
		public List<BlueprintPieceData> Pieces { get; set; } = new List<BlueprintPieceData>();
		// Optional: NPC faction names allowed to build this blueprint. Omit or leave empty for "any faction".
		public List<string> AllowedFactions { get; set; } = new List<string>();
	}

	[Serializable]
	public class BlueprintConfigFile
	{
		public List<BlueprintData> Blueprints { get; set; } = new List<BlueprintData>();
	}

	public static class BlueprintConfig
	{
		private static string BlueprintsFolder => Path.Combine(BepInEx.Paths.ConfigPath, "blueprints");
		private static Dictionary<string, Blueprint> _loadedBlueprints = new Dictionary<string, Blueprint>();

		/// <summary>
		/// Load all blueprints from YAML files in blueprints/ folder
		/// </summary>
		public static void LoadFromFile()
		{
			if (!Directory.Exists(BlueprintsFolder))
			{
				Directory.CreateDirectory(BlueprintsFolder);
				DBG.blogInfo($"[BlueprintConfig] Created blueprints folder at {BlueprintsFolder}");
				CreateDefaultBlueprint();
				return;
			}

			try
			{
				var yamlFiles = Directory.GetFiles(BlueprintsFolder, "*.yaml");
				if (yamlFiles.Length == 0)
				{
					DBG.blogWarning($"[BlueprintConfig] No YAML files in {BlueprintsFolder}, creating default");
					CreateDefaultBlueprint();
					return;
				}

				_loadedBlueprints.Clear();
				foreach (var file in yamlFiles)
				{
					LoadBlueprintFile(file);
				}

				DBG.blogInfo($"[BlueprintConfig] Loaded {_loadedBlueprints.Count} blueprints from {yamlFiles.Length} files");

				// Update Blueprints.All with loaded data
				Blueprints.All.Clear();
				Blueprints.All.AddRange(_loadedBlueprints.Values);
				SyncVillagersAssignment();
			}
			catch (Exception ex)
			{
				DBG.blogError($"[BlueprintConfig] Failed to load blueprints: {ex.Message}");
			}
		}

		private static void LoadBlueprintFile(string filePath)
		{
			try
			{
				string yaml = File.ReadAllText(filePath);
				var deserializer = new DeserializerBuilder()
					.WithNamingConvention(PascalCaseNamingConvention.Instance)
					.Build();

				var bpData = deserializer.Deserialize<BlueprintData>(yaml);

				// Convert YAML data to Blueprint object
				var pieces = bpData.Pieces.Select(p => new BlueprintPiece(
					p.PrefabName,
					new Vector3(p.PosX, p.PosY, p.PosZ),
					new Vector3(p.RotX, p.RotY, p.RotZ)
				)).ToArray();

				var blueprint = new Blueprint(bpData.Name, bpData.ResourceCosts, pieces, bpData.AllowedFactions);
				_loadedBlueprints[bpData.Name] = blueprint;

				DBG.blogInfo($"[BlueprintConfig] Loaded '{bpData.Name}' from {Path.GetFileName(filePath)}");
			}
			catch (Exception ex)
			{
				DBG.blogError($"[BlueprintConfig] Failed to load {Path.GetFileName(filePath)}: {ex.Message}");
			}
		}

		/// <summary>
		/// Parse YAML string into blueprints (for server sync)
		/// </summary>
		public static void ParseYaml(string yaml)
		{
			try
			{
				var deserializer = new DeserializerBuilder()
					.WithNamingConvention(PascalCaseNamingConvention.Instance)
					.Build();

				var config = deserializer.Deserialize<BlueprintConfigFile>(yaml);
				if (config?.Blueprints == null) return;
				_loadedBlueprints.Clear();

				foreach (var bpData in config.Blueprints)
				{
					var pieces = bpData.Pieces.Select(p => new BlueprintPiece(
						p.PrefabName,
						new Vector3(p.PosX, p.PosY, p.PosZ),
						new Vector3(p.RotX, p.RotY, p.RotZ)
					)).ToArray();

					var blueprint = new Blueprint(bpData.Name, bpData.ResourceCosts, pieces, bpData.AllowedFactions);
					_loadedBlueprints[bpData.Name] = blueprint;
				}

				DBG.blogInfo($"[BlueprintConfig] Loaded {_loadedBlueprints.Count} blueprints from sync YAML");

				Blueprints.All.Clear();
				Blueprints.All.AddRange(_loadedBlueprints.Values);
				SyncVillagersAssignment();
			}
			catch (Exception e)
			{
				DBG.blogWarning("[BlueprintConfig] Failed to parse blueprint YAML: " + e.Message);
			}
		}

		// BuilderNPC.FactionName defaults to "Villagers", but no faction_config.yaml ever actually
		// defines a "Villagers" faction - relying on the implicit "empty AllowedFactions = any faction"
		// fallback made it impossible to tell a genuinely-broken assignment from a merely-implicit one.
		// Explicitly (re)assign every currently-loaded blueprint every time the list changes (initial
		// load, a new blueprint saved mid-session, or a sync from server) so Builder NPCs always have a
		// real, visible assignment.
		private static void SyncVillagersAssignment()
		{
			if (!FactionManager.Factions.ContainsKey("Villagers"))
			{
				FactionManager.Factions["Villagers"] = new FactionDef { Name = "Villagers" };
			}
			FactionManager.Factions["Villagers"].AssignedBlueprints = Blueprints.All.Select(bp => bp.name).ToList();
		}

		/// <summary>
		/// Save a new blueprint to individual YAML file
		/// </summary>
		public static void SaveBlueprint(Blueprint blueprint)
		{
			// Ensure folder exists
			if (!Directory.Exists(BlueprintsFolder))
			{
				Directory.CreateDirectory(BlueprintsFolder);
			}

			// Convert Blueprint to BlueprintData
			var bpData = new BlueprintData
			{
				Name = blueprint.name,
				ResourceCosts = new Dictionary<string, int>(blueprint.resourceCosts),
				AllowedFactions = blueprint.allowedFactions != null ? new List<string>(blueprint.allowedFactions) : new List<string>(),
				Pieces = blueprint.pieces.Select(p => new BlueprintPieceData
				{
					PrefabName = p.prefabName,
					PosX = p.localPosition.x,
					PosY = p.localPosition.y,
					PosZ = p.localPosition.z,
					RotX = p.rotation.x,
					RotY = p.rotation.y,
					RotZ = p.rotation.z
				}).ToList()
			};

			// Save to individual file (sanitize name for filename)
			string safeFileName = string.Join("_", blueprint.name.Split(Path.GetInvalidFileNameChars()));
			string filePath = Path.Combine(BlueprintsFolder, $"{safeFileName}.yaml");

			var serializer = new SerializerBuilder()
				.WithNamingConvention(PascalCaseNamingConvention.Instance)
				.Build();
			string yaml = serializer.Serialize(bpData);
			File.WriteAllText(filePath, yaml);

			DBG.blogInfo($"[BlueprintConfig] Saved blueprint '{blueprint.name}' to {filePath}");

			// Reload all blueprints
			LoadFromFile();
		}

		/// <summary>
		/// Export scanned blueprint directly to YAML
		/// </summary>
		public static void ExportScannedBlueprint(string name, List<Piece> pieces, Vector3 origin, Dictionary<string, int> costs)
		{
			// Convert pieces to BlueprintPiece array
			var bpPieces = pieces.Select(piece =>
			{
				string prefabName = piece.name.Replace("(Clone)", "").Trim();
				Vector3 localPos = piece.transform.position - origin;
				Vector3 rotation = piece.transform.rotation.eulerAngles;
				return new BlueprintPiece(prefabName, localPos, rotation);
			}).ToArray();

			// Create Blueprint object
			var blueprint = new Blueprint(name, costs, bpPieces);

			// Save to YAML
			SaveBlueprint(blueprint);

			Player.m_localPlayer.Message(MessageHud.MessageType.Center,
				$"Blueprint '{name}' exported to YAML!");
		}

		private static void CreateDefaultBlueprint()
		{
			var exampleBp = new BlueprintData
			{
				Name = "ExampleHut",
				ResourceCosts = new Dictionary<string, int> { { "Wood", 20 } },
				Pieces = new List<BlueprintPieceData>
				{
					new BlueprintPieceData { PrefabName = "wood_floor_1x1", PosX = 0f, PosY = 0f, PosZ = 0f, RotX = 0f, RotY = 0f, RotZ = 0f },
					new BlueprintPieceData { PrefabName = "wood_floor_1x1", PosX = 2f, PosY = 0f, PosZ = 0f, RotX = 0f, RotY = 0f, RotZ = 0f },
					new BlueprintPieceData { PrefabName = "wood_wall_roof", PosX = -1f, PosY = 0f, PosZ = 1f, RotX = 0f, RotY = 90f, RotZ = 0f },
					new BlueprintPieceData { PrefabName = "wood_wall_roof", PosX = 3f, PosY = 0f, PosZ = 1f, RotX = 0f, RotY = 270f, RotZ = 0f }
				}
			};

			string filePath = Path.Combine(BlueprintsFolder, "ExampleHut.yaml");

			var serializer = new SerializerBuilder()
				.WithNamingConvention(PascalCaseNamingConvention.Instance)
				.Build();
			string yaml = serializer.Serialize(exampleBp);
			File.WriteAllText(filePath, yaml);

			DBG.blogInfo($"[BlueprintConfig] Created default blueprint at {filePath}");
			LoadFromFile();
		}

		/// <summary>
		/// Get all loaded blueprints
		/// </summary>
		public static List<Blueprint> GetAllBlueprints()
		{
			return _loadedBlueprints.Values.ToList();
		}

		/// <summary>
		/// Server: Get combined YAML for syncing all blueprints to clients
		/// </summary>
		public static string GetYamlForSync()
		{
			var config = new BlueprintConfigFile { Blueprints = new List<BlueprintData>() };

			// Combine all individual blueprint files into one config for sync
			if (Directory.Exists(BlueprintsFolder))
			{
				foreach (var file in Directory.GetFiles(BlueprintsFolder, "*.yaml"))
				{
					try
					{
						string yaml = File.ReadAllText(file);
						var deserializer = new DeserializerBuilder()
							.WithNamingConvention(PascalCaseNamingConvention.Instance)
							.Build();
						var bpData = deserializer.Deserialize<BlueprintData>(yaml);
						config.Blueprints.Add(bpData);
					}
					catch (Exception ex)
					{
						DBG.blogError($"[BlueprintConfig] Failed to load {Path.GetFileName(file)} for sync: {ex.Message}");
					}
				}
			}

			var serializer = new SerializerBuilder()
				.WithNamingConvention(PascalCaseNamingConvention.Instance)
				.Build();
			return serializer.Serialize(config);
		}

		/// <summary>
		/// Client: Receive YAML from server
		/// </summary>
		public static void ReceiveYamlFromServer(string yaml)
		{
			DBG.blogInfo("[BlueprintConfig] Received blueprints from server");
			ParseYaml(yaml);
		}

		// These sync methods existed but were never wired to an actual RPC before this pass -
		// GetYamlForSync()/ReceiveYamlFromServer() had zero callers anywhere in the project.
		public static void RegisterRpc()
		{
			ZRoutedRpc.instance.Register<string>("BlueprintConfigSync", new Action<long, string>(RPC_BlueprintConfigSync));
			ZRoutedRpc.instance.Register("RequestBlueprintSync", new Action<long>(RPC_RequestBlueprintSync));
		}

		// Broadcast covers the common case (dedicated server boots before players join). A player
		// joining later requests sync themselves (see RequestSyncFromServer), covering the late-join case.
		public static void BroadcastSync()
		{
			if (ZNet.instance == null || !ZNet.instance.IsServer()) return;
			ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.Everybody, "BlueprintConfigSync", GetYamlForSync());
		}

		public static void RequestSyncFromServer()
		{
			if (ZNet.instance == null || ZNet.instance.IsServer()) return;
			ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.Everybody, "RequestBlueprintSync");
		}

		private static void RPC_BlueprintConfigSync(long sender, string yaml)
		{
			if (ZNet.instance.IsServer()) return; // Server doesn't need its own broadcast
			ReceiveYamlFromServer(yaml);
		}

		private static void RPC_RequestBlueprintSync(long sender)
		{
			if (ZNet.instance == null || !ZNet.instance.IsServer()) return;
			ZRoutedRpc.instance.InvokeRoutedRPC(sender, "BlueprintConfigSync", GetYamlForSync());
		}
	}
}
