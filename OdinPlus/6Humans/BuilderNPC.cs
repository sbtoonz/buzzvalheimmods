using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using HarmonyLib;

namespace OdinPlus
{
	public class BuilderNPC : MaterialVillager
	{
		#region Fields

		Coroutine m_buildCoroutine = null!;
		Blueprint m_currentBlueprint = null!;
		int m_buildStep = 0;
		Vector3 m_buildOrigin;
		List<GameObject> m_ghosts = new();
		Dictionary<string, int> m_resourcePool = new();
		// Last few completed piece names, for "recently placed" dialogue (Task 5 of the material-request brief).
		readonly List<string> m_recentlyPlaced = new();
		const int MaxRecentMilestones = 3;
		MonsterAI m_ai = null!;
		BuildSite m_claimedSite = null!;

		// ZDO keys - persisting build state means an in-progress structure survives save/load and
		// server restarts instead of silently losing all progress and donated resources (previously
		// these were plain MonoBehaviour fields, never written to the NPC's ZDO at all).
		const string ZK_BuildName = "OP_BuildName";
		const string ZK_BuildStep = "OP_BuildStep";
		const string ZK_BuildOrigin = "OP_BuildOrigin";
		// Whole resource pool serialized as one "Key:Amount;Key2:Amount2" string, since the set of
		// resources is now dynamic (whatever the assigned blueprint's real Bill of Materials needs),
		// not a fixed Wood/Stone pair.
		const string ZK_ResourcePool = "OP_ResourcePool";

		static readonly AccessTools.FieldRef<BaseAI, Vector3> s_patrolPointRef =
			AccessTools.FieldRefAccess<BaseAI, Vector3>("m_patrolPoint");
		static readonly AccessTools.FieldRef<BaseAI, bool> s_patrolRef =
			AccessTools.FieldRefAccess<BaseAI, bool>("m_patrol");

		#endregion Fields

		#region Lifecycle

		protected override void Awake()
		{
			base.Awake();
			ChoiceList = new string[3] { "$op_talk", "What do you need?", "Status" };
			if(m_hum != null) m_hum.m_faction = Character.Faction.Players;
			SetupGatherAI();

			var zdo = m_nview.GetZDO();
			if(zdo == null) return;

			RestoreResourcePool(zdo);

			var savedName = zdo.GetString(ZK_BuildName, "");
			if(string.IsNullOrEmpty(savedName))
			{
				// Not resuming anything - proactively check in case this NPC already has enough
				// resources to start building (e.g. a fresh spawn given a preset pool, or resources
				// that arrived through some path other than a donation/harvest event). Previously this
				// only ever got checked reactively from UseItem/HarvestTarget, so a NPC sitting on
				// enough resources with no new donation would just stay idle forever.
				CheckForBuildableStructures();
				return;
			}

			var bp = Blueprints.All.Find(b => b.name == savedName);
			if(bp == null)
			{
				DBG.blogWarning($"BuilderNPC: Saved in-progress blueprint '{savedName}' no longer exists, abandoning resume");
				zdo.Set(ZK_BuildName, "");
				return;
			}

			m_currentBlueprint = bp;
			m_buildStep = zdo.GetInt(ZK_BuildStep, 0);
			m_buildOrigin = zdo.GetVec3(ZK_BuildOrigin, transform.position);
			DBG.blogInfo($"BuilderNPC: Resuming '{savedName}' at step {m_buildStep}/{bp.pieces.Length} after reload");
			SpawnGhostsFrom(m_buildStep);
			m_buildCoroutine = StartCoroutine(BuildCoroutine());
		}

		void OnDestroy()
		{
			// Stop build coroutine to prevent memory leak
			if(m_buildCoroutine != null)
			{
				StopCoroutine(m_buildCoroutine);
				m_buildCoroutine = null;
			}
			ClearGhosts();
		}

		#endregion Lifecycle

		#region Interaction

		public override void Choice0() => Say("Greetings! I'm a builder. Give me wood and stone, and I'll construct structures for you!");

