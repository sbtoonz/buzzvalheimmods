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
namespace OdinPlus
{
	[BepInPlugin("buzz.valheim.OdinPlus", "OdinPlus", "0.2.5")]
	public class Plugin : BaseUnityPlugin
	{
		#region Config Var
		public static ManualLogSource logger;
		public static ConfigEntry<bool> CFG_Enabled;
		public static ConfigEntry<bool> CFG_ServerEnforced;
		public static ConfigEntry<string> CFG_LogLevel;

		public static KeyboardShortcut SecondInteractKey
		{
			get
			{
				var g = FactionManager.General;
				KeyCode main = KeyCode.E;
				KeyCode mod = KeyCode.LeftAlt;
				System.Enum.TryParse(g.SecondInteractKey, out main);
				System.Enum.TryParse(g.SecondInteractModifier, out mod);
				return new KeyboardShortcut(main, mod);
			}
		}

		public static Vector3 OdinPosition
		{
			get
			{
				var g = FactionManager.General;
				return new Vector3(g.OdinPositionX, g.OdinPositionY, g.OdinPositionZ);
			}
		}

		public static bool ForceOdinPosition => FactionManager.General.ForceOdinPosition;
		public static bool Set_FOP = false;
		public static int RaiseCost => FactionManager.Economy.SkillRaiseCost;
		public static int RaiseFactor => FactionManager.Economy.SkillRaiseFactor;

		Harmony _harmony;
		#endregion
		public static GameObject OdinPlusRoot;

		#region Actions
		public static Action posZone;
		public static Action RegRPC;
		public static Action<ObjectDB> preODB;
		#endregion Actions

		#region Mono
		private void Awake()
		{
			Plugin.logger = base.Logger;
			CFG_Enabled = base.Config.Bind<bool>("General", "Enabled", true, "Enable or disable OdinPlus entirely");
			CFG_ServerEnforced = base.Config.Bind<bool>("General", "ServerEnforced", true, "Server pushes its YAML config to all clients. When false, clients use their own local faction_config.yaml");
			CFG_LogLevel = base.Config.Bind<string>("General", "LogLevel", "Info", "Log verbosity: None, Error, Warn, Info, Debug");
			if (System.Enum.TryParse<LogLevel>(CFG_LogLevel.Value, true, out var lvl))
				DBG.Level = lvl;

			if (!CFG_Enabled.Value)
			{
				logger.LogInfo("OdinPlus is disabled via config");
				return;
			}

			RegRPC = (Action)ReigsterRpc;

			_harmony = Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);

			RegisterConsoleCommands();

			OdinPlusRoot = new GameObject("OdinPlus");
			OdinPlusRoot.AddComponent<ResourceAssetManager>();
			OdinPlusRoot.AddComponent<OdinPlus>();

			DontDestroyOnLoad(OdinPlusRoot);
			DBG.blogInfo("OdinPlus Loaded");
		}

		public static void ReigsterRpc()
		{
			DBG.blogWarning("Starting reg rpc");
		}
		private void OnDestroy()
		{
			if (_harmony != null) _harmony.UnpatchSelf();
		}
		#endregion Mono

		#region patch		
		#region StoreGui
		[HarmonyPatch(typeof(StoreGui), "Show")]
		private static class Prefix_StoreGui_Show
		{
			private static void Postfix(StoreGui __instance, Trader trader)
			{
				if (OdinPlus.traderNameList.Contains(trader.m_name))
				{
					OdinTrader.TweakGui(__instance, true);
					return;
				}
				return;
			}
		}
		private static readonly AccessTools.FieldRef<StoreGui, Trader> s_storeTraderRef =
			AccessTools.FieldRefAccess<StoreGui, Trader>("m_trader");
		private static readonly AccessTools.FieldRef<StoreGui, Trader.TradeItem> s_storeSelectedItemRef =
			AccessTools.FieldRefAccess<StoreGui, Trader.TradeItem>("m_selectedItem");
		private static readonly MethodInfo s_fillListMethod = AccessTools.Method(typeof(StoreGui), "FillList");

