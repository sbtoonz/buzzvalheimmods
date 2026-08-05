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
		private Coroutine m_buildCoroutine;
		private Blueprint m_currentBlueprint;
		private int m_buildStep = 0;
		private Vector3 m_buildOrigin;
		private List<GameObject> m_ghosts = new List<GameObject>();

		private Dictionary<string, int> m_resourcePool = new Dictionary<string, int>();
		// Last few completed piece names, for "recently placed" dialogue (Task 5 of the material-request brief).
		private readonly List<string> m_recentlyPlaced = new List<string>();
		private const int MaxRecentMilestones = 3;

		// How much of each resource the NPC tries to keep in reserve by gathering on its own. Donations
		// from players are still the fast path and are the only thing that earns Odin bucks (see UseItem) -
		// self-gathering just means the NPC is never permanently stuck waiting on a player who isn't around.
		private const int GatherTargetAmount = 40;

		// Self-gathering state - reuses MonsterAI purely for its pathing (MoveTo), matching the same
		// AddComponentcc<MonsterAI> convention HumanManager already uses for other humanoid NPCs.
		private enum GatherState { Idle, MovingToResource, Harvesting, Returning }
		private MonsterAI m_ai;
		private GatherState m_gatherState = GatherState.Idle;
		private Component m_gatherTarget;
		private string m_gatherResourceKey;
		private Vector3 m_gatherTargetPos;
		private Vector3 m_homePos;
		private float m_harvestTimer;
		private float m_nextGatherScanTime;
		private const float GatherScanInterval = 5f;
		private const float GatherRadius = 20f;
		private const float GatherArriveDist = 2.5f;
		private const float HarvestDuration = 3f;
		private const int HarvestYield = 10;
		// Shared, non-allocating buffer for the periodic resource scan (avoids a GC alloc every 5s per NPC).
		// 64 is generous for a 20m-radius overlap in a normal Valheim forest/rock cluster.
		private static readonly Collider[] s_gatherHitsBuffer = new Collider[64];

		// Cached reflection for BaseAI.MoveTo — called per-frame during gather, avoid Traverse overhead
		private static readonly MethodInfo s_moveToMethod = AccessTools.Method(typeof(BaseAI), "MoveTo");
		private static readonly object[] s_moveToArgs = new object[4];

		// ZDO keys - persisting build state means an in-progress structure survives save/load and
		// server restarts instead of silently losing all progress and donated resources (previously
		// these were plain MonoBehaviour fields, never written to the NPC's ZDO at all).
		private const string ZK_BuildName = "OP_BuildName";
		private const string ZK_BuildStep = "OP_BuildStep";
		private const string ZK_BuildOrigin = "OP_BuildOrigin";
		// Whole resource pool serialized as one "Key:Amount;Key2:Amount2" string, since the set of
		// resources is now dynamic (whatever the assigned blueprint's real Bill of Materials needs),
		// not a fixed Wood/Stone pair.
		private const string ZK_ResourcePool = "OP_ResourcePool";

		protected override void Awake()
		{
			base.Awake();
			ChoiceList = new string[3] { "$op_talk", "What do you need?", "Status" };
			// Don't set build origin here - calculate it when starting to build
			m_homePos = transform.position;
			SetupGatherAI();
			// Stagger the first scan per instance so multiple BuilderNPCs don't all run their
			// Physics.OverlapSphere scan on the same tick.
			m_nextGatherScanTime = Time.time + UnityEngine.Random.Range(0f, GatherScanInterval);

			var zdo = m_nview.GetZDO();
			if (zdo == null) return;

			RestoreResourcePool(zdo);

			string savedName = zdo.GetString(ZK_BuildName, "");
			if (string.IsNullOrEmpty(savedName))
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
			if (bp == null)
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

		private void OnDestroy()
		{
			// Stop build coroutine to prevent memory leak
			if (m_buildCoroutine != null)
			{
				StopCoroutine(m_buildCoroutine);
				m_buildCoroutine = null;
			}
			ClearGhosts();
		}

		public override void Choice0()
		{
			Say("Greetings! I'm a builder. Give me wood and stone, and I'll construct structures for you!");
		}

		public new void Choice1()
		{
			if (m_buildCoroutine != null)
			{
				Say($"I'm currently building {m_currentBlueprint.name}. Progress: {m_buildStep}/{m_currentBlueprint.pieces.Length}");
				return;
			}

			var target = GetNextTarget();
			if (target != null)
			{
				Say("We still need:\n" + FormatRemaining(target));
			}
			else
			{
				Say("Bring me materials and I'll build something!");
			}
		}

		public void Choice2()
		{
			string status = "Resources on hand:\n" + FormatPool(m_resourcePool);
			if (m_buildCoroutine != null)
			{
				int percent = m_currentBlueprint.pieces.Length > 0 ? m_buildStep * 100 / m_currentBlueprint.pieces.Length : 100;
				status += $"\n\nBuilding: {m_currentBlueprint.name} - {percent}% complete ({m_buildStep}/{m_currentBlueprint.pieces.Length})";
				if (m_recentlyPlaced.Count > 0) status += $"\nRecently placed: {string.Join(", ", m_recentlyPlaced)}";
			}
			else
			{
				var target = GetNextTarget();
				if (target != null) status += "\n\nStill need:\n" + FormatRemaining(target);
			}
			Say(status);
		}

		public override bool UseItem(Humanoid user, ItemDrop.ItemData item)
		{
			// Accept whatever resource any eligible blueprint's real Bill of Materials actually needs -
			// no hardcoded Wood/Stone check. Donating still earns Odin bucks (the "mission/quest" path),
			// unlike resources the NPC gathers for itself via SelfGather, below.
			string key = item.m_dropPrefab != null ? item.m_dropPrefab.name : null;
			if (!string.IsNullOrEmpty(key) && IsNeededResource(key))
			{
				int count = Mathf.Min(item.m_stack, 50);
				user.GetInventory().RemoveItem(item.m_shared.m_name, count);
				m_resourcePool[key] = GetOrZero(m_resourcePool, key) + count;
				PersistResourcePool();
				OdinData.AddCredits(Mathf.Max(1, count / 5), true);

				var target = GetNextTarget();
				string thanks = $"Thanks! I have {m_resourcePool[key]} {key} now.";
				if (target != null) thanks += "\nStill need:\n" + FormatRemaining(target);
				Say(thanks);

				CheckForBuildableStructures();
				return true;
			}
			return base.UseItem(user, item);
		}

		// A resource is "needed" if it appears in the cost list of any faction-eligible blueprint - keeps
		// the NPC from hoarding random donated items while still accepting anything a real assigned
		// blueprint requires, vanilla or modded, without a hardcoded item-name whitelist.
		private bool IsNeededResource(string key)
		{
			foreach (var bp in GetEligibleBlueprints())
			{
				if (bp.resourceCosts.ContainsKey(key)) return true;
			}
			return false;
		}

		// The next eligible blueprint this NPC can't yet afford - used to drive "still need" dialogue.
		private Blueprint GetNextTarget()
		{
			return GetEligibleBlueprints().FirstOrDefault(bp => !CanAfford(bp));
		}

		private string FormatRemaining(Blueprint bp)
		{
			var sb = new System.Text.StringBuilder();
			foreach (var cost in bp.resourceCosts)
			{
				int remaining = Mathf.Max(0, cost.Value - GetOrZero(m_resourcePool, cost.Key));
				if (remaining > 0) sb.AppendLine($" - {remaining} {cost.Key}");
			}
			return sb.ToString();
		}

		private static string FormatPool(Dictionary<string, int> pool)
		{
			if (pool.Count == 0) return "(nothing yet)";
			return string.Join("\n", pool.Select(kv => $"{kv.Key}: {kv.Value}"));
		}

		// Plain Dictionary<TKey,TValue>.GetValueOrDefault is ambiguous in this project - a ZDOHelper
		// extension method of the same name (different generic shape) is also in scope, so use this instead.
		private static int GetOrZero(Dictionary<string, int> pool, string key)
		{
			return pool.TryGetValue(key, out int value) ? value : 0;
		}

		private void PersistResourcePool()
		{
			var zdo = m_nview.GetZDO();
			if (zdo == null) return;
			zdo.Set(ZK_ResourcePool, string.Join(";", m_resourcePool.Select(kv => kv.Key + ":" + kv.Value)));
		}

		private void RestoreResourcePool(ZDO zdo)
		{
			string saved = zdo.GetString(ZK_ResourcePool, "");
			if (string.IsNullOrEmpty(saved)) return;
			foreach (var entry in saved.Split(';'))
			{
				var parts = entry.Split(':');
				if (parts.Length == 2 && int.TryParse(parts[1], out int amount))
					m_resourcePool[parts[0]] = amount;
			}
		}

		#region SelfGather
		// NPCs can path to nearby trees/rock formations and harvest wood/stone themselves so they're
		// never permanently stuck waiting on a player - but donating materials directly is still the
		// only way for a *player* to earn Odin bucks (see UseItem), keeping the donation mission/quest
		// meaningful rather than making self-gathering a strictly-better replacement for it.

		private void SetupGatherAI()
		{
			m_ai = GetComponent<MonsterAI>();
			if (m_ai == null)
			{
				// Copy MonsterAI from an existing humanoid template purely for its pathing (MoveTo) -
				// matches the same AddComponentcc<MonsterAI> convention HumanManager already uses when
				// giving custom humanoid NPCs working AI/navigation.
				var template = ZNetScene.instance?.GetPrefab("Goblin");
				var templateAi = template != null ? template.GetComponent<MonsterAI>() : null;
				if (templateAi == null)
				{
					DBG.blogWarning("BuilderNPC: No MonsterAI template found, self-gathering disabled for this NPC");
					return;
				}

				m_ai = gameObject.AddComponentcc(templateAi);
				// The template is a hostile Goblin - force this NPC back to the friendly faction so it
				// never turns on players/other villagers just because it borrowed the Goblin's AI settings.
				if (m_hum != null) m_hum.m_faction = Character.Faction.Players;
			}

			// MonsterAI.UpdateAI is NOT driven by this component's own Update() - it's called by the
			// global MonoUpdaters.FixedUpdate staggered loop (~20Hz) for every enabled BaseAI in the
			// scene (see MonoUpdaters.cs / BaseAI.OnEnable registering into BaseAI.Instances), completely
			// independent of any per-GameObject Update(). Left enabled, MonsterAI's own wander/combat/
			// flee/sleep logic would run in parallel with (and fight) the manual MoveTo driving below.
			// We only want MonsterAI for its protected MoveTo pathing helper, which works fine called
			// directly via reflection even while disabled (it doesn't check m_ai.enabled internally), so
			// disable it to fully suppress all of its autonomous behavior.
			m_ai.enabled = false;
		}

		private void Update()
		{
			if (m_ai == null) return;
			if (ZNet.instance != null && !ZNet.instance.IsServer() && !m_nview.IsOwner()) return;

			switch (m_gatherState)
			{
				case GatherState.Idle:
					if (Time.time >= m_nextGatherScanTime)
					{
						m_nextGatherScanTime = Time.time + GatherScanInterval;
						TryStartGathering();
					}
					break;

				case GatherState.MovingToResource:
					if (m_gatherTarget == null)
					{
						m_gatherState = GatherState.Returning;
						break;
					}
					if (MoveToward(m_gatherTargetPos, GatherArriveDist))
					{
						m_harvestTimer = 0f;
						m_gatherState = GatherState.Harvesting;
					}
					break;

				case GatherState.Harvesting:
					if (m_gatherTarget == null)
					{
						m_gatherState = GatherState.Returning;
						break;
					}
					m_harvestTimer += Time.deltaTime;
					if (m_harvestTimer >= HarvestDuration)
					{
						HarvestTarget();
						m_gatherState = GatherState.Returning;
					}
					break;

				case GatherState.Returning:
					if (MoveToward(m_homePos, 1f))
					{
						m_gatherState = GatherState.Idle;
					}
					break;
			}
		}

		private bool MoveToward(Vector3 point, float arriveDist)
		{
			s_moveToArgs[0] = Time.deltaTime;
			s_moveToArgs[1] = point;
			s_moveToArgs[2] = arriveDist;
			s_moveToArgs[3] = false;
			return (bool)s_moveToMethod.Invoke(m_ai, s_moveToArgs);
		}

		private void TryStartGathering()
		{
			if (m_gatherState != GatherState.Idle) return;

			// Gather whichever resource is currently scarcer so neither pool starves while waiting on
			// donations for the other. Self-gathering only ever produces Wood/Stone (that's all
			// TreeBase/MineRock can yield) - anything else a blueprint needs must come from players.
			string primary = GetOrZero(m_resourcePool, "Wood") <= GetOrZero(m_resourcePool, "Stone") ? "Wood" : "Stone";
			string secondary = primary == "Wood" ? "Stone" : "Wood";

			if (GetOrZero(m_resourcePool, primary) < GatherTargetAmount && TryFindResource(primary))
				return;
			if (GetOrZero(m_resourcePool, secondary) < GatherTargetAmount && TryFindResource(secondary))
				return;
		}

		private bool TryFindResource(string resourceKey)
		{
			int hitCount = Physics.OverlapSphereNonAlloc(transform.position, GatherRadius, s_gatherHitsBuffer);
			Component best = null;
			float bestDist = float.MaxValue;

			for (int i = 0; i < hitCount; i++)
			{
				var col = s_gatherHitsBuffer[i];
				Component candidate = resourceKey == "Wood"
					? (Component)col.GetComponentInParent<TreeBase>()
					: col.GetComponentInParent<MineRock5>() ?? (Component)col.GetComponentInParent<MineRock>();

				if (candidate == null) continue;

				float dist = Vector3.Distance(transform.position, candidate.transform.position);
				if (dist < bestDist)
				{
					bestDist = dist;
					best = candidate;
				}
			}

			if (best == null) return false;

			m_gatherTarget = best;
			m_gatherResourceKey = resourceKey;
			m_gatherTargetPos = best.transform.position;
			m_gatherState = GatherState.MovingToResource;
			return true;
		}

		private void HarvestTarget()
		{
			if (m_gatherTarget is IDestructible destructible)
			{
				var hit = new HitData();
				hit.m_damage.m_chop = 1000f;
				hit.m_damage.m_pickaxe = 1000f;
				hit.m_toolTier = 100;
				hit.m_point = m_gatherTarget.transform.position;
				destructible.Damage(hit);
			}

			m_resourcePool[m_gatherResourceKey] = GetOrZero(m_resourcePool, m_gatherResourceKey) + HarvestYield;
			PersistResourcePool();

			DBG.blogInfo($"BuilderNPC: Harvested {HarvestYield} {m_gatherResourceKey}, pool now {m_resourcePool[m_gatherResourceKey]}");
			m_gatherTarget = null;
			CheckForBuildableStructures();
		}
		#endregion SelfGather

		private void CheckForBuildableStructures()
		{
			if (m_buildCoroutine != null)
			{
				Say("I'm already building!");
				return;
			}

			foreach (var bp in GetEligibleBlueprints())
			{
				if (CanAfford(bp))
				{
					StartBuilding(bp);
					return;
				}
			}
		}

		// Faction YAML can assign specific blueprints to a faction (FactionDef.AssignedBlueprints); if it
		// does, only those are considered for this NPC - the faction config becomes the single place that
		// decides *which* blueprints apply, while the blueprint YAML remains the only place resource costs
		// live (no duplicated cost lists). Falls back to the older per-blueprint AllowedFactions check for
		// factions/blueprints that don't use assignment yet.
		private IEnumerable<Blueprint> GetEligibleBlueprints()
		{
			if (FactionManager.Factions.TryGetValue(FactionName, out var faction) &&
				faction.AssignedBlueprints != null && faction.AssignedBlueprints.Count > 0)
			{
				foreach (var name in faction.AssignedBlueprints)
				{
					var bp = Blueprints.All.FirstOrDefault(b => b.name == name);
					if (bp != null) yield return bp;
				}
				yield break;
			}

			foreach (var bp in Blueprints.All)
			{
				if (IsAllowedForFaction(bp)) yield return bp;
			}
		}

		// A blueprint with no AllowedFactions entries is buildable by any faction (backwards compatible
		// default). Non-empty means only listed factions (matched against this NPC's FactionName) qualify.
		private bool IsAllowedForFaction(Blueprint bp)
		{
			if (bp.allowedFactions == null || bp.allowedFactions.Count == 0) return true;
			return bp.allowedFactions.Contains(FactionName);
		}

		private bool CanAfford(Blueprint bp)
		{
			foreach (var cost in bp.resourceCosts)
			{
				if (GetOrZero(m_resourcePool, cost.Key) < cost.Value)
					return false;
			}
			return true;
		}

		private void StartBuilding(Blueprint bp)
		{
			// Set build origin now, in front of current position
			m_buildOrigin = transform.position + transform.forward * 5f;

			m_currentBlueprint = bp;
			m_buildStep = 0;

			var zdo = m_nview.GetZDO();
			if (zdo != null)
			{
				zdo.Set(ZK_BuildName, bp.name);
				zdo.Set(ZK_BuildStep, 0);
				zdo.Set(ZK_BuildOrigin, m_buildOrigin);
			}

			// Resources are deducted incrementally as each piece completes (see BuildCoroutine),
			// not all up-front - matches the rest of the structure existing as blue holograms
			// until the NPC actually "builds" that specific piece.
			Say($"I'll start building a {bp.name}! Marking out the plan now...");
			SpawnGhostsFrom(0);
			m_buildCoroutine = StartCoroutine(BuildCoroutine());
		}

		private void SpawnGhostsFrom(int startStep)
		{
			ClearGhosts();
			if (m_currentBlueprint == null) return;
			for (int i = startStep; i < m_currentBlueprint.pieces.Length; i++)
			{
				var piece = m_currentBlueprint.pieces[i];
				var prefab = ZNetScene.instance.GetPrefab(piece.prefabName);
				if (prefab == null) continue;
				var ghost = BlueprintGhost.Create(prefab, m_buildOrigin + piece.localPosition, Quaternion.Euler(piece.rotation), null);
				if (ghost != null) m_ghosts.Add(ghost);
			}
		}

		private void ClearGhosts()
		{
			foreach (var ghost in m_ghosts)
			{
				if (ghost != null) Destroy(ghost);
			}
			m_ghosts.Clear();
		}

		private IEnumerator BuildCoroutine()
		{
			Say($"Starting construction of {m_currentBlueprint.name} - watch the blueprint come to life!");
			yield return new WaitForSeconds(1f);

			// Split the blueprint's total resource cost evenly across its pieces so each completed
			// piece consumes its own share incrementally, instead of the whole structure's cost being
			// deducted in one lump sum before any hologram exists.
			int pieceCount = Mathf.Max(1, m_currentBlueprint.pieces.Length);

			// Build each piece
			while (m_buildStep < m_currentBlueprint.pieces.Length)
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

		private void RecordMilestone(string prefabName)
		{
			m_recentlyPlaced.Add(prefabName);
			while (m_recentlyPlaced.Count > MaxRecentMilestones) m_recentlyPlaced.RemoveAt(0);
		}

		private void DeductPieceShare(int stepIndex, int pieceCount)
		{
			foreach (var cost in m_currentBlueprint.resourceCosts)
			{
				int totalForResource = cost.Value;
				int baseShare = totalForResource / pieceCount;
				int remainder = totalForResource % pieceCount;
				// Give the last piece any leftover from integer division so the full cost is always
				// consumed exactly once, not under/over by rounding.
				int share = baseShare + (stepIndex == pieceCount - 1 ? remainder : 0);
				m_resourcePool[cost.Key] = Mathf.Max(0, GetOrZero(m_resourcePool, cost.Key) - share);
			}
			PersistResourcePool();
		}

		private void ReplaceGhostWithPiece(int stepIndex, BlueprintPiece piece)
		{
			if (stepIndex < m_ghosts.Count && m_ghosts[stepIndex] != null)
			{
				Destroy(m_ghosts[stepIndex]);
				m_ghosts[stepIndex] = null;
			}
			PlacePiece(piece);
		}

		private void PlacePiece(BlueprintPiece piece)
		{
			var prefab = ZNetScene.instance.GetPrefab(piece.prefabName);
			if (prefab == null)
			{
				DBG.blogWarning($"BuilderNPC: Prefab '{piece.prefabName}' not found");
				return;
			}

			Vector3 worldPos = m_buildOrigin + piece.localPosition;
			Quaternion worldRot = Quaternion.Euler(piece.rotation);

			var go = Instantiate(prefab, worldPos, worldRot);
			go.SetActive(true);

			DBG.blogInfo($"BuilderNPC: Placed {piece.prefabName} at {worldPos}");
		}

		public override string GetHoverText()
		{
			if (m_hum.m_faction != Character.Faction.Players) return "";

			string text = $"<color=#ADD8E6FF>{m_name} (Builder)</color>\n";

			if (m_buildCoroutine != null)
			{
				int percent = m_currentBlueprint.pieces.Length > 0 ? m_buildStep * 100 / m_currentBlueprint.pieces.Length : 100;
				text += $"<color=yellow>Building {m_currentBlueprint.name} - {percent}% complete ({m_buildStep}/{m_currentBlueprint.pieces.Length})</color>\n";
				if (m_recentlyPlaced.Count > 0)
					text += $"<color=white>Recently placed: {string.Join(", ", m_recentlyPlaced)}</color>\n";
			}
			else
			{
				var target = GetNextTarget();
				if (target != null)
					text += $"<color=white>We still need:</color>\n{FormatRemaining(target)}";
				else
					text += "<color=white>No blueprint assigned yet.</color>\n";
			}

			text += "[<color=yellow><b>$KEY_Use</b></color>] Talk";
			return text;
		}
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

		public static List<Blueprint> All = new List<Blueprint>();
	}

	public static class Blueprints
	{
		public static List<Blueprint> All = new List<Blueprint>();

		public static void Init()
		{
			// =====================================================
			// EXAMPLE BLUEPRINTS - REPLACE WITH UNITY EXPORTS!
			//
			// To create proper blueprints:
			// 1. Open Valheim Unity project
			// 2. Build your structure using piece prefabs (wood_floor_1x1, wood_wall_roof, etc.)
			// 3. Select all pieces in the hierarchy
			// 4. Tools → Export Blueprint
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
					new BlueprintPiece("wood_floor_1x1", new Vector3(0f, 0f, 0f), Vector3.zero),
					new BlueprintPiece("wood_floor_1x1", new Vector3(2f, 0f, 0f), Vector3.zero),
					new BlueprintPiece("wood_floor_1x1", new Vector3(0f, 0f, 2f), Vector3.zero),
					new BlueprintPiece("wood_floor_1x1", new Vector3(2f, 0f, 2f), Vector3.zero),
				}
			));

			// Example 2: Stone hearth area
			All.Add(new Blueprint(
				"Stone Hearth",
				new Dictionary<string, int> { { "Stone", 15 } },
				new BlueprintPiece[]
				{
					new BlueprintPiece("stone_floor_2x2", new Vector3(0f, 0f, 0f), Vector3.zero),
					new BlueprintPiece("fire_pit", new Vector3(0f, 0f, 0f), Vector3.zero),
				}
			));

			// Example 3: Simple fence segment
			All.Add(new Blueprint(
				"Fence Segment",
				new Dictionary<string, int> { { "Wood", 15 } },
				new BlueprintPiece[]
				{
					new BlueprintPiece("wood_fence", new Vector3(0f, 0f, 0f), Vector3.zero),
					new BlueprintPiece("wood_fence", new Vector3(2f, 0f, 0f), Vector3.zero),
					new BlueprintPiece("wood_fence", new Vector3(4f, 0f, 0f), Vector3.zero),
					new BlueprintPiece("wood_fence", new Vector3(6f, 0f, 0f), Vector3.zero),
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
					new BlueprintPiece("wood_floor_1x1", new Vector3(0f, 0f, 0f), Vector3.zero),
					new BlueprintPiece("wood_floor_1x1", new Vector3(2f, 0f, 0f), Vector3.zero),
					// ... (rest of pieces from exporter)
				}
			));
			*/

			DBG.blogInfo($"Blueprints initialized: {All.Count} structures available");
		}
	}
}