		public new void Choice1()
		{
			if(m_buildCoroutine != null)
			{
				var percent = m_currentBlueprint.pieces.Length > 0 ? m_buildStep * 100 / m_currentBlueprint.pieces.Length : 100;
				var msg = $"Building {m_currentBlueprint.name} - {percent}% ({m_buildStep}/{m_currentBlueprint.pieces.Length})";
				// If waiting on resources, tell the player what's needed
				var pieceCount = Mathf.Max(1, m_currentBlueprint.pieces.Length);
				if(!CanAffordPieceShare(m_buildStep, pieceCount))
					msg += $"\n<color=yellow>Waiting for resources!</color>\n{FormatRemaining(m_currentBlueprint)}";
				Say(msg);
				return;
			}

			// Check for unclaimed sites assigned to our faction
			var site = BuildSiteManager.GetUnclaimedSite(transform.position, 50f, FactionName);
			if(site != null)
			{
				Say($"I see a build site for {site.blueprint.name}!\nI need:\n{FormatRemaining(site.blueprint)}");
				return;
			}

			var target = GetNextTarget();
			if(target != null)
				Say($"We still need:\n{FormatRemaining(target)}");
			else
				Say("Place a blueprint nearby and bring me materials!");
		}

		public void Choice2()
		{
			var status = $"Resources on hand:\n{FormatPool(m_resourcePool)}";
			if(m_buildCoroutine != null)
			{
				var percent = m_currentBlueprint.pieces.Length > 0 ? m_buildStep * 100 / m_currentBlueprint.pieces.Length : 100;
				status += $"\n\nBuilding: {m_currentBlueprint.name} - {percent}% complete ({m_buildStep}/{m_currentBlueprint.pieces.Length})";
				if(m_recentlyPlaced.Count > 0) status += $"\nRecently placed: {string.Join(", ", m_recentlyPlaced)}";
			}
			else
			{
				var target = GetNextTarget();
				if(target != null) status += $"\n\nStill need:\n{FormatRemaining(target)}";
			}
			Say(status);
		}

		public override bool UseItem(Humanoid user, ItemDrop.ItemData item)
		{
			// Accept whatever resource any eligible blueprint's real Bill of Materials actually needs -
			// no hardcoded Wood/Stone check. Donating still earns Odin bucks (the "mission/quest" path),
			// unlike resources the NPC gathers for itself via SelfGather, below.
			var key = item.m_dropPrefab != null ? item.m_dropPrefab.name : null;
			if(!string.IsNullOrEmpty(key) && IsNeededResource(key))
			{
				var count = Mathf.Min(item.m_stack, 50);
				user.GetInventory().RemoveItem(item.m_shared.m_name, count);
				m_resourcePool[key] = GetOrZero(m_resourcePool, key) + count;
				PersistResourcePool();
				OdinData.AddCredits(Mathf.Max(1, count / 5), true);

				var target = GetNextTarget();
				var thanks = $"Thanks! I have {m_resourcePool[key]} {key} now.";
				if(target != null) thanks += $"\nStill need:\n{FormatRemaining(target)}";
				Say(thanks);

				CheckForBuildableStructures();
				return true;
			}
			return base.UseItem(user, item);
		}

		public override string GetHoverText()
		{
			if(m_hum.m_faction != Character.Faction.Players) return "";

			var text = $"<color=#ADD8E6FF>{m_name} (Builder)</color>\n";

			if(m_buildCoroutine != null)
			{
				var percent = m_currentBlueprint.pieces.Length > 0 ? m_buildStep * 100 / m_currentBlueprint.pieces.Length : 100;
				text += $"<color=yellow>Building {m_currentBlueprint.name} - {percent}%</color>\n";
				text += FormatMaterialStatus(m_currentBlueprint);
			}
			else
			{
				var target = GetNextTarget();
				if(target != null)
				{
					text += $"Next: {target.name}\n";
					text += FormatMaterialStatus(target);
				}
				else
					text += "No blueprint assigned.\n";
			}

			text += "\n[<color=yellow><b>1-8</b></color>] Give materials";
			text += "\n[<color=yellow><b>$KEY_Use</b></color>] Talk";
			return Localization.instance.Localize(text);
		}

		#endregion Interaction

		#region Resources

		public void ReceiveMaterials(string key, int amount)
		{
			m_resourcePool[key] = GetOrZero(m_resourcePool, key) + amount;
			PersistResourcePool();
			CheckForBuildableStructures();
		}

		public bool IsNeededResourcePublic(string key) => IsNeededResource(key);

		public string FormatRemainingPublic()
		{
			var target = GetNextTarget();
			return target != null ? FormatRemaining(target) : "";
		}

		public Blueprint GetNextTarget() => GetEligibleBlueprints().FirstOrDefault(bp => !CanAfford(bp));

