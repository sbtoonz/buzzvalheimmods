using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using UnityEngine;

namespace OdinPlus
{
	/// <summary>
	/// Visual blueprint selection system - place corner markers to define zone
	/// </summary>
	public class BlueprintSelector : MonoBehaviour
	{
		private static BlueprintSelector _instance;
		private List<GameObject> _markers = new List<GameObject>();
		private GameObject _previewBox;
		private bool _isPlacingMarkers = false;
		private string _blueprintName = "";
		private float _heightAdjust = 0f;
		private float _lastMessageTime = 0f;

		public static BlueprintSelector Instance
		{
			get
			{
				if (_instance == null)
				{
					var go = new GameObject("BlueprintSelector");
					_instance = go.AddComponent<BlueprintSelector>();
					DontDestroyOnLoad(go);
				}
				return _instance;
			}
		}

		private void Update()
		{
			if (!_isPlacingMarkers) return;
			if (Player.m_localPlayer == null) return;

			// Show instructions (throttled to every 0.5s to avoid spam)
			if (Time.time - _lastMessageTime > 0.5f)
			{
				_lastMessageTime = Time.time;

				string instructions = "";
				if (_markers.Count == 0)
				{
					instructions = $"Blueprint: {_blueprintName}\n[LMB] Place Corner 1 | [Esc] Cancel";
				}
				else if (_markers.Count == 1)
				{
					instructions = $"Blueprint: {_blueprintName}\n[LMB] Place Corner 2 | [RMB] Clear | [Esc] Cancel";
				}
				else if (_markers.Count == 2)
				{
					instructions = $"Blueprint: {_blueprintName} | Height: {_heightAdjust:F1}m\n[Scroll] Adjust | [LMB] Confirm | [RMB] Clear | [Esc] Cancel";
				}

				if (!string.IsNullOrEmpty(instructions))
				{
					Player.m_localPlayer.Message(MessageHud.MessageType.Center, instructions);
				}
			}

			// Handle scroll wheel for height adjustment (when preview box is visible)
			if (_markers.Count == 2 && _previewBox != null)
			{
				float scroll = Input.GetAxis("Mouse ScrollWheel");
				if (scroll != 0f)
				{
					_heightAdjust += scroll * 2f; // Adjust by 2m per scroll tick
					UpdatePreviewBox();
				}
			}

			// Handle input
			if (Input.GetKeyDown(KeyCode.Escape))
			{
				CancelSelection();
				return;
			}

			if (Input.GetMouseButtonDown(1) && _markers.Count > 0) // Right click = clear last
			{
				RemoveLastMarker();
				return;
			}

			if (Input.GetMouseButtonDown(0)) // Left click = place marker
			{
				PlaceMarker();
			}
		}

		/// <summary>
		/// Start blueprint selection mode
		/// </summary>
		public void StartSelection(string blueprintName)
		{
			if (_isPlacingMarkers)
			{
				DBG.blogWarning("[BlueprintSelector] Already in selection mode");
				return;
			}

			_blueprintName = blueprintName;
			_isPlacingMarkers = true;
			_markers.Clear();

			Player.m_localPlayer.Message(MessageHud.MessageType.Center,
				$"Blueprint Selection: {blueprintName}");
			DBG.blogInfo($"[BlueprintSelector] Started selection for '{blueprintName}'");
		}

