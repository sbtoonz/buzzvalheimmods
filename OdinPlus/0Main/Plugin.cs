using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using BepInEx.Logging;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Globalization;
using UnityEngine.UI;
using ConfigSyncAPI;
namespace OdinPlus
{
	[BepInPlugin("buzz.valheim.OdinPlus", "OdinPlus", "0.2.6")]
	[BepInDependency("org.bepinex.plugins.jewelcrafting", BepInDependency.DependencyFlags.SoftDependency)]
	public class Plugin : BaseUnityPlugin
	{
		#region Config Var
		internal static ManualLogSource logger = null!;
		internal static ConfigEntry<bool> CFG_Enabled = null!;
		internal static ConfigEntry<bool> CFG_ServerEnforced = null!;
		internal static ConfigEntry<string> CFG_LogLevel = null!;

		// ConfigSyncAPI — syncs YAML configs from server to all clients automatically
		internal static readonly ConfigSync configSync = new("buzz.valheim.OdinPlus") {
			DisplayName = "OdinPlus",
			CurrentVersion = "0.2.6",
			MinimumRequiredVersion = "0.2.5"
		};
		internal static CustomSyncedValue<string> SyncedFactionConfig = null!;
		internal static CustomSyncedValue<string> SyncedQuestConfig = null!;
		internal static CustomSyncedValue<string> SyncedBlueprintConfig = null!;

		internal static KeyboardShortcut SecondInteractKey
		{
			get
			{
				var g = FactionManager.General;
				var main = KeyCode.E;
				var mod = KeyCode.LeftAlt;
				System.Enum.TryParse(g.SecondInteractKey, out main);
				System.Enum.TryParse(g.SecondInteractModifier, out mod);
				return new(main, mod);
			}
		}

		internal static Vector3 OdinPosition
		{
			get
			{
				var g = FactionManager.General;
				return new(g.OdinPositionX, g.OdinPositionY, g.OdinPositionZ);
			}
		}

		internal static bool ForceOdinPosition => FactionManager.General.ForceOdinPosition;
		internal static bool Set_FOP = false;
		internal static int RaiseCost => FactionManager.Economy.SkillRaiseCost;
		internal static int RaiseFactor => FactionManager.Economy.SkillRaiseFactor;

		Harmony _harmony = null!;
		#endregion
		internal static GameObject OdinPlusRoot = null!;

		#region Actions
		internal static Action posZone = null!;
		internal static Action RegRPC = null!;
		internal static Action<ObjectDB> preODB = null!;
		#endregion Actions

		#region Mono
		void Awake()
		{
			Plugin.logger = base.Logger;
			CFG_Enabled = base.Config.Bind<bool>("General", "Enabled", true, "Enable or disable OdinPlus entirely");
			CFG_ServerEnforced = base.Config.Bind<bool>("General", "ServerEnforced", true, "Server pushes its YAML config to all clients. When false, clients use their own local faction_config.yaml");
			CFG_LogLevel = base.Config.Bind<string>("General", "LogLevel", "Info", "Log verbosity: None, Error, Warn, Info, Debug");
			if(System.Enum.TryParse<LogLevel>(CFG_LogLevel.Value, true, out var lvl))
				DBG.Level = lvl;

			if(!CFG_Enabled.Value)
			{
				logger.LogInfo("OdinPlus is disabled via config");
				return;
			}

			RegRPC = (Action)ReigsterRpc;

			// ConfigSyncAPI: create synced values (triggers static ctor which installs Harmony patches)
			SyncedFactionConfig = new(configSync, "FactionConfig", "");
			SyncedQuestConfig = new(configSync, "QuestConfig", "");
			SyncedBlueprintConfig = new(configSync, "BlueprintConfig", "");

			// Client receives server config via ConfigSyncAPI
			SyncedFactionConfig.ValueChanged += () =>
			{
				if(!ConfigSync.ProcessingServerUpdate) return;
				if(!string.IsNullOrEmpty(SyncedFactionConfig.Value))
					FactionManager.ApplyYaml(SyncedFactionConfig.Value);
			};
			SyncedQuestConfig.ValueChanged += () =>
			{
				if(!ConfigSync.ProcessingServerUpdate) return;
				if(!string.IsNullOrEmpty(SyncedQuestConfig.Value))
					FactionQuestManager.ApplyYaml(SyncedQuestConfig.Value);
			};
			SyncedBlueprintConfig.ValueChanged += () =>
			{
				if(!ConfigSync.ProcessingServerUpdate) return;
				if(!string.IsNullOrEmpty(SyncedBlueprintConfig.Value))
					BlueprintConfig.ApplyYaml(SyncedBlueprintConfig.Value);
			};

			_harmony = Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);

