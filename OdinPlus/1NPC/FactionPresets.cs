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
				Factions = new()
				{
					{ "Nords", new() { Allies = new() { "Merchants" }, Enemies = new() { "Raiders", "Beasts" } } },
					{ "Raiders", new() { Allies = new() { "Beasts" }, Enemies = new() { "Nords", "Merchants" } } },
					{ "Merchants", new() { Allies = new() { "Nords" }, Enemies = new() { "Raiders" } } },
					{ "Beasts", new() { Allies = new() { "Raiders" }, Enemies = new() { "Nords" } } }
				}
			};
		}

		public static FactionConfig GetTeamDeathmatchPreset()
		{
			return new FactionConfig
			{
				Factions = new()
				{
					{ "RedTeam", new() { Allies = new(), Enemies = new() { "BlueTeam", "GreenTeam", "YellowTeam" } } },
					{ "BlueTeam", new() { Allies = new(), Enemies = new() { "RedTeam", "GreenTeam", "YellowTeam" } } },
					{ "GreenTeam", new() { Allies = new(), Enemies = new() { "RedTeam", "BlueTeam", "YellowTeam" } } },
					{ "YellowTeam", new() { Allies = new(), Enemies = new() { "RedTeam", "BlueTeam", "GreenTeam" } } }
				}
			};
		}

		public static FactionConfig GetAllianceWarPreset()
		{
			return new FactionConfig
			{
				Factions = new()
				{
					{ "RedTeam", new() { Allies = new() { "GreenTeam" }, Enemies = new() { "BlueTeam", "YellowTeam" } } },
					{ "BlueTeam", new() { Allies = new() { "YellowTeam" }, Enemies = new() { "RedTeam", "GreenTeam" } } },
					{ "GreenTeam", new() { Allies = new() { "RedTeam" }, Enemies = new() { "BlueTeam", "YellowTeam" } } },
					{ "YellowTeam", new() { Allies = new() { "BlueTeam" }, Enemies = new() { "RedTeam", "GreenTeam" } } }
				}
			};
		}

		public static FactionConfig GetKingdomsPreset()
		{
			return new FactionConfig
			{
				Factions = new()
				{
					{ "Kingdom", new() { Allies = new() { "Villagers", "Traders" }, Enemies = new() { "Bandits", "Monsters" } } },
					{ "Villagers", new() { Allies = new() { "Kingdom", "Traders" }, Enemies = new() { "Bandits", "Monsters" } } },
					{ "Traders", new() { Allies = new() { "Kingdom", "Villagers" }, Enemies = new() { "Bandits" } } },
					{ "Bandits", new() { Allies = new() { "Monsters" }, Enemies = new() { "Kingdom", "Villagers", "Traders" } } },
					{ "Monsters", new() { Allies = new() { "Bandits" }, Enemies = new() { "Kingdom", "Villagers" } } }
				}
			};
		}

		public static FactionConfig GetNeutralTraderPreset()
		{
			return new FactionConfig
			{
				Factions = new()
				{
					{ "Traders", new() { Allies = new(), Enemies = new() } },
					{ "Warriors", new() { Allies = new(), Enemies = new() { "Invaders" } } },
					{ "Invaders", new() { Allies = new(), Enemies = new() { "Warriors" } } }
				}
			};
		}
	}
}