		private void PlaceMarker()
		{
			if (_markers.Count >= 2)
			{
				// Already have 2 corners - scan and finish
				ScanZone();
				return;
			}

			// Don't place if hovering over UI or menu
			if (GUIUtility.hotControl != 0) return;

			// Raycast to find ground position
			Ray ray = GameCamera.instance.GetComponent<Camera>().ScreenPointToRay(Input.mousePosition);
			if (Physics.Raycast(ray, out RaycastHit hit, 100f))
			{
				Vector3 pos = hit.point;
				DBG.blogInfo($"[BlueprintSelector] Raycast hit at {pos}, object: {hit.collider?.gameObject.name}");

				// Create marker visual
				GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
				marker.name = $"BlueprintMarker_{_markers.Count + 1}";
				marker.transform.position = pos;
				marker.transform.localScale = new Vector3(0.5f, 2f, 0.5f); // Tall cylinder

				// Style it
				var renderer = marker.GetComponent<Renderer>();
				if (renderer != null)
				{
					renderer.material.color = _markers.Count == 0 ? Color.green : Color.red;
					renderer.material.SetFloat("_Metallic", 0.8f);
				}

				// Remove collider so it doesn't interfere
				var collider = marker.GetComponent<Collider>();
				if (collider != null) Destroy(collider);

				_markers.Add(marker);

				Player.m_localPlayer.Message(MessageHud.MessageType.Center,
					$"Corner {_markers.Count} placed");
				DBG.blogInfo($"[BlueprintSelector] Corner {_markers.Count} placed at {pos}");

				// If we have 2 markers, show preview box
				if (_markers.Count == 2)
				{
					_heightAdjust = 0f; // Reset height adjustment
					DBG.blogInfo("[BlueprintSelector] Both corners placed, showing preview box");
					ShowPreviewBox();
					Player.m_localPlayer.Message(MessageHud.MessageType.Center,
						"[Scroll] Adjust Height\n[LMB] Confirm\n[RMB] Clear\n[Esc] Cancel");
				}
			}
			else
			{
				DBG.blogWarning("[BlueprintSelector] Raycast hit nothing - try clicking on solid ground/objects");
			}
		}

		private void RemoveLastMarker()
		{
			if (_markers.Count > 0)
			{
				var last = _markers[_markers.Count - 1];
				_markers.RemoveAt(_markers.Count - 1);
				Destroy(last);

				// If we removed the preview box, clean it up
				if (_previewBox != null)
				{
					Destroy(_previewBox);
					_previewBox = null;
				}

				Player.m_localPlayer.Message(MessageHud.MessageType.Center,
					$"Removed corner {_markers.Count + 1}");
			}
		}

		private void ShowPreviewBox()
		{
			if (_markers.Count != 2) return;

			Vector3 corner1 = _markers[0].transform.position;
			Vector3 corner2 = _markers[1].transform.position;

			// Calculate bounds
			Vector3 min = new Vector3(
				Mathf.Min(corner1.x, corner2.x),
				Mathf.Min(corner1.y, corner2.y),
				Mathf.Min(corner1.z, corner2.z)
			);
			Vector3 max = new Vector3(
				Mathf.Max(corner1.x, corner2.x),
				Mathf.Max(corner1.y, corner2.y),
				Mathf.Max(corner1.z, corner2.z)
			);

			Vector3 center = (min + max) / 2f;
			Vector3 size = max - min;

			// Create preview box
			GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
			box.name = "BlueprintPreviewBox";
			box.transform.position = center;
			box.transform.localScale = size;

			// Make it transparent cyan using forced transparency settings
			var renderer = box.GetComponent<Renderer>();
			if (renderer != null)
			{
				Material mat = renderer.material;

				// Force transparency in every possible way
				mat.SetOverrideTag("RenderType", "Transparent");
				mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
				mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
				mat.SetInt("_ZWrite", 0);
				mat.DisableKeyword("_ALPHATEST_ON");
				mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
				mat.EnableKeyword("_ALPHABLEND_ON");
				mat.renderQueue = 3000;

				// Set cyan color with alpha
				Color cyan = new Color(0f, 0.8f, 1f, 0.4f);
				mat.color = cyan;
				mat.SetColor("_Color", cyan);
				mat.SetColor("_BaseColor", cyan);
				mat.SetColor("_EmissionColor", cyan * 0.5f);

				DBG.blogInfo($"[BlueprintSelector] Created transparent preview box with shader: {mat.shader.name}");
			}

			// Remove collider
			var collider = box.GetComponent<Collider>();
			if (collider != null) Destroy(collider);

			_previewBox = box;
		}