			RegisterConsoleCommands();

			OdinPlusRoot = new("OdinPlus");
			OdinPlusRoot.AddComponent<ResourceAssetManager>();
			OdinPlusRoot.AddComponent<OdinPlus>();

			DontDestroyOnLoad(OdinPlusRoot);
			DBG.blogInfo("OdinPlus Loaded");
		}

		internal static void ReigsterRpc()
		{
			DBG.blogWarning("Starting reg rpc");
		}
		void OnDestroy()
		{
			if(_harmony != null) _harmony.UnpatchSelf();
		}
		#endregion Mono

		#region patch
		#region StoreGui
		[HarmonyPatch(typeof(StoreGui), "Show")]
		static class Prefix_StoreGui_Show
		{
			static void Postfix(StoreGui __instance, Trader trader)
			{
				if(OdinPlus.traderNameList.Contains(trader.m_name))
				{
					OdinTrader.TweakGui(__instance, true);
					return;
				}
			}
		}
		static readonly AccessTools.FieldRef<StoreGui, Trader> s_storeTraderRef =
			AccessTools.FieldRefAccess<StoreGui, Trader>("m_trader");
		static readonly AccessTools.FieldRef<StoreGui, Trader.TradeItem> s_storeSelectedItemRef =
			AccessTools.FieldRefAccess<StoreGui, Trader.TradeItem>("m_selectedItem");
		static readonly MethodInfo s_fillListMethod = AccessTools.Method(typeof(StoreGui), "FillList");

		[HarmonyPatch(typeof(StoreGui), "Hide")]
		static class Prefix_StoreGui_Hide
		{
			static void Prefix(StoreGui __instance)
			{
				var trader = s_storeTraderRef(__instance);
				if(trader != null && OdinPlus.traderNameList.Contains(trader.m_name))
					OdinTrader.TweakGui(__instance, false);
			}
		}
		[HarmonyPatch(typeof(StoreGui), "GetPlayerCoins")]
		static class Postfix_StoreGui_GetPlayerCoins
		{
			static void Postfix(StoreGui __instance, ref int __result)
			{
				var t = s_storeTraderRef(__instance);
				if(t != null && OdinPlus.traderNameList.Contains(t.m_name))
					__result = OdinData.Credits;
			}
		}
		[HarmonyPatch(typeof(StoreGui), "BuySelectedItem")]
		static class Prefix_StoreGui_BuySelectedItem
		{
			static bool Prefix(StoreGui __instance)
			{
				var trader = s_storeTraderRef(__instance);
				if(trader == null || !OdinPlus.traderNameList.Contains(trader.m_name)) return true;

				var m_selectedItem = s_storeSelectedItemRef(__instance);
				if(m_selectedItem == null) return false;
				var stack = Mathf.Min(m_selectedItem.m_stack, m_selectedItem.m_prefab.m_itemData.m_shared.m_maxStackSize);
				if(m_selectedItem.m_price * stack - OdinData.Credits > 0) return false;

				var quality = m_selectedItem.m_prefab.m_itemData.m_quality;
				var variant = m_selectedItem.m_prefab.m_itemData.m_variant;
				if(Player.m_localPlayer.GetInventory().AddItem(m_selectedItem.m_prefab.name, stack, quality, variant, 0L, "") != null)
				{
					OdinData.RemoveCredits(m_selectedItem.m_price * stack);
					__instance.m_buyEffects.Create(__instance.gameObject.transform.position, Quaternion.identity, null, 1f);
					Player.m_localPlayer.ShowPickupMessage(m_selectedItem.m_prefab.m_itemData, m_selectedItem.m_prefab.m_itemData.m_stack);
					s_fillListMethod.Invoke(__instance, null);
					Gogan.LogEvent("Game", "BoughtItem", m_selectedItem.m_prefab.name, 0L);
				}
				return false;
			}
		}