		// A resource is "needed" if it appears in the cost list of any faction-eligible blueprint - keeps
		// the NPC from hoarding random donated items while still accepting anything a real assigned
		// blueprint requires, vanilla or modded, without a hardcoded item-name whitelist.
		bool IsNeededResource(string key)
		{
			foreach(var bp in GetEligibleBlueprints())
				if(bp.resourceCosts.ContainsKey(key)) return true;
			return false;
		}

		bool CanAfford(Blueprint bp)
		{
			foreach(var cost in bp.resourceCosts)
				if(GetOrZero(m_resourcePool, cost.Key) < cost.Value) return false;
			return true;
		}

		bool CanAffordPieceShare(int stepIndex, int pieceCount)
		{
			foreach(var cost in m_currentBlueprint.resourceCosts)
			{
				var totalForResource = cost.Value;
				var baseShare = totalForResource / pieceCount;
				var remainder = totalForResource % pieceCount;
				var share = baseShare + (stepIndex == pieceCount - 1 ? remainder : 0);
				if(GetOrZero(m_resourcePool, cost.Key) < share) return false;
			}
			return true;
		}

		void DeductPieceShare(int stepIndex, int pieceCount)
		{
			foreach(var cost in m_currentBlueprint.resourceCosts)
			{
				var totalForResource = cost.Value;
				var baseShare = totalForResource / pieceCount;
				var remainder = totalForResource % pieceCount;
				// Give the last piece any leftover from integer division so the full cost is always
				// consumed exactly once, not under/over by rounding.
				var share = baseShare + (stepIndex == pieceCount - 1 ? remainder : 0);
				m_resourcePool[cost.Key] = Mathf.Max(0, GetOrZero(m_resourcePool, cost.Key) - share);
			}
			PersistResourcePool();
		}

		void PersistResourcePool()
		{
			var zdo = m_nview.GetZDO();
			if(zdo == null) return;
			zdo.Set(ZK_ResourcePool, string.Join(";", m_resourcePool.Select(kv => $"{kv.Key}:{kv.Value}")));
		}

		void RestoreResourcePool(ZDO zdo)
		{
			var saved = zdo.GetString(ZK_ResourcePool, "");
			if(string.IsNullOrEmpty(saved)) return;
			foreach(var entry in saved.Split(';'))
			{
				var parts = entry.Split(':');
				if(parts.Length == 2 && int.TryParse(parts[1], out var amount))
					m_resourcePool[parts[0]] = amount;
			}
		}

		// Plain Dictionary<TKey,TValue>.GetValueOrDefault is ambiguous in this project - a ZDOHelper
		// extension method of the same name (different generic shape) is also in scope, so use this instead.
		static int GetOrZero(Dictionary<string, int> pool, string key) =>
			pool.TryGetValue(key, out var value) ? value : 0;

		static string FormatPool(Dictionary<string, int> pool) =>
			pool.Count == 0 ? "(nothing yet)" : string.Join("\n", pool.Select(kv => $"{kv.Key}: {kv.Value}"));

		string FormatRemaining(Blueprint bp)
		{
			var sb = new System.Text.StringBuilder();
			foreach(var cost in bp.resourceCosts)
			{
				var remaining = Mathf.Max(0, cost.Value - GetOrZero(m_resourcePool, cost.Key));
				if(remaining > 0) sb.AppendLine($" - {remaining} {cost.Key}");
			}
			return sb.ToString();
		}

		string FormatMaterialStatus(Blueprint bp)
		{
			var sb = new System.Text.StringBuilder();
			foreach(var cost in bp.resourceCosts)
			{
				var have = GetOrZero(m_resourcePool, cost.Key);
				var color = have >= cost.Value ? "green" : "yellow";
				sb.AppendLine($"  <color={color}>{cost.Key}: {have}/{cost.Value}</color>");
			}
			return sb.ToString();
		}

		#endregion Resources

		#region SelfGather

		// NPCs can path to nearby trees/rock formations and harvest wood/stone themselves so they're
		// never permanently stuck waiting on a player - but donating materials directly is still the
		// only way for a *player* to earn Odin bucks (see UseItem), keeping the donation mission/quest
		// meaningful rather than making self-gathering a strictly-better replacement for it.