		private void UpdatePreviewBox()
		{
			if (_markers.Count != 2 || _previewBox == null) return;

			Vector3 corner1 = _markers[0].transform.position;
			Vector3 corner2 = _markers[1].transform.position;

			// Calculate bounds with height adjustment
			Vector3 min = new Vector3(
				Mathf.Min(corner1.x, corner2.x),
				Mathf.Min(corner1.y, corner2.y) - _heightAdjust,
				Mathf.Min(corner1.z, corner2.z)
			);
			Vector3 max = new Vector3(
				Mathf.Max(corner1.x, corner2.x),
				Mathf.Max(corner1.y, corner2.y) + _heightAdjust,
				Mathf.Max(corner1.z, corner2.z)
			);

			Vector3 center = (min + max) / 2f;
			Vector3 size = max - min;

			_previewBox.transform.position = center;
			_previewBox.transform.localScale = size;
		}

		private void ScanZone()
		{
			if (_markers.Count < 2)
			{
				DBG.blogWarning("[BlueprintSelector] Need at least 2 corners");
				return;
			}

			Vector3 corner1 = _markers[0].transform.position;
			Vector3 corner2 = _markers[1].transform.position;

			// Calculate bounds with height adjustment
			Vector3 min = new Vector3(
				Mathf.Min(corner1.x, corner2.x),
				Mathf.Min(corner1.y, corner2.y) - _heightAdjust,
				Mathf.Min(corner1.z, corner2.z)
			);
			Vector3 max = new Vector3(
				Mathf.Max(corner1.x, corner2.x),
				Mathf.Max(corner1.y, corner2.y) + _heightAdjust,
				Mathf.Max(corner1.z, corner2.z)
			);

			Vector3 center = (min + max) / 2f;

			// Find all pieces in bounds
			var allPieces = FindObjectsOfType<Piece>();
			var piecesInBounds = allPieces
				.Where(p => p.gameObject.activeInHierarchy)
				.Where(p => IsInBounds(p.transform.position, min, max))
				.OrderBy(p => p.transform.position.y)
				.ThenBy(p => p.transform.position.x)
				.ToList();

			if (piecesInBounds.Count == 0)
			{
				Player.m_localPlayer.Message(MessageHud.MessageType.Center,
					"No pieces found in selection!");
				DBG.blogWarning("[BlueprintSelector] No pieces in bounds");
				CancelSelection();
				return;
			}

			Player.m_localPlayer.Message(MessageHud.MessageType.Center,
				$"Scanning {piecesInBounds.Count} pieces...");

			// Use center of bounds as origin
			Vector3 origin = center;
			origin.y = piecesInBounds[0].transform.position.y; // Match ground level

			// Calculate actual costs from piece requirements
			Dictionary<string, int> totalCosts = new Dictionary<string, int>();
			foreach (var piece in piecesInBounds)
			{
				// Get the actual piece requirements
				if (piece.m_resources != null)
				{
					foreach (var req in piece.m_resources)
					{
						if (req.m_resItem == null) continue;

						string resourceName = req.m_resItem.m_itemData.m_shared.m_name;
						int amount = req.m_amount;

						// Normalize names to simple keys
						string key = GetResourceKey(resourceName);
						if (!string.IsNullOrEmpty(key))
						{
							if (!totalCosts.ContainsKey(key))
								totalCosts[key] = 0;
							totalCosts[key] += amount;
						}
					}
				}
			}

			// Export directly to YAML
			BlueprintConfig.ExportScannedBlueprint(_blueprintName, piecesInBounds, origin, totalCosts);

			// Build cost summary message
			string costSummary = string.Join(", ", totalCosts.OrderBy(kvp => kvp.Key).Select(kvp => $"{kvp.Key}: {kvp.Value}"));
			if (string.IsNullOrEmpty(costSummary)) costSummary = "No resources";

			DBG.blogInfo($"[BlueprintSelector] Scanned {piecesInBounds.Count} pieces, costs: {costSummary}, exported to YAML");

			// Cleanup
			FinishSelection();
		}

