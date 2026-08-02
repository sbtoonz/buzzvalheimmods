using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace OdinPlus
{
	public class BuilderNPC : MaterialVillager
	{
		private Coroutine m_buildCoroutine;
		private Blueprint m_currentBlueprint;
		private int m_buildStep = 0;
		private Vector3 m_buildOrigin;

		private Dictionary<string, int> m_resourcePool = new Dictionary<string, int>
		{
			{ "Wood", 0 },
			{ "Stone", 0 }
		};

		protected override void Awake()
		{
			base.Awake();
			ChoiceList = new string[3] { "$op_talk", "What do you need?", "Status" };
			// Don't set build origin here - calculate it when starting to build
		}

		private void OnDestroy()
		{
			// Stop build coroutine to prevent memory leak
			if (m_buildCoroutine != null)
			{
				StopCoroutine(m_buildCoroutine);
				m_buildCoroutine = null;
			}
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
			}
			else
			{
				string needs = "I can build once you give me:\n";
				var nextBlueprint = Blueprints.All.FirstOrDefault(bp => !CanAfford(bp));
				if (nextBlueprint != null)
				{
					foreach (var cost in nextBlueprint.resourceCosts)
					{
						int current = m_resourcePool.ContainsKey(cost.Key) ? m_resourcePool[cost.Key] : 0;
						needs += $"{cost.Key}: {current}/{cost.Value}\n";
					}
					Say(needs);
				}
				else
				{
					Say("Give me wood or stone, and I'll build something!");
				}
			}
		}

		public void Choice2()
		{
			string status = $"Resources on hand:\nWood: {m_resourcePool["Wood"]}\nStone: {m_resourcePool["Stone"]}";
			if (m_buildCoroutine != null)
			{
				status += $"\n\nBuilding: {m_currentBlueprint.name} ({m_buildStep}/{m_currentBlueprint.pieces.Length})";
			}
			Say(status);
		}

		public override bool UseItem(Humanoid user, ItemDrop.ItemData item)
		{
			// Accept wood and stone donations
			if (item.m_shared.m_name == "$item_wood")
			{
				int count = Mathf.Min(item.m_stack, 50);
				user.GetInventory().RemoveItem(item.m_shared.m_name, count);
				m_resourcePool["Wood"] += count;
				Say($"Thanks! I have {m_resourcePool["Wood"]} wood now.");
				CheckForBuildableStructures();
				return true;
			}
			else if (item.m_shared.m_name == "$item_stone")
			{
				int count = Mathf.Min(item.m_stack, 50);
				user.GetInventory().RemoveItem(item.m_shared.m_name, count);
				m_resourcePool["Stone"] += count;
				Say($"Thanks! I have {m_resourcePool["Stone"]} stone now.");
				CheckForBuildableStructures();
				return true;
			}
			return base.UseItem(user, item);
		}

		private void CheckForBuildableStructures()
		{
			if (m_buildCoroutine != null)
			{
				Say("I'm already building!");
				return;
			}

			// Check if we can afford any blueprint
			foreach (var bp in Blueprints.All)
			{
				if (CanAfford(bp))
				{
					StartBuilding(bp);
					return;
				}
			}
		}

		private bool CanAfford(Blueprint bp)
		{
			foreach (var cost in bp.resourceCosts)
			{
				if (!m_resourcePool.ContainsKey(cost.Key) || m_resourcePool[cost.Key] < cost.Value)
					return false;
			}
			return true;
		}

		private void StartBuilding(Blueprint bp)
		{
			// Deduct resources
			foreach (var cost in bp.resourceCosts)
			{
				m_resourcePool[cost.Key] -= cost.Value;
			}

			// Set build origin now, in front of current position
			m_buildOrigin = transform.position + transform.forward * 5f;

			m_currentBlueprint = bp;
			m_buildStep = 0;
			Say($"I'll start building a {bp.name}!");
			m_buildCoroutine = StartCoroutine(BuildCoroutine());
		}

		private IEnumerator BuildCoroutine()
		{
			// Calculate build center from all pieces
			Vector3 buildCenter = m_buildOrigin;
			if (m_currentBlueprint.pieces.Length > 0)
			{
				Vector3 sum = Vector3.zero;
				foreach (var piece in m_currentBlueprint.pieces)
				{
					sum += m_buildOrigin + piece.localPosition;
				}
				buildCenter = sum / m_currentBlueprint.pieces.Length;
			}

			// Walk to build site (simplified - just build where standing)
			Say($"Starting construction at {m_buildOrigin}!");
			yield return new WaitForSeconds(1f);

			// Build each piece
			while (m_buildStep < m_currentBlueprint.pieces.Length)
			{
				var piece = m_currentBlueprint.pieces[m_buildStep];
				PlacePiece(piece);
				m_buildStep++;
				yield return new WaitForSeconds(3f);
			}

			Say($"{m_currentBlueprint.name} is complete!");
			m_buildCoroutine = null;
			m_currentBlueprint = null;
			m_buildStep = 0;
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
			text += $"<color=white>Wood: {m_resourcePool["Wood"]}, Stone: {m_resourcePool["Stone"]}</color>\n";

			if (m_buildCoroutine != null)
			{
				text += $"<color=yellow>Building {m_currentBlueprint.name}... ({m_buildStep}/{m_currentBlueprint.pieces.Length})</color>\n";
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

		public Blueprint(string n, Dictionary<string, int> costs, BlueprintPiece[] p)
		{
			name = n;
			resourceCosts = costs;
			pieces = p;
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