		void SetupGatherAI()
		{
			m_ai = GetComponent<MonsterAI>();
			if(m_ai == null) return; // No MonsterAI on this prefab - self-gathering won't work but UseItem donations still do
			// Keep AI disabled by default - only enable briefly for BuildSiteCoroutine pathing
			m_ai.enabled = false;
		}

		// ponytail: self-gather disabled - per-frame reflection MoveToward on disabled AI caused frame drops
		// and never actually moved the NPC. Donations via UseItem are the working resource path.

		#endregion SelfGather

		#region Building

		void CheckForBuildableStructures()
		{
			if(m_buildCoroutine != null) return;

			// Only build from player-placed or NPC-created build sites
			var site = BuildSiteManager.GetUnclaimedSite(transform.position, 50f, FactionName);
			if(site != null && CanAfford(site.blueprint))
				ClaimAndBuildSite(site);
		}

		void ClaimAndBuildSite(BuildSite site)
		{
			site.claimedBy = this;
			m_claimedSite = site;
			m_currentBlueprint = site.blueprint;
			m_buildStep = 0;
			m_buildOrigin = site.origin;

			var zdo = m_nview.GetZDO();
			if(zdo != null)
			{
				zdo.Set(ZK_BuildName, site.blueprint.name);
				zdo.Set(ZK_BuildStep, 0);
				zdo.Set(ZK_BuildOrigin, site.origin);
			}

			Say($"I'll build that {site.blueprint.name}!");
			m_buildCoroutine = StartCoroutine(BuildSiteCoroutine());
		}

		// Faction YAML can assign specific blueprints to a faction (FactionDef.AssignedBlueprints); if it
		// does, only those are considered for this NPC - the faction config becomes the single place that
		// decides *which* blueprints apply, while the blueprint YAML remains the only place resource costs
		// live (no duplicated cost lists). Falls back to the older per-blueprint AllowedFactions check for
		// factions/blueprints that don't use assignment yet.
		IEnumerable<Blueprint> GetEligibleBlueprints()
		{
			if(FactionManager.Factions.TryGetValue(FactionName, out var faction) &&
				faction.AssignedBlueprints != null && faction.AssignedBlueprints.Count > 0)
			{
				foreach(var name in faction.AssignedBlueprints)
				{
					var bp = Blueprints.All.FirstOrDefault(b => b.name == name);
					if(bp != null) yield return bp;
				}
				yield break;
			}

			foreach(var bp in Blueprints.All)
				if(IsAllowedForFaction(bp)) yield return bp;
		}

		// A blueprint with no AllowedFactions entries is buildable by any faction (backwards compatible
		// default). Non-empty means only listed factions (matched against this NPC's FactionName) qualify.
		bool IsAllowedForFaction(Blueprint bp) =>
			bp.allowedFactions == null || bp.allowedFactions.Count == 0 || bp.allowedFactions.Contains(FactionName);

		void SpawnGhostsFrom(int startStep)
		{
			ClearGhosts();
			if(m_currentBlueprint == null) return;
			for(var i = startStep; i < m_currentBlueprint.pieces.Length; i++)
			{
				var piece = m_currentBlueprint.pieces[i];
				var prefab = ZNetScene.instance.GetPrefab(piece.prefabName);
				if(prefab == null) continue;
				var ghost = BlueprintGhost.Create(prefab, m_buildOrigin + piece.localPosition, Quaternion.Euler(piece.rotation), null);
				if(ghost != null) m_ghosts.Add(ghost);
			}
		}

		void ClearGhosts()
		{
			foreach(var ghost in m_ghosts)
				if(ghost != null) Destroy(ghost);
			m_ghosts.Clear();
		}