		private bool IsInBounds(Vector3 point, Vector3 min, Vector3 max)
		{
			return point.x >= min.x && point.x <= max.x &&
			       point.y >= min.y && point.y <= max.y &&
			       point.z >= min.z && point.z <= max.z;
		}

		private string GenerateBlueprintCode(string name, List<Piece> pieces, Vector3 origin, Dictionary<string, int> costs)
		{
			StringBuilder sb = new StringBuilder();

			sb.AppendLine($"// Blueprint: {name}");
			sb.AppendLine($"// Generated from {pieces.Count} in-game pieces");
			sb.AppendLine($"// Scanned at: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
			sb.AppendLine($"// Selection mode: Visual zone selector");
			sb.AppendLine($"// Costs calculated from actual piece requirements");
			sb.AppendLine($"All.Add(new Blueprint(");
			sb.AppendLine($"    \"{name}\",");

			// Resource costs from actual pieces
			sb.Append($"    new Dictionary<string, int> {{ ");
			bool hasResources = false;
			foreach (var cost in costs.OrderBy(kvp => kvp.Key))
			{
				if (cost.Value > 0)
				{
					if (hasResources) sb.Append(", ");
					sb.Append($"{{ \"{cost.Key}\", {cost.Value} }}");
					hasResources = true;
				}
			}
			if (!hasResources)
			{
				// Fallback if no resources detected
				sb.Append("{ \"Wood\", 10 }");
			}
			sb.AppendLine(" },");

			// Pieces array
			sb.AppendLine("    new BlueprintPiece[]");
			sb.AppendLine("    {");

			for (int i = 0; i < pieces.Count; i++)
			{
				var piece = pieces[i];
				string prefabName = piece.name.Replace("(Clone)", "").Trim();

				// Calculate local position relative to origin
				Vector3 localPos = piece.transform.position - origin;

				// Get rotation
				Vector3 rotation = piece.transform.rotation.eulerAngles;

				// Format the line
				string line = $"        new BlueprintPiece(\"{prefabName}\", " +
				              $"new Vector3({localPos.x:F1}f, {localPos.y:F1}f, {localPos.z:F1}f), " +
				              $"new Vector3({rotation.x:F1}f, {rotation.y:F1}f, {rotation.z:F1}f))";

				if (i < pieces.Count - 1)
					line += ",";

				sb.AppendLine(line);
			}

			sb.AppendLine("    }");
			sb.AppendLine("));");

			return sb.ToString();
		}

		private string GetResourceKey(string resourceName)
		{
			// Normalize Valheim resource names to simple keys
			string lower = resourceName.ToLower();

			if (lower.Contains("wood")) return "Wood";
			if (lower.Contains("stone")) return "Stone";
			if (lower.Contains("iron")) return "Iron";
			if (lower.Contains("bronze")) return "Bronze";
			if (lower.Contains("copper")) return "Copper";
			if (lower.Contains("tin")) return "Tin";
			if (lower.Contains("silver")) return "Silver";
			if (lower.Contains("blackmetal")) return "BlackMetal";
			if (lower.Contains("coal")) return "Coal";
			if (lower.Contains("resin")) return "Resin";
			if (lower.Contains("leather")) return "Leather";
			if (lower.Contains("hide")) return "Hide";

			return null; // Ignore unknown resources
		}

		private void CancelSelection()
		{
			_isPlacingMarkers = false;
			ClearMarkers();
			Player.m_localPlayer.Message(MessageHud.MessageType.Center, "Blueprint selection cancelled");
			DBG.blogInfo("[BlueprintSelector] Selection cancelled");
		}

		private void FinishSelection()
		{
			_isPlacingMarkers = false;
			ClearMarkers();
		}

		private void ClearMarkers()
		{
			foreach (var marker in _markers)
			{
				if (marker != null) Destroy(marker);
			}
			_markers.Clear();

			if (_previewBox != null)
			{
				Destroy(_previewBox);
				_previewBox = null;
			}

			_heightAdjust = 0f;
		}
	}
}