		#endregion
		#region Player and Console and Fejd
		[HarmonyPatch(typeof(Player), "Update")]
		static class Patch_Player_Update
		{
			static void Postfix(Player __instance)
			{
				if(CheckPlayerNull()) return;
				if(BlueprintSelector.IsActive) return;
				if(!SecondInteractKey.IsDown()) return;

				var hoverObj = __instance.GetHoverObject();
				if(hoverObj == null) return;

				// Try root GameObject first
				if(hoverObj.TryGetComponent<OdinInteractable>(out var interactable))
				{
					interactable.SecondaryInteract(__instance);
					return;
				}
				// If hover object is a child (collider), search up the hierarchy
				interactable = hoverObj.GetComponentInParent<OdinInteractable>();
				if(interactable != null)
					interactable.SecondaryInteract(__instance);
			}
		}

		[HarmonyPatch(typeof(FejdStartup), "Start")]
		static class FejdStartup_Start_Patch
		{
			static void Postfix()
			{
				if(OdinPlus.isInit) return;
				// Load blueprint browser UI from AssetBundle
				BlueprintBrowserAssets.Load();
				OdinPlus.Init();
			}
		}
		#region ConsoleCommands
		static void RegisterConsoleCommands()
		{
			new Terminal.ConsoleCommand("odinhere", "Teleport Odin camp to player", delegate(Terminal.ConsoleEventArgs args)
			{
				if(Player.m_localPlayer == null || !OdinPlus.isNPCInit) return;
				if(Set_FOP)
				{
					LocationManager.GetStartPos();
					return;
				}
				NpcManager.Root.transform.localPosition = Player.m_localPlayer.transform.localPosition + Vector3.forward * 4;
				args.Context.AddString("Odin camp moved to player");
			});

			new Terminal.ConsoleCommand("whereami", "Print current player position", delegate(Terminal.ConsoleEventArgs args)
			{
				if(Player.m_localPlayer == null) return;
				var pos = Player.m_localPlayer.transform.position;
				var s = $"{pos.x:F1},{pos.y:F1},{pos.z:F1}";
				args.Context.AddString(s);
				DBG.cprt(s);
			});

			new Terminal.ConsoleCommand("whereodin", "Print Odin camp position", delegate(Terminal.ConsoleEventArgs args)
			{
				if(Player.m_localPlayer == null || !OdinPlus.isNPCInit) return;
				var pos = NpcManager.Root.transform.localPosition;
				var s = $"{pos.x:F1},{pos.y:F1},{pos.z:F1}";
				args.Context.AddString(s);
				DBG.cprt(s);
			});

			new Terminal.ConsoleCommand("setodin", "Save current Odin position to config", delegate(Terminal.ConsoleEventArgs args)
			{
				if(!OdinPlus.isNPCInit) return;
				var pos = NpcManager.Root.transform.localPosition;
				FactionManager.General.OdinPositionX = pos.x;
				FactionManager.General.OdinPositionY = pos.y;
				FactionManager.General.OdinPositionZ = pos.z;
				args.Context.AddString("Odin position saved (update faction_config.yaml to persist)");
			});

			new Terminal.ConsoleCommand("findfarm", "[locationName] - Reveal NPC camp locations on map (admin only). No args = all camps.", delegate(Terminal.ConsoleEventArgs args)
			{
				if(Player.m_localPlayer == null) return;
				if(!ZNet.instance.IsServer() && !ZNet.instance.IsLocalInstance())
				{
					args.Context.AddString("Admin only");
					return;
				}
				if(args.Length >= 2)
				{
					var locName = args[1];
					var count = LocationManager.RevealAllLocations(locName);
					args.Context.AddString($"Revealed {count} '{locName}' locations on map");
				}
				else
				{
					var total = 0;
					foreach(var loc in FactionManager.VillageLocations)
						total += LocationManager.RevealAllLocations(loc);
					args.Context.AddString($"Revealed {total} NPC camp locations across {FactionManager.VillageLocations.Count} location types");
				}
			}, isCheat: true);

			new Terminal.ConsoleCommand("scanblueprint", "[name] [radius] - Scan built structures as blueprint", delegate(Terminal.ConsoleEventArgs args)
			{
				if(Player.m_localPlayer == null) return;
				if(args.Length < 3)
				{
					args.Context.AddString("Usage: scanblueprint <name> <radius> [woodCost] [stoneCost]");
					return;
				}
				var name = args[1];
				if(!float.TryParse(args[2], out float radius)) { args.Context.AddString("Invalid radius"); return; }
				var woodCost = (args.Length > 3 && int.TryParse(args[3], out int w)) ? w : 0;
				var stoneCost = (args.Length > 4 && int.TryParse(args[4], out int st)) ? st : 0;
				BlueprintScanner.Instance.ScanArea(name, radius, woodCost, stoneCost);
			});

			new Terminal.ConsoleCommand("selectblueprint", "[name] - Start visual blueprint selection mode", delegate(Terminal.ConsoleEventArgs args)
			{
				if(Player.m_localPlayer == null) return;
				if(args.Length < 2)
				{
					args.Context.AddString("Usage: selectblueprint <name>");
					return;
				}
				BlueprintSelector.Instance.StartSelection(args[1]);
			});

			new Terminal.ConsoleCommand("previewscan", "[radius] - Preview scan area with markers", delegate(Terminal.ConsoleEventArgs args)
			{
				if(Player.m_localPlayer == null) return;
				if(args.Length < 2 || !float.TryParse(args[1], out float radius))
				{
					args.Context.AddString("Usage: previewscan <radius>");
					return;
				}
				BlueprintScanner.Instance.PreviewScanArea(radius);
			});

			new Terminal.ConsoleCommand("listblueprints", "List all loaded blueprints", delegate(Terminal.ConsoleEventArgs args)
			{
				var blueprints = BlueprintConfig.GetAllBlueprints();
				if(blueprints.Count == 0)
				{
					args.Context.AddString("No blueprints loaded.");
					return;
				}
				foreach(var bp in blueprints)
					args.Context.AddString(bp.name);
			});
		}
		#endregion ConsoleCommands


