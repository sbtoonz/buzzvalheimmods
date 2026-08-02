using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using UnityEngine;

namespace OdinPlus
{
	/// <summary>
	/// In-game blueprint scanner - build structures with hammer, scan them as blueprints
	/// </summary>
	public class BlueprintScanner : MonoBehaviour
	{
		private static BlueprintScanner _instance;

		public static BlueprintScanner Instance
		{
			get
			{
				if (_instance == null)
				{
					var go = new GameObject("BlueprintScanner");
					_instance = go.AddComponent<BlueprintScanner>();
					DontDestroyOnLoad(go);
				}
				return _instance;
			}
		}

		/// <summary>
		/// Scan area around player for built pieces and generate blueprint code
		/// </summary>
		public void ScanArea(string blueprintName, float radius, int woodCost, int stoneCost)
		{
			if (Player.m_localPlayer == null)
			{
				DBG.blogWarning("[BlueprintScanner] No local player found");
				return;
			}

			Vector3 playerPos = Player.m_localPlayer.transform.position;
			DBG.blogInfo($"[BlueprintScanner] Scanning {radius}m radius around player for pieces...");

			// Find all Piece components within radius
			var allPieces = FindObjectsOfType<Piece>();
			var nearbyPieces = allPieces
				.Where(p => Vector3.Distance(p.transform.position, playerPos) <= radius)
				.Where(p => p.gameObject.activeInHierarchy)
				.OrderBy(p => p.transform.position.y)
				.ThenBy(p => p.transform.position.x)
				.ToList();

			if (nearbyPieces.Count == 0)
			{
				DBG.blogWarning($"[BlueprintScanner] No pieces found within {radius}m");
				Player.m_localPlayer.Message(MessageHud.MessageType.Center, $"No pieces found within {radius}m");
				return;
			}

			DBG.blogInfo($"[BlueprintScanner] Found {nearbyPieces.Count} pieces");

			// Use player position as origin (or first piece)
			Vector3 origin = playerPos;
			origin.y = nearbyPieces[0].transform.position.y; // Match ground level

			// Generate blueprint code
			string blueprintCode = GenerateBlueprintCode(blueprintName, nearbyPieces, origin, woodCost, stoneCost);

			// Save to file
			string outputPath = Path.Combine(BepInEx.Paths.PluginPath, $"blueprint_{blueprintName}.txt");
			File.WriteAllText(outputPath, blueprintCode);

			// Also log it
			DBG.blogWarning("=== BLUEPRINT CODE ===");
			DBG.blogWarning(blueprintCode);
			DBG.blogWarning("======================");

			Player.m_localPlayer.Message(MessageHud.MessageType.Center,
				$"Scanned {nearbyPieces.Count} pieces!\nSaved to: blueprint_{blueprintName}.txt");

			DBG.blogInfo($"[BlueprintScanner] Blueprint saved to {outputPath}");
		}