		IEnumerator BuildSiteCoroutine()
		{
			Say($"Starting construction of {m_currentBlueprint.name}!");

			// Walk to the build site before starting
			if(m_ai != null)
			{
				// Set patrol point to build origin and enable AI so it paths there
				s_patrolPointRef(m_ai) = m_buildOrigin;
				s_patrolRef(m_ai) = true;
				m_ai.enabled = true;

				var timeout = 30f;
				var dist = Vector3.Distance(transform.position, m_buildOrigin);
				while(dist > 3f && timeout > 0f)
				{
					yield return new WaitForSeconds(0.5f);
					timeout -= 0.5f;
					dist = Vector3.Distance(transform.position, m_buildOrigin);
				}

				m_ai.enabled = false;
			}

			yield return new WaitForSeconds(1f);

			var pieceCount = Mathf.Max(1, m_currentBlueprint.pieces.Length);

			while(m_buildStep < m_currentBlueprint.pieces.Length)
			{
				// Wait for resources - don't build until we can afford the next piece
				while(!CanAffordPieceShare(m_buildStep, pieceCount))
					yield return new WaitForSeconds(2f);

				var piece = m_currentBlueprint.pieces[m_buildStep];
				DeductPieceShare(m_buildStep, pieceCount);

				// Replace the ghost from the site with the real piece
				if(m_claimedSite != null && m_buildStep < m_claimedSite.ghosts.Count)
				{
					var ghost = m_claimedSite.ghosts[m_buildStep];
					if(ghost != null) Destroy(ghost);
					m_claimedSite.ghosts[m_buildStep] = null;
				}
				PlacePieceRotated(piece);
				RecordMilestone(piece.prefabName);

				m_buildStep++;
				var zdo = m_nview.GetZDO();
				zdo?.Set(ZK_BuildStep, m_buildStep);

				yield return new WaitForSeconds(3f);
			}

			Say($"{m_currentBlueprint.name} is complete!");
			var doneZdo = m_nview.GetZDO();
			doneZdo?.Set(ZK_BuildName, "");

			if(m_claimedSite != null)
			{
				BuildSiteManager.RemoveSite(m_claimedSite);
				m_claimedSite = null;
			}

			m_buildCoroutine = null;
			m_currentBlueprint = null;
			m_buildStep = 0;
			m_recentlyPlaced.Clear();
		}

		IEnumerator BuildCoroutine()
		{
			Say($"Starting construction of {m_currentBlueprint.name} - watch the blueprint come to life!");
			yield return new WaitForSeconds(1f);

			// Split the blueprint's total resource cost evenly across its pieces so each completed
			// piece consumes its own share incrementally, instead of the whole structure's cost being
			// deducted in one lump sum before any hologram exists.
			var pieceCount = Mathf.Max(1, m_currentBlueprint.pieces.Length);

			// Build each piece
			while(m_buildStep < m_currentBlueprint.pieces.Length)
			{
				var piece = m_currentBlueprint.pieces[m_buildStep];

				DeductPieceShare(m_buildStep, pieceCount);
				ReplaceGhostWithPiece(m_buildStep, piece);
				RecordMilestone(piece.prefabName);

				m_buildStep++;
				var zdo = m_nview.GetZDO();
				zdo?.Set(ZK_BuildStep, m_buildStep);

				yield return new WaitForSeconds(3f);
			}

			Say($"{m_currentBlueprint.name} is complete!");
			var doneZdo = m_nview.GetZDO();
			doneZdo?.Set(ZK_BuildName, "");
			m_buildCoroutine = null;
			m_currentBlueprint = null;
			m_buildStep = 0;
			m_recentlyPlaced.Clear();
		}

		void PlacePieceRotated(BlueprintPiece piece)
		{
			var prefab = ZNetScene.instance.GetPrefab(piece.prefabName);
			if(prefab == null) return;
			var siteRot = m_claimedSite != null ? m_claimedSite.rotation : Quaternion.identity;
			var worldPos = m_buildOrigin + siteRot * piece.localPosition;
			var worldRot = siteRot * Quaternion.Euler(piece.rotation);
			var go = Instantiate(prefab, worldPos, worldRot);
			go.SetActive(true);
		}

		void PlacePiece(BlueprintPiece piece)
		{
			var prefab = ZNetScene.instance.GetPrefab(piece.prefabName);
			if(prefab == null)
			{
				DBG.blogWarning($"BuilderNPC: Prefab '{piece.prefabName}' not found");
				return;
			}

			var worldPos = m_buildOrigin + piece.localPosition;
			var worldRot = Quaternion.Euler(piece.rotation);

			var go = Instantiate(prefab, worldPos, worldRot);
			go.SetActive(true);

			DBG.blogInfo($"BuilderNPC: Placed {piece.prefabName} at {worldPos}");
		}

		void ReplaceGhostWithPiece(int stepIndex, BlueprintPiece piece)
		{
			if(stepIndex < m_ghosts.Count && m_ghosts[stepIndex] != null)
			{
				Destroy(m_ghosts[stepIndex]);
				m_ghosts[stepIndex] = null;
			}
			PlacePiece(piece);
		}