		#endregion
		#region Misc


		[HarmonyPatch(typeof(Localization), "SetupLanguage")]
		static class MyLocalizationPatch
		{
			static void Postfix(Localization __instance, string language)
			{
				BuzzLocal.init(language, __instance);
				BuzzLocal.UpdateDictinary();
			}
		}

		[HarmonyPatch(typeof(PlayerProfile), "SavePlayerToDisk")]
		static class PlayerProfile_SavePlayerData_Patch
		{
			static void Prefix(PlayerProfile __instance)
			{
				if(CheckPlayerNull()) return;
				OdinData.saveOdinData($"{Player.m_localPlayer.GetPlayerName()}_{ZNet.instance.GetWorldName()}");
			}
		}

		[HarmonyPatch(typeof(PlayerProfile), "LoadPlayerData")]
		static class Patch_PlayerProfile_LoadPlayerData
		{
			static void Postfix()
			{
				if(ZNet.instance == null) return;
				if(CheckPlayerNull() || OdinPlus.m_instance.isLoaded) return;
				OdinData.loadOdinData($"{Player.m_localPlayer.GetPlayerName()}_{ZNet.instance.GetWorldName()}");
			}
		}
		[HarmonyPatch(typeof(Tameable), "GetHoverText")]
		static class Postfix_Tameable_GetHoverText
		{
			static string _cachedWolfHoverText;
			static void Postfix(Tameable __instance, ref string __result)
			{
				if(!__instance.TryGetComponent<Character>(out var character)) return;
				if(character.m_name != "$op_wolf_name") return;

				if(_cachedWolfHoverText == null)
				{
					_cachedWolfHoverText = Localization.instance.Localize(
						$"\n<color=yellow><b>[{SecondInteractKey.MainKey}]</b></color>$op_wolf_use");
				}
				__result += _cachedWolfHoverText;
			}
		}

		#region BlueprintTool
		// Hijacks the vanilla Hammer's own piece-selection window (Hud.SelectionWindow) whenever
		// BlueprintTool is equipped, so the tool gets identical equip/animation/camera/hotkey behavior
		// (via Humanoid.SetupEquipment's existing m_buildPieces check) for free, with only the "open
		// build menu" moment redirected to our own Blueprint Browser instead.
		[HarmonyPatch(typeof(Hud), "TogglePieceSelection")]
		static class Prefix_Hud_TogglePieceSelection_BlueprintTool
		{
			static bool Prefix()
			{
				if(Player.m_localPlayer == null || !Player.m_localPlayer.InPlaceMode()) return true;
				Player.m_localPlayer.GetBuildSelection(out _, out _, out _, out _, out PieceTable pieceTable);
				if(pieceTable != OdinItem.BlueprintToolPieceTable)
					return true; // Not our tool - vanilla Hammer/other tools behave normally
				BlueprintBrowser.Toggle();
				return false; // Skip vanilla piece-selection window entirely
			}
		}