		private string GenerateBlueprintCode(string name, List<Piece> pieces, Vector3 origin, int woodCost, int stoneCost)
		{
			StringBuilder sb = new StringBuilder();

			sb.AppendLine($"// Blueprint: {name}");
			sb.AppendLine($"// Generated from {pieces.Count} in-game pieces");
			sb.AppendLine($"// Scanned at: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
			sb.AppendLine($"All.Add(new Blueprint(");
			sb.AppendLine($"    \"{name}\",");

			// Resource costs
			sb.Append($"    new Dictionary<string, int> {{ ");
			bool hasResources = false;
			if (woodCost > 0)
			{
				sb.Append($"{{ \"Wood\", {woodCost} }}");
				hasResources = true;
			}
			if (stoneCost > 0)
			{
				sb.Append($"{(hasResources ? ", " : "")}{{ \"Stone\", {stoneCost} }}");
			}
			sb.AppendLine(" },");

			sb.AppendLine($"    new BlueprintPiece[]");
			sb.AppendLine($"    {{");

			// Generate piece entries
			foreach (var piece in pieces)
			{
				// Calculate relative position
				Vector3 localPos = piece.transform.position - origin;

				// Get prefab name (strip "(Clone)" suffix)
				string prefabName = piece.gameObject.name.Replace("(Clone)", "").Trim();

				// Get rotation (round to nearest 90 degrees for cleaner code)
				Vector3 rotation = piece.transform.rotation.eulerAngles;
				rotation = new Vector3(
					RoundTo90(rotation.x),
					RoundTo90(rotation.y),
					RoundTo90(rotation.z)
				);

				// Round positions to 2 decimals
				localPos = new Vector3(
					Mathf.Round(localPos.x * 100f) / 100f,
					Mathf.Round(localPos.y * 100f) / 100f,
					Mathf.Round(localPos.z * 100f) / 100f
				);

				string posStr = $"new Vector3({localPos.x}f, {localPos.y}f, {localPos.z}f)";
				string rotStr = rotation == Vector3.zero ? "Vector3.zero" : $"new Vector3({rotation.x}f, {rotation.y}f, {rotation.z}f)";

				sb.AppendLine($"        new BlueprintPiece(\"{prefabName}\", {posStr}, {rotStr}),");
			}

			sb.AppendLine($"    }}");
			sb.AppendLine($"));");
			sb.AppendLine();

			return sb.ToString();
		}

		/// <summary>
		/// Round angle to nearest 90 degrees
		/// </summary>
		private float RoundTo90(float angle)
		{
			// Normalize to 0-360
			while (angle < 0) angle += 360;
			while (angle >= 360) angle -= 360;

			// Round to nearest 90
			int rounded = Mathf.RoundToInt(angle / 90f) * 90;
			if (rounded == 360) rounded = 0;

			return rounded;
		}

		/// <summary>
		/// Scan and auto-calculate resource costs based on pieces found
		/// </summary>
		public void ScanAreaAuto(string blueprintName, float radius)
		{
			if (Player.m_localPlayer == null)
			{
				DBG.blogWarning("[BlueprintScanner] No local player found");
				return;
			}

			Vector3 playerPos = Player.m_localPlayer.transform.position;

			// Find pieces
			var allPieces = FindObjectsOfType<Piece>();
			var nearbyPieces = allPieces
				.Where(p => Vector3.Distance(p.transform.position, playerPos) <= radius)
				.Where(p => p.gameObject.activeInHierarchy)
				.ToList();

			if (nearbyPieces.Count == 0)
			{
				DBG.blogWarning($"[BlueprintScanner] No pieces found within {radius}m");
				Player.m_localPlayer.Message(MessageHud.MessageType.Center, $"No pieces found within {radius}m");
				return;
			}

			// Calculate resource costs by counting piece types
			int woodCost = 0;
			int stoneCost = 0;

			foreach (var piece in nearbyPieces)
			{
				string name = piece.gameObject.name.ToLower();

				// Simple heuristic: wood pieces cost ~2 wood, stone pieces cost ~3 stone
				if (name.Contains("wood") || name.Contains("thatch"))
				{
					woodCost += 2;
				}
				else if (name.Contains("stone") || name.Contains("marble"))
				{
					stoneCost += 3;
				}
				else
				{
					// Default: assume wood
					woodCost += 1;
				}
			}

			// Round up to nearest 5
			woodCost = Mathf.CeilToInt(woodCost / 5f) * 5;
			stoneCost = Mathf.CeilToInt(stoneCost / 5f) * 5;

			DBG.blogInfo($"[BlueprintScanner] Auto-calculated costs: {woodCost} wood, {stoneCost} stone");

			// Delegate to regular scan with calculated costs
			ScanArea(blueprintName, radius, woodCost, stoneCost);
		}

		/// <summary>
		/// Visual preview - highlight pieces that would be scanned
		/// </summary>
		public void PreviewScanArea(float radius, float duration = 5f)
		{
			if (Player.m_localPlayer == null) return;

			Vector3 playerPos = Player.m_localPlayer.transform.position;

			var allPieces = FindObjectsOfType<Piece>();
			var nearbyPieces = allPieces
				.Where(p => Vector3.Distance(p.transform.position, playerPos) <= radius)
				.Where(p => p.gameObject.activeInHierarchy)
				.ToList();

			Player.m_localPlayer.Message(MessageHud.MessageType.Center,
				$"Found {nearbyPieces.Count} pieces in {radius}m radius");

			// Draw debug spheres around each piece
			foreach (var piece in nearbyPieces)
			{
				// Create temporary visual indicator
				GameObject indicator = GameObject.CreatePrimitive(PrimitiveType.Sphere);
				indicator.transform.position = piece.transform.position;
				indicator.transform.localScale = Vector3.one * 0.5f;

				// Make it glow
				var renderer = indicator.GetComponent<Renderer>();
				if (renderer != null)
				{
					renderer.material.color = Color.yellow;
					renderer.material.SetFloat("_EmissionColor", 1f);
				}

				// Remove collider
				var collider = indicator.GetComponent<Collider>();
				if (collider != null) Destroy(collider);

				// Auto-destroy after duration
				Destroy(indicator, duration);
			}

			DBG.blogInfo($"[BlueprintScanner] Preview: {nearbyPieces.Count} pieces highlighted for {duration}s");
		}
	}
}
