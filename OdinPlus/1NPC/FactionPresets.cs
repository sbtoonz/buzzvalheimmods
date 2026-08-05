using System.Collections.Generic;

namespace OdinPlus
{
	/// <summary>
	/// Preset faction configurations for common scenarios
	/// Use: FactionConfig.SaveToFile(FactionPresets.GetClassicWarfarePreset());
	/// </summary>
	public static class FactionPresets
	{
		public static FactionConfig GetClassicWarfarePreset()
		{
			return new FactionConfig
			{
				Factions = new Dictionary<string, FactionDef>
				{
					{ "Nords", new FactionDef { Allies = new List<string> { "Merchants" }, Enemies = new List<string> { "Raiders", "Beasts" } } },
					{ "Raiders", new FactionDef { Allies = new List<string> { "Beasts" }, Enemies = new List<string> { "Nords", "Merchants" } } },
					{ "Merchants", new FactionDef { Allies = new List<string> { "Nords" }, Enemies = new List<string> { "Raiders" } } },
					{ "Beasts", new FactionDef { Allies = new List<string> { "Raiders" }, Enemies = new List<string> { "Nords" } } }
				}
			};
		}

		public static FactionConfig GetTeamDeathmatchPreset()
		{
			return new FactionConfig
			{
				Factions = new Dictionary<string, FactionDef>
				{
					{ "RedTeam", new FactionDef { Allies = new List<string>(), Enemies = new List<string> { "BlueTeam", "GreenTeam", "YellowTeam" } } },
					{ "BlueTeam", new FactionDef { Allies = new List<string>(), Enemies = new List<string> { "RedTeam", "GreenTeam", "YellowTeam" } } },
					{ "GreenTeam", new FactionDef { Allies = new List<string>(), Enemies = new List<string> { "RedTeam", "BlueTeam", "YellowTeam" } } },
					{ "YellowTeam", new FactionDef { Allies = new List<string>(), Enemies = new List<string> { "RedTeam", "BlueTeam", "GreenTeam" } } }
				}
			};
		}

		public static FactionConfig GetAllianceWarPreset()
		{
			return new FactionConfig
			{
				Factions = new Dictionary<string, FactionDef>
				{
					{ "RedTeam", new FactionDef { Allies = new List<string> { "GreenTeam" }, Enemies = new List<string> { "BlueTeam", "YellowTeam" } } },
					{ "BlueTeam", new FactionDef { Allies = new List<string> { "YellowTeam" }, Enemies = new List<string> { "RedTeam", "GreenTeam" } } },
					{ "GreenTeam", new FactionDef { Allies = new List<string> { "RedTeam" }, Enemies = new List<string> { "BlueTeam", "YellowTeam" } } },
					{ "YellowTeam", new FactionDef { Allies = new List<string> { "BlueTeam" }, Enemies = new List<string> { "RedTeam", "GreenTeam" } } }
				}
			};
		}

		public static FactionConfig GetKingdomsPreset()
		{
			return new FactionConfig
			{
				Factions = new Dictionary<string, FactionDef>
				{
					{ "Kingdom", new FactionDef { Allies = new List<string> { "Villagers", "Traders" }, Enemies = new List<string> { "Bandits", "Monsters" } } },
					{ "Villagers", new FactionDef { Allies = new List<string> { "Kingdom", "Traders" }, Enemies = new List<string> { "Bandits", "Monsters" } } },
					{ "Traders", new FactionDef { Allies = new List<string> { "Kingdom", "Villagers" }, Enemies = new List<string> { "Bandits" } } },
					{ "Bandits", new FactionDef { Allies = new List<string> { "Monsters" }, Enemies = new List<string> { "Kingdom", "Villagers", "Traders" } } },
					{ "Monsters", new FactionDef { Allies = new List<string> { "Bandits" }, Enemies = new List<string> { "Kingdom", "Villagers" } } }
				}
			};
		}

		public static FactionConfig GetNeutralTraderPreset()
		{
			return new FactionConfig
			{
				Factions = new Dictionary<string, FactionDef>
				{
					{ "Traders", new FactionDef { Allies = new List<string>(), Enemies = new List<string>() } },
					{ "Warriors", new FactionDef { Allies = new List<string>(), Enemies = new List<string> { "Invaders" } } },
					{ "Invaders", new FactionDef { Allies = new List<string>(), Enemies = new List<string> { "Warriors" } } }
				}
			};
		}
	}
}