		// Makes camera/minimap/console/menu input-gating treat our Browser exactly like vanilla's own
		// piece-selection window (see GameCamera.UpdateMouseCapture, which unlocks the cursor based on
		// this same flag) - without this, the Browser would open but the mouse would stay locked/hidden.
		[HarmonyPatch(typeof(Hud), "IsPieceSelectionVisible")]
		static class Postfix_Hud_IsPieceSelectionVisible_BlueprintTool
		{
			static void Postfix(ref bool __result)
			{
				if(BlueprintBrowser.IsVisible) __result = true;
			}
		}
		#endregion BlueprintTool


		#endregion
		#region ZnetScene
		[HarmonyPatch(typeof(ZNetScene), "Awake")]
		static class ZNetScene_Awake_Prefix
		{
			static void Prefix(ZNetScene __instance) => OdinPlus.PreZNS(__instance);
		}
		[HarmonyPriority(600)]
		[HarmonyBefore(new string[] { "buzz.valheim.AllTameable", "org.bepinex.plugins.creaturelevelcontrol" })]
		[HarmonyPatch(typeof(ZNetScene), "Awake")]
		static class ZNetScene_Awake_Patch
		{
			static void Postfix(ZNetScene __instance) => OdinPlus.PostZNS();
		}
		[HarmonyPatch(typeof(ZNetScene), "Shutdown")]
		static class ZNetScene_Shutdown_Patch
		{
			static void Postfix()
			{
				if(ZNet.instance.IsDedicated() && ZNet.instance.IsServer())
					OdinData.saveOdinData(ZNet.instance.GetWorldName());
				OdinPlus.UnRegister();
				OdinPlus.Clear();
			}
		}
		#endregion
		#region ZoneSystem
		[HarmonyPatch(typeof(ZoneSystem), "Start")]
		static class Postfix_ZoneSystem_Start
		{
			static void Postfix()
			{
				// posZone subscribers (HumanManager.PostZone, etc.) run here — they don't need
				// m_locationInstances. OdinPlus.PostZone is deferred to ZNet.Start postfix because
				// ZoneSystem.Start fires BEFORE ZNet.Start → ServerLoadWorld → ZoneSystem.Load
				// populates m_locationInstances (confirmed via log: "Loaded 11430 locations" comes
				// after "Zonesystem Start" but after "ZNET START").
				if(posZone != null)
				{
					try
					{
						posZone();
					}
					catch(Exception e)
					{
						DBG.blogWarning($"[Plugin] posZone threw: {e}");
					}
				}
			}
		}
		[HarmonyPatch(typeof(ZoneSystem), "Start")]
		static class Prefix_ZoneSystem_Start
		{
			static void Prefix()
			{
				//LocationMarker.HackLoctaions();
			}
		}
		[HarmonyPatch(typeof(DungeonGenerator), "Awake")]
		static class Postfix_DungeonDB_Awake
		{
			static void Postfix(DungeonGenerator __instance)
			{
				if(__instance.GetComponent<ZNetView>())
					__instance.gameObject.AddComponent<LocationMarker>();
			}
		}
		[HarmonyPatch(typeof(LocationProxy), "Awake")]
		static class Postfix_LocationProxy_Awake
		{
			static void Postfix(LocationProxy __instance)
			{
				if(__instance.GetComponentInChildren<DungeonGenerator>(true) != null) return;
				__instance.gameObject.AddComponent<LocationMarker>();
			}
		}

		// Village-NPC-regression fix: LocationProxy is the persistent marker ZoneSystem spawns at the
		// real world position of every placed location ("WoodFarm1" villages included). Fires via
		// SetLocation() the very first time a location is ever generated, and via Awake() alone on every
		// later zone reload once the ZDO is persisted - patch both so we never miss the seed event.
		// See HumanManager.TrySeedVillage for the idempotency guard (dedup via a custom ZDO flag).
		[HarmonyPatch(typeof(LocationProxy), "Awake")]
		static class Postfix_LocationProxy_Awake_VillageSeed
		{
			static void Postfix(LocationProxy __instance) => HumanManager.TrySeedVillage(__instance);
		}
		[HarmonyPatch(typeof(LocationProxy), "SetLocation")]
		static class Postfix_LocationProxy_SetLocation_VillageSeed
		{
			static void Postfix(LocationProxy __instance) => HumanManager.TrySeedVillage(__instance);
		}

		#endregion ZoneSystem
		#region ODB
		[HarmonyPatch(typeof(ObjectDB), "Awake")]
		static class Prefix_ObjectDB_Awake
		{
			static void Prefix(ObjectDB __instance) => preODB(__instance);
		}


