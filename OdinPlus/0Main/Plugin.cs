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
		public static ConfigEntry<int> nexusID;
		public static ManualLogSource logger;
		public static ConfigEntry<KeyboardShortcut> KS_SecondInteractkey;
		public static ConfigEntry<string> CFG_ItemSellValue;
		public static ConfigEntry<Vector3> CFG_OdinPosition;
		public static ConfigEntry<bool> CFG_ForceOdinPosition;
		public static bool Set_FOP = false;
		#region InternalConfig
		public static int RaiseCost = 10;
		public static int RaiseFactor = 100;

		#endregion InternalConfig
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
			CFG_ItemSellValue = base.Config.Bind<string>("Config", "ItemSellValue", "TrophyBlob:20;TrophyBoar:5;TrophyBonemass:50;TrophyDeathsquito:20;TrophyDeer:5;TrophyDragonQueen:50;TrophyDraugr:20;TrophyDraugrElite:30;TrophyDraugrFem:20;TrophyEikthyr:50;TrophyFenring:30;TrophyForestTroll:30;TrophyFrostTroll:20;TrophyGoblin:20;TrophyGoblinBrute:30;TrophyGoblinKing:50;TrophyGoblinShaman:20;TrophyGreydwarf:5;TrophyGreydwarfBrute:15;TrophyGreydwarfShaman:15;TrophyHatchling:20;TrophyLeech:15;TrophyLox:20;TrophyNeck:5;TrophySerpent:30;TrophySGolem:30;TrophySkeleton:10;TrophySkeletonPoison:30;TrophySurtling:20;TrophyTheElder:50;TrophyWolf:20;TrophyWraith:30;AncientSeed:5;BoneFragments:1;Chitin:5;WitheredBone:10;DragonEgg:40;GoblinTotem:20;OdinLegacy:20");
			Plugin.nexusID = base.Config.Bind<int>("General", "NexusID", 798, "Nexus mod ID for updates");
			KS_SecondInteractkey = base.Config.Bind<KeyboardShortcut>("1Hotkeys", "Second Interact key", new KeyboardShortcut(KeyCode.E, KeyCode.LeftAlt));
			CFG_OdinPosition = base.Config.Bind<Vector3>("2Server set only", "Odin position", Vector3.zero);
			CFG_ForceOdinPosition = base.Config.Bind<bool>("2Server set only", "Force Odin Position", false);

			RegRPC = (Action)ReigsterRpc;

			_harmony = Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);

			// RegisterConsoleCommands() only writes to Terminal's static commands dict, safe to call before Terminal exists
			RegisterConsoleCommands();

			//-- init here
			OdinPlusRoot = new GameObject("OdinPlus");
			OdinPlusRoot.AddComponent<ResourceAssetManager>();
			OdinPlusRoot.AddComponent<OdinPlus>();

			//notice Debug
			OdinPlusRoot.AddComponent<DevTool>();

			DontDestroyOnLoad(OdinPlusRoot);
			DBG.blogInfo("OdinPlus Loadded");
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

				// Skip all OdinPlus interactions during blueprint selection
				// (attacks are already blocked by not having weapons equipped in place mode)
				if (BlueprintSelector.IsActive) return;

				if (KS_SecondInteractkey.Value.IsDown() && __instance.GetHoverObject() != null)
				{
					if (__instance.GetHoverObject().GetComponent<OdinInteractable>() != null)
					{
						__instance.GetHoverObject().GetComponent<OdinInteractable>().SecondaryInteract(__instance);
						return;
					}
					if (__instance.GetHoverObject().GetComponentInParent<OdinInteractable>() != null)
					{
						__instance.GetHoverObject().GetComponentInParent<OdinInteractable>().SecondaryInteract(__instance);
						return;
					}
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
				CFG_OdinPosition.Value = NpcManager.Root.transform.localPosition;
				args.Context.AddString("Odin position saved");
			});

			new Terminal.ConsoleCommand("findfarm", "Reveal nearest WoodFarm1 location on map", delegate(Terminal.ConsoleEventArgs args)
			{
				if (Player.m_localPlayer == null) return;
				Game.instance.DiscoverClosestLocation("WoodFarm1", Player.m_localPlayer.transform.position, "Village", 0);
				args.Context.AddString("Searching for nearest farm...");
			});

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
			private static void Postfix(Tameable __instance, ref string __result)
			{
				if (__instance.gameObject.GetComponent<Character>().m_name == "$op_wolf_name")
				{
					__result += Localization.instance.Localize(String.Format("\n<color=yellow><b>[{0}]</b></color>$op_wolf_use", Plugin.KS_SecondInteractkey.Value.MainKey.ToString()));
				}
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
				Component comp = __instance.GetComponent<HumanNPC>();
				if (comp)
				{
					__result = ((HumanNPC)comp).GetHoverText();
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