		void RecordMilestone(string prefabName)
		{
			m_recentlyPlaced.Add(prefabName);
			while(m_recentlyPlaced.Count > MaxRecentMilestones) m_recentlyPlaced.RemoveAt(0);
		}

		#endregion Building
	}

	public struct BlueprintPiece
	{
		public string prefabName;
		public Vector3 localPosition;
		public Vector3 rotation;

		public BlueprintPiece(string name, Vector3 pos, Vector3 rot)
		{
			prefabName = name;
			localPosition = pos;
			rotation = rot;
		}
	}

	public class Blueprint
	{
		public string name;
		public BlueprintPiece[] pieces;
		public Dictionary<string, int> resourceCosts;
		// Optional: which HumanNPC.FactionName values may build this blueprint. Null/empty = any faction.
		public List<string> allowedFactions;

		public Blueprint(string n, Dictionary<string, int> costs, BlueprintPiece[] p, List<string> factions = null)
		{
			name = n;
			resourceCosts = costs;
			pieces = p;
			allowedFactions = factions;
		}

		internal static List<Blueprint> All = new();
	}

	public static class Blueprints
	{
		internal static List<Blueprint> All = new();

		public static void Init()
		{
			// =====================================================
			// EXAMPLE BLUEPRINTS - REPLACE WITH UNITY EXPORTS!
			//
			// To create proper blueprints:
			// 1. Open Valheim Unity project
			// 2. Build your structure using piece prefabs (wood_floor_1x1, wood_wall_roof, etc.)
			// 3. Select all pieces in the hierarchy
			// 4. Tools -> Export Blueprint
			// 5. Paste the generated code here
			//
			// Available pieces from Unity dump:
			// - Floors: wood_floor_1x1, wood_floor, stone_floor_2x2
			// - Walls: wood_wall_roof, wood_wall_half, wood_wall_quarter, stone_wall_1x1, stone_wall_2x1
			// - Roofs: wood_roof, wood_roof_45, wood_roof_top, wood_roof_top_45
			// - Doors: wood_door, wood_gate
			// - Misc: wood_fence, wood_pole, wood_beam, piece_workbench, fire_pit
			// =====================================================

			// Example 1: Simple 2x2 floor platform
			All.Add(new Blueprint(
				"Floor Platform",
				new Dictionary<string, int> { { "Wood", 10 } },
				new BlueprintPiece[]
				{
					new("wood_floor_1x1", new Vector3(0f, 0f, 0f), Vector3.zero),
					new("wood_floor_1x1", new Vector3(2f, 0f, 0f), Vector3.zero),
					new("wood_floor_1x1", new Vector3(0f, 0f, 2f), Vector3.zero),
					new("wood_floor_1x1", new Vector3(2f, 0f, 2f), Vector3.zero),
				}
			));

			// Example 2: Stone hearth area
			All.Add(new Blueprint(
				"Stone Hearth",
				new Dictionary<string, int> { { "Stone", 15 } },
				new BlueprintPiece[]
				{
					new("stone_floor_2x2", new Vector3(0f, 0f, 0f), Vector3.zero),
					new("fire_pit", new Vector3(0f, 0f, 0f), Vector3.zero),
				}
			));

			// Example 3: Simple fence segment
			All.Add(new Blueprint(
				"Fence Segment",
				new Dictionary<string, int> { { "Wood", 15 } },
				new BlueprintPiece[]
				{
					new("wood_fence", new Vector3(0f, 0f, 0f), Vector3.zero),
					new("wood_fence", new Vector3(2f, 0f, 0f), Vector3.zero),
					new("wood_fence", new Vector3(4f, 0f, 0f), Vector3.zero),
					new("wood_fence", new Vector3(6f, 0f, 0f), Vector3.zero),
				}
			));

			// PASTE YOUR UNITY-EXPORTED BLUEPRINTS BELOW THIS LINE
			// =====================================================

			// Example from export (replace this with your actual exports):
			/*
			All.Add(new Blueprint(
				"Viking Hut",
				new Dictionary<string, int> { { "Wood", 50 } },
				new BlueprintPiece[]
				{
					// Floor
					new("wood_floor_1x1", new Vector3(0f, 0f, 0f), Vector3.zero),
					new("wood_floor_1x1", new Vector3(2f, 0f, 0f), Vector3.zero),
					// ... (rest of pieces from exporter)
				}
			));
			*/

			DBG.blogInfo($"Blueprints initialized: {All.Count} structures available");
		}
	}
}