		[HarmonyPatch(typeof(ObjectDB), "Awake")]
		static class Patch_ObjectDB_Awake
		{
			static void Postfix(ObjectDB __instance) => OdinPlus.PostODB();
		}

		#endregion
		#region Znet
		[HarmonyPatch(typeof(ZNet), "Awake")]
		static class Postfix_ZNet_Awake
		{
			static void Postfix()
			{
				RegRPC();
				LocationManager.RequestServerFop();
			}
		}

		[HarmonyPatch(typeof(ZNet), "Start")]
		static class Postfix_ZNet_Start
		{
			// Runs AFTER ServerLoadWorld() → LoadWorld() → ZoneSystem.instance.Load()
			// so m_locationInstances is populated and FindClosestLocation works.
			static void Postfix() => OdinPlus.PostZone();
		}

		#endregion znet
		#region CreatureSpawner
		[HarmonyPatch(typeof(CreatureSpawner), "Spawn")]
		static class Postfix_CreatureSpawner_Spawn
		{
			static void Postfix(CreatureSpawner __instance)
			{
				var spawnerZnv = __instance.GetComponent<ZNetView>();
				if(spawnerZnv == null || spawnerZnv.GetZDO() == null) return;
				var faction = spawnerZnv.GetZDO().GetString("npc_faction", "");
				if(string.IsNullOrEmpty(faction)) return;

				var patrol = spawnerZnv.GetZDO().GetFloat("npc_patrol_radius", 0f);
				// Find the spawned creature (child or nearby with our NPC component)
				var npc = __instance.GetComponentInChildren<HumanNPC>(true);
				if(npc == null)
				{
					// CreatureSpawner spawns at its own position — search nearby
					var cols = Physics.OverlapSphere(__instance.transform.position, 2f);
					foreach(var col in cols)
					{
						npc = col.GetComponentInParent<HumanNPC>();
						if(npc != null) break;
					}
				}
				if(npc == null) return;

				npc.FactionName = faction;
				var npcZnv = npc.GetComponent<ZNetView>();
				if(npcZnv != null && npcZnv.GetZDO() != null)
					npcZnv.GetZDO().Set("npc_faction", faction);

				if(patrol > 0f)
				{
					var ai = npc.GetComponent<MonsterAI>();
					if(ai != null) ai.m_randomMoveRange = patrol;
				}
			}
		}
		#endregion CreatureSpawner
		#region container
		[HarmonyPatch(typeof(Container), "Interact")]
		static class Postfix_Container_Interact
		{
			static void Postfix(Container __instance, Humanoid character, bool hold)
			{
				var a = __instance.GetComponent<LegacyChest>();
				if(a)
					a.OnOpen(character, hold);
			}
		}
		#endregion container
		#region Charactor
		[HarmonyPatch(typeof(Character), "GetHoverText")]
		static class Prefix_Character_GetHoverText
		{
			static bool Prefix(Character __instance, ref string __result)
			{
				if(__instance.TryGetComponent<HumanNPC>(out var npc))
				{
					__result = npc.GetHoverText();
					return false;
				}
				return true;
			}
		}
		#endregion Charactor

		// ZNetScene_RemoveObjects_NullFix REMOVED — root cause fixed:
		// All template-creation Instantiate() calls now use ZNetView.m_forceDisableInit = true,
		// preventing ZNetView.Awake() from registering phantom entries in m_instances.
		// No null entries = RemoveObjects works correctly = distant-object culling stays healthy.

		#region BlueprintZoomBlock
		[HarmonyPatch(typeof(GameCamera), "UpdateCamera")]
		static class GameCamera_BlockZoomDuringSelection
		{
			static void Prefix(GameCamera __instance, ref float __state)
			{
				if(BlueprintSelector.IsActive)
				{
					__state = __instance.m_zoomSens;
					__instance.m_zoomSens = 0f;
				}
			}
			static void Postfix(GameCamera __instance, float __state)
			{
				if(BlueprintSelector.IsActive && __state > 0f)
					__instance.m_zoomSens = __state;
			}
		}
		#endregion BlueprintZoomBlock

		#endregion patch

		#region Tool
		internal static bool CheckPlayerNull(bool log = false)
		{
			if(Player.m_localPlayer == null)
			{
				if(log) DBG.blogWarning("Player is Null");
				return true;
			}
			return false;
		}


		#endregion

		#region Delegates
		#endregion Delegates
	}

}