		[HarmonyPatch(typeof(StoreGui), "Hide")]
		private static class Prefix_StoreGui_Hide
		{
			private static void Prefix(StoreGui __instance)
			{
				var trader = s_storeTraderRef(__instance);
				if (trader != null && OdinPlus.traderNameList.Contains(trader.m_name))
				{
					OdinTrader.TweakGui(__instance, false);
				}
			}
		}
		[HarmonyPatch(typeof(StoreGui), "GetPlayerCoins")]
		private static class Postfix_StoreGui_GetPlayerCoins
		{
			private static void Postfix(StoreGui __instance, ref int __result)
			{
				var t = s_storeTraderRef(__instance);
				if (t != null && OdinPlus.traderNameList.Contains(t.m_name))
				{
					__result = OdinData.Credits;
				}
			}
		}
		[HarmonyPatch(typeof(StoreGui), "BuySelectedItem")]
		private static class Prefix_StoreGui_BuySelectedItem
		{
			private static bool Prefix(StoreGui __instance)
			{
				var trader = s_storeTraderRef(__instance);
				if (trader == null || !OdinPlus.traderNameList.Contains(trader.m_name)) return true;

				var m_selectedItem = s_storeSelectedItemRef(__instance);
				if (m_selectedItem == null) return false;
				int stack = Mathf.Min(m_selectedItem.m_stack, m_selectedItem.m_prefab.m_itemData.m_shared.m_maxStackSize);
				if (m_selectedItem.m_price * stack - OdinData.Credits > 0) return false;

				int quality = m_selectedItem.m_prefab.m_itemData.m_quality;
				int variant = m_selectedItem.m_prefab.m_itemData.m_variant;
				if (Player.m_localPlayer.GetInventory().AddItem(m_selectedItem.m_prefab.name, stack, quality, variant, 0L, "") != null)
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
		private static class Patch_Player_Update
		{
			private static void Postfix(Player __instance)
			{
				if (CheckPlayerNull()) return;
				if (BlueprintSelector.IsActive) return;
				if (!SecondInteractKey.IsDown()) return;

				var hoverObj = __instance.GetHoverObject();
				if (hoverObj == null) return;

				// Try root GameObject first
				if (hoverObj.TryGetComponent<OdinInteractable>(out var interactable))
				{
					interactable.SecondaryInteract(__instance);
					return;
				}
				// If hover object is a child (collider), search up the hierarchy
				interactable = hoverObj.GetComponentInParent<OdinInteractable>();
				if (interactable != null)
				{
					interactable.SecondaryInteract(__instance);
				}
			}
		}

		[HarmonyPatch(typeof(FejdStartup), "Start")]
		private static class FejdStartup_Start_Patch
		{
			private static void Postfix()
			{
				if (OdinPlus.isInit)
				{
					return;
				}
				// Load blueprint browser UI from AssetBundle
				BlueprintBrowserAssets.Load();
				OdinPlus.Init();
			}
		}
		#region ConsoleCommands
		private static void RegisterConsoleCommands()
		{
			new Terminal.ConsoleCommand("odinhere", "Teleport Odin camp to player", delegate(Terminal.ConsoleEventArgs args)
			{
				if (Player.m_localPlayer == null || !OdinPlus.isNPCInit) return;
				if (Set_FOP)
				{
					LocationManager.GetStartPos();
					return;
				}
				NpcManager.Root.transform.localPosition = Player.m_localPlayer.transform.localPosition + Vector3.forward * 4;
				args.Context.AddString("Odin camp moved to player");
			});

			new Terminal.ConsoleCommand("whereami", "Print current player position", delegate(Terminal.ConsoleEventArgs args)
			{
				if (Player.m_localPlayer == null) return;
				var pos = Player.m_localPlayer.transform.position;
				string s = $"{pos.x:F1},{pos.y:F1},{pos.z:F1}";
				args.Context.AddString(s);
				DBG.cprt(s);
			});

			new Terminal.ConsoleCommand("whereodin", "Print Odin camp position", delegate(Terminal.ConsoleEventArgs args)
			{
				if (Player.m_localPlayer == null || !OdinPlus.isNPCInit) return;
				var pos = NpcManager.Root.transform.localPosition;
				string s = $"{pos.x:F1},{pos.y:F1},{pos.z:F1}";
				args.Context.AddString(s);
				DBG.cprt(s);
			});

			new Terminal.ConsoleCommand("setodin", "Save current Odin position to config", delegate(Terminal.ConsoleEventArgs args)
			{
				if (!OdinPlus.isNPCInit) return;
				var pos = NpcManager.Root.transform.localPosition;
				FactionManager.General.OdinPositionX = pos.x;
				FactionManager.General.OdinPositionY = pos.y;
				FactionManager.General.OdinPositionZ = pos.z;
				args.Context.AddString("Odin position saved (update faction_config.yaml to persist)");
			});

			new Terminal.ConsoleCommand("findfarm", "[locationName] - Reveal all instances of a location on map (admin only)", delegate(Terminal.ConsoleEventArgs args)
			{
				if (Player.m_localPlayer == null) return;
				if (!ZNet.instance.IsServer() && !ZNet.instance.IsLocalInstance())
				{
					args.Context.AddString("Admin only");
					return;
				}
				string locName = args.Length >= 2 ? args[1] : "WoodFarm1";
				int count = LocationManager.RevealAllLocations(locName);
				args.Context.AddString($"Revealed {count} '{locName}' locations on map");
			}, isCheat: true);

			new Terminal.ConsoleCommand("scanblueprint", "[name] [radius] - Scan built structures as blueprint", delegate(Terminal.ConsoleEventArgs args)
			{
				if (Player.m_localPlayer == null) return;
				if (args.Length < 3)
				{
					args.Context.AddString("Usage: scanblueprint <name> <radius> [woodCost] [stoneCost]");
					return;
				}
				string name = args[1];
				if (!float.TryParse(args[2], out float radius)) { args.Context.AddString("Invalid radius"); return; }
				int woodCost = (args.Length > 3 && int.TryParse(args[3], out int w)) ? w : 0;
				int stoneCost = (args.Length > 4 && int.TryParse(args[4], out int st)) ? st : 0;
				BlueprintScanner.Instance.ScanArea(name, radius, woodCost, stoneCost);
			});

			new Terminal.ConsoleCommand("selectblueprint", "[name] - Start visual blueprint selection mode", delegate(Terminal.ConsoleEventArgs args)
			{
				if (Player.m_localPlayer == null) return;
				if (args.Length < 2)
				{
					args.Context.AddString("Usage: selectblueprint <name>");
					return;
				}
				BlueprintSelector.Instance.StartSelection(args[1]);
			});

			new Terminal.ConsoleCommand("previewscan", "[radius] - Preview scan area with markers", delegate(Terminal.ConsoleEventArgs args)
			{
				if (Player.m_localPlayer == null) return;
				if (args.Length < 2 || !float.TryParse(args[1], out float radius))
				{
					args.Context.AddString("Usage: previewscan <radius>");
					return;
				}
				BlueprintScanner.Instance.PreviewScanArea(radius);
			});

			new Terminal.ConsoleCommand("listblueprints", "List all loaded blueprints", delegate(Terminal.ConsoleEventArgs args)
			{
				var blueprints = BlueprintConfig.GetAllBlueprints();
				if (blueprints.Count == 0)
				{
					args.Context.AddString("No blueprints loaded.");
					return;
				}
				foreach (var bp in blueprints)
				{
					args.Context.AddString(bp.name);
				}
			});
		}
		#endregion ConsoleCommands


		#endregion
		#region Misc


		[HarmonyPatch(typeof(Localization), "SetupLanguage")]
		public static class MyLocalizationPatch
		{
			public static void Postfix(Localization __instance, string language)
			{
				//Debug.LogWarning(language);
				BuzzLocal.init(language, __instance);
				BuzzLocal.UpdateDictinary();
			}
		}

		[HarmonyPatch(typeof(PlayerProfile), "SavePlayerToDisk")]
		public static class PlayerProfile_SavePlayerData_Patch
		{
			public static void Prefix(PlayerProfile __instance)
			{
				if (CheckPlayerNull())
				{
					return;
				}
				OdinData.saveOdinData(Player.m_localPlayer.GetPlayerName() + "_" + ZNet.instance.GetWorldName());
			}
		}

		[HarmonyPatch(typeof(PlayerProfile), "LoadPlayerData")]
		private static class Patch_PlayerProfile_LoadPlayerData
		{
			private static void Postfix()
			{
				if (ZNet.instance == null)
				{
					return;
				}
				{
					if (CheckPlayerNull() || OdinPlus.m_instance.isLoaded) { return; }
					OdinData.loadOdinData(Player.m_localPlayer.GetPlayerName() + "_" + ZNet.instance.GetWorldName());
				}

			}
		}
		[HarmonyPatch(typeof(Tameable), "GetHoverText")]
		private static class Postfix_Tameable_GetHoverText
		{
			private static string _cachedWolfHoverText;
			private static void Postfix(Tameable __instance, ref string __result)
			{
				if (!__instance.TryGetComponent<Character>(out var character)) return;
				if (character.m_name != "$op_wolf_name") return;

				if (_cachedWolfHoverText == null)
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
		private static class Prefix_Hud_TogglePieceSelection_BlueprintTool
		{
			private static bool Prefix()
			{
				if (Player.m_localPlayer == null || !Player.m_localPlayer.InPlaceMode()) return true;
				Player.m_localPlayer.GetBuildSelection(out _, out _, out _, out _, out PieceTable pieceTable);
				if (pieceTable != OdinItem.BlueprintToolPieceTable)
				{
					return true; // Not our tool - vanilla Hammer/other tools behave normally
				}
				BlueprintBrowser.Toggle();
				return false; // Skip vanilla piece-selection window entirely
			}
		}

		// Makes camera/minimap/console/menu input-gating treat our Browser exactly like vanilla's own
		// piece-selection window (see GameCamera.UpdateMouseCapture, which unlocks the cursor based on
		// this same flag) - without this, the Browser would open but the mouse would stay locked/hidden.
		[HarmonyPatch(typeof(Hud), "IsPieceSelectionVisible")]
		private static class Postfix_Hud_IsPieceSelectionVisible_BlueprintTool
		{
			private static void Postfix(ref bool __result)
			{
				if (BlueprintBrowser.IsVisible) __result = true;
			}
		}
		#endregion BlueprintTool


		#endregion
		#region ZnetScene
		[HarmonyPatch(typeof(ZNetScene), "Awake")]
		private static class ZNetScene_Awake_Prefix
		{
			private static void Prefix(ZNetScene __instance)
			{
				OdinPlus.PreZNS(__instance);

			}
		}
		[HarmonyPriority(600)]
		[HarmonyBefore(new string[] { "buzz.valheim.AllTameable", "org.bepinex.plugins.creaturelevelcontrol" })]
		[HarmonyPatch(typeof(ZNetScene), "Awake")]
		private static class ZNetScene_Awake_Patch
		{
			private static void Postfix(ZNetScene __instance)
			{
				//Pet.init(__instance);
				OdinPlus.PostZNS();
			}
		}
		[HarmonyPatch(typeof(ZNetScene), "Shutdown")]
		private static class ZNetScene_Shutdown_Patch
		{
			private static void Postfix()
			{
				if (ZNet.instance.IsDedicated() && ZNet.instance.IsServer())
				{
					OdinData.saveOdinData(ZNet.instance.GetWorldName());
				}
				OdinPlus.UnRegister();
				OdinPlus.Clear();
			}
		}
		#endregion
		#region ZoneSystem
		[HarmonyPatch(typeof(ZoneSystem), "Start")]
		private static class Postfix_ZoneSystem_Start
		{
			private static void Postfix()
			{
				// Odin's camp init (OdinPlus.PostZone) must run even if a posZone subscriber throws.
				if (posZone != null)
				{
					try
					{
						posZone();
					}
					catch (Exception e)
					{
						DBG.blogWarning("[Plugin] posZone threw, continuing to OdinPlus.PostZone: " + e);
					}
				}
				OdinPlus.PostZone();
			}
		}
		[HarmonyPatch(typeof(ZoneSystem), "Start")]
		private static class Prefix_ZoneSystem_Start
		{
			private static void Prefix()
			{
				//LocationMarker.HackLoctaions();
			}
		}
		[HarmonyPatch(typeof(DungeonGenerator), "Awake")]
		private static class Postfix_DungeonDB_Awake
		{
			private static void Postfix(DungeonGenerator __instance)
			{
				if (__instance.GetComponent<ZNetView>())
				{
					__instance.gameObject.AddComponent<LocationMarker>();
				}
			}
		}
		[HarmonyPatch(typeof(LocationProxy), "Awake")]
		private static class Postfix_LocationProxy_Awake
		{
			private static void Postfix(LocationProxy __instance)
			{
				if (__instance.GetComponentInChildren<DungeonGenerator>(true)!=null)
				{
					return;
				}
				__instance.gameObject.AddComponent<LocationMarker>();
			}
		}

		// Village-NPC-regression fix: LocationProxy is the persistent marker ZoneSystem spawns at the
		// real world position of every placed location ("WoodFarm1" villages included). Fires via
		// SetLocation() the very first time a location is ever generated, and via Awake() alone on every
		// later zone reload once the ZDO is persisted - patch both so we never miss the seed event.
		// See HumanManager.TrySeedVillage for the idempotency guard (dedup via a custom ZDO flag).
		[HarmonyPatch(typeof(LocationProxy), "Awake")]
		private static class Postfix_LocationProxy_Awake_VillageSeed
		{
			private static void Postfix(LocationProxy __instance)
			{
				HumanManager.TrySeedVillage(__instance);
			}
		}
		[HarmonyPatch(typeof(LocationProxy), "SetLocation")]
		private static class Postfix_LocationProxy_SetLocation_VillageSeed
		{
			private static void Postfix(LocationProxy __instance)
			{
				HumanManager.TrySeedVillage(__instance);
			}
		}

		#endregion ZoneSystem
		#region ODB
		[HarmonyPatch(typeof(ObjectDB), "Awake")]
		private static class Prefix_ObjectDB_Awake
		{
			private static void Prefix(ObjectDB __instance)
			{
				preODB(__instance);
			}
		}


		[HarmonyPatch(typeof(ObjectDB), "Awake")]
		private static class Patch_ObjectDB_Awake
		{
			private static void Postfix(ObjectDB __instance)
			{
				OdinPlus.PostODB();
			}
		}

		#endregion
		#region Znet
		[HarmonyPatch(typeof(ZNet), "Awake")]
		private static class Postfix_ZNet_Awake
		{
			private static void Postfix()
			{
				RegRPC();
				LocationManager.RequestServerFop();
			}
		}

		#endregion znet
		#region CreatureSpawner
		[HarmonyPatch(typeof(CreatureSpawner), "Spawn")]
		private static class Postfix_CreatureSpawner_Spawn
		{
			private static void Postfix(CreatureSpawner __instance)
			{
				var spawnerZnv = __instance.GetComponent<ZNetView>();
				if (spawnerZnv == null || spawnerZnv.GetZDO() == null) return;
				string faction = spawnerZnv.GetZDO().GetString("npc_faction", "");
				if (string.IsNullOrEmpty(faction)) return;

				float patrol = spawnerZnv.GetZDO().GetFloat("npc_patrol_radius", 0f);
				// Find the spawned creature (child or nearby with our NPC component)
				var npc = __instance.GetComponentInChildren<HumanNPC>(true);
				if (npc == null)
				{
					// CreatureSpawner spawns at its own position — search nearby
					var cols = Physics.OverlapSphere(__instance.transform.position, 2f);
					foreach (var col in cols)
					{
						npc = col.GetComponentInParent<HumanNPC>();
						if (npc != null) break;
					}
				}
				if (npc == null) return;

				npc.FactionName = faction;
				var npcZnv = npc.GetComponent<ZNetView>();
				if (npcZnv != null && npcZnv.GetZDO() != null)
					npcZnv.GetZDO().Set("npc_faction", faction);

				if (patrol > 0f)
				{
					var ai = npc.GetComponent<MonsterAI>();
					if (ai != null) ai.m_randomMoveRange = patrol;
				}
			}
		}
		#endregion CreatureSpawner
		#region container
		[HarmonyPatch(typeof(Container), "Interact")]
		private static class Postfix_Container_Interact
		{
			private static void Postfix(Container __instance, Humanoid character, bool hold)
			{
				var a = __instance.GetComponent<LegacyChest>();
				if (a)
				{
					a.OnOpen(character,hold);
				}
			}
		}
		#endregion container
		#region Charactor
		[HarmonyPatch(typeof(Character), "GetHoverText")]
		private static class Prefix_Character_GetHoverText
		{
			private static bool Prefix(Character __instance, ref string __result)
			{
				if (__instance.TryGetComponent<HumanNPC>(out var npc))
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
		private static class GameCamera_BlockZoomDuringSelection
		{
			private static void Prefix(GameCamera __instance, ref float __state)
			{
				if (BlueprintSelector.IsActive)
				{
					__state = __instance.m_zoomSens;
					__instance.m_zoomSens = 0f;
				}
			}
			private static void Postfix(GameCamera __instance, float __state)
			{
				if (BlueprintSelector.IsActive && __state > 0f)
				{
					__instance.m_zoomSens = __state;
				}
			}
		}
		#endregion BlueprintZoomBlock

		#endregion patch

		#region Tool
		public static bool CheckPlayerNull(bool log = false)
		{
			if (Player.m_localPlayer == null)
			{
				if (log) { DBG.blogWarning("Player is Null"); }

				return true;
			}
			return false;
		}


		#endregion

		#region Delegates
		#endregion Delegates
	}

}