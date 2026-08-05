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
		// PlanBuild's Blueprint Rune offers 3 selection tools (circle-radius marker, connected-piece
		// flood fill, and click-to-add/remove) - Box mode below is our original 2-corner cuboid, Radius
		// and FloodFill mirror those two additional PlanBuild tools.
		private enum SelectionMode { Box, Radius, FloodFill }

		private static BlueprintSelector _instance;
		private List<GameObject> _markers = new List<GameObject>();
		private GameObject _previewBox;
		private bool _isPlacingMarkers = false;
		public static bool IsActive => _instance != null && _instance._isPlacingMarkers;
		private string _blueprintName = "";
		private float _heightAdjust = 0f;
		private float _lastMessageTime = 0f;
		private SelectionMode _mode = SelectionMode.Box;

		// Radius mode state
		private Vector3? _radiusCenter = null;
		private float _radius = 5f;
		private GameObject _radiusPreviewSphere;

		// FloodFill mode state
		private readonly HashSet<Piece> _floodSelection = new HashSet<Piece>();
		private readonly Dictionary<Renderer, Material[]> _originalMaterials = new Dictionary<Renderer, Material[]>();
		private Material _highlightMat;
		private float _floodMargin = 0.15f;

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

			if (Input.GetKeyDown(KeyCode.Escape))
			{
				CancelSelection();
				return;
			}

			// Only allow switching tools while nothing is mid-selection, matching PlanBuild's "Toggle"
			// key convention (their default is Q; Tab is used here since Q already has other bindings
			// on OdinPlus's other tools).
			if (Input.GetKeyDown(KeyCode.Tab) && _markers.Count == 0 && _floodSelection.Count == 0 && _radiusCenter == null)
			{
				CycleMode();
				return;
			}

			switch (_mode)
			{
				case SelectionMode.Box: UpdateBoxMode(); break;
				case SelectionMode.Radius: UpdateRadiusMode(); break;
				case SelectionMode.FloodFill: UpdateFloodFillMode(); break;
			}
		}

		private void CycleMode()
		{
			_mode = (SelectionMode)(((int)_mode + 1) % 3);
			Player.m_localPlayer.Message(MessageHud.MessageType.Center, $"Selection tool: {_mode}");
			DBG.blogInfo($"[BlueprintSelector] Switched to {_mode} mode");
		}

		private bool TryRaycastMouse(out RaycastHit hit)
		{
			Ray ray = GameCamera.instance.GetComponent<Camera>().ScreenPointToRay(Input.mousePosition);
			return Physics.Raycast(ray, out hit, 100f);
		}

		private void UpdateBoxMode()
		{
			// Show instructions (throttled to every 0.5s to avoid spam)
			if (Time.time - _lastMessageTime > 0.5f)
			{
				_lastMessageTime = Time.time;

				string instructions = "";
				if (_markers.Count == 0)
				{
					instructions = $"Blueprint: {_blueprintName} | Box mode ([Tab] switch tool)\n[LMB] Place Corner 1 | [Esc] Cancel";
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

		private void UpdateRadiusMode()
		{
			if (Time.time - _lastMessageTime > 0.5f)
			{
				_lastMessageTime = Time.time;
				string instructions = _radiusCenter == null
					? $"Blueprint: {_blueprintName} | Radius mode ([Tab] switch tool)\n[LMB] Place Center | [Esc] Cancel"
					: $"Blueprint: {_blueprintName} | Radius: {_radius:F1}m\n[Scroll] Adjust Radius | [LMB] Confirm | [RMB] Reset | [Esc] Cancel";
				Player.m_localPlayer.Message(MessageHud.MessageType.Center, instructions);
			}

			if (_radiusCenter == null)
			{
				if (Input.GetMouseButtonDown(0) && GUIUtility.hotControl == 0 && TryRaycastMouse(out var hit))
				{
					_radiusCenter = hit.point;
					_radius = 5f;
					UpdateRadiusPreview();
				}
				return;
			}

			float scroll = Input.GetAxis("Mouse ScrollWheel");
			if (scroll != 0f)
			{
				_radius = Mathf.Clamp(_radius + scroll * 2f, 1f, 100f);
				UpdateRadiusPreview();
			}

			if (Input.GetMouseButtonDown(1))
			{
				if (_radiusPreviewSphere != null) { Destroy(_radiusPreviewSphere); _radiusPreviewSphere = null; }
				_radiusCenter = null;
				return;
			}

			if (Input.GetMouseButtonDown(0) && GUIUtility.hotControl == 0)
			{
				ConfirmRadiusSelection();
			}
		}

		private void UpdateRadiusPreview()
		{
			if (_radiusCenter == null) return;

			if (_radiusPreviewSphere == null)
			{
				_radiusPreviewSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
				_radiusPreviewSphere.name = "BlueprintRadiusPreview";
				var col = _radiusPreviewSphere.GetComponent<Collider>();
				if (col != null) Destroy(col);
				var renderer = _radiusPreviewSphere.GetComponent<Renderer>();
				if (renderer != null)
				{
					var mat = Util.CreateGhostMaterial(new Color(0f, 0.8f, 1f, 0.25f));
					if (mat != null) renderer.material = mat;
				}
			}

			_radiusPreviewSphere.transform.position = _radiusCenter.Value;
			_radiusPreviewSphere.transform.localScale = Vector3.one * _radius * 2f;
		}

		private void ConfirmRadiusSelection()
		{
			if (_radiusCenter == null) return;

			var pieces = Physics.OverlapSphere(_radiusCenter.Value, _radius)
				.Select(c => c.GetComponentInParent<Piece>())
				.Where(p => p != null && p.gameObject.activeInHierarchy)
				.Distinct()
				.OrderBy(p => p.transform.position.y)
				.ThenBy(p => p.transform.position.x)
				.ToList();

			Vector3 origin = _radiusCenter.Value;
			if (pieces.Count > 0) origin.y = pieces[0].transform.position.y;

			if (_radiusPreviewSphere != null) { Destroy(_radiusPreviewSphere); _radiusPreviewSphere = null; }
			_radiusCenter = null;

			FinalizeScan(pieces, origin);
		}

		private void UpdateFloodFillMode()
		{
			if (Time.time - _lastMessageTime > 0.5f)
			{
				_lastMessageTime = Time.time;
				Player.m_localPlayer.Message(MessageHud.MessageType.Center,
					$"Blueprint: {_blueprintName} | FloodFill mode ([Tab] switch tool) | Selected: {_floodSelection.Count}\n" +
					"[LMB] Add connected | [Alt+LMB] Remove piece | [Ctrl+Scroll] Margin | [Enter] Confirm | [Esc] Cancel");
			}

			if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
			{
				float scroll = Input.GetAxis("Mouse ScrollWheel");
				if (scroll != 0f) _floodMargin = Mathf.Clamp(_floodMargin + scroll * 0.05f, 0.01f, 1f);
			}

			if ((Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)) && _floodSelection.Count > 0)
			{
				ConfirmFloodFillSelection();
				return;
			}

			if (Input.GetMouseButtonDown(0) && GUIUtility.hotControl == 0 && TryRaycastMouse(out var hit))
			{
				var piece = hit.collider.GetComponentInParent<Piece>();
				if (piece == null) return;

				bool removing = Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);
				if (removing)
				{
					if (_floodSelection.Remove(piece)) HighlightPiece(piece, false);
				}
				else if (!_floodSelection.Contains(piece))
				{
					FloodFillAdd(piece);
				}
			}
		}

		private Bounds GetPieceBounds(Piece p)
		{
			var cols = p.GetComponentsInChildren<Collider>();
			if (cols.Length == 0) return new Bounds(p.transform.position, Vector3.one * 0.5f);

			Bounds b = cols[0].bounds;
			for (int i = 1; i < cols.Length; i++) b.Encapsulate(cols[i].bounds);
			return b;
		}

		private void FloodFillAdd(Piece seed)
		{
			var queue = new Queue<Piece>();
			_floodSelection.Add(seed);
			HighlightPiece(seed, true);
			queue.Enqueue(seed);

			// Bounded BFS over physically-overlapping piece colliders - mirrors PlanBuild's "connected check
			// margin" flood fill (their config's default margin is 0.01; guard caps worst-case cost on huge builds).
			int guard = 0;
			while (queue.Count > 0 && guard < 2000)
			{
				guard++;
				var current = queue.Dequeue();
				var bounds = GetPieceBounds(current);
				bounds.Expand(_floodMargin);

				foreach (var col in Physics.OverlapBox(bounds.center, bounds.extents, Quaternion.identity))
				{
					var other = col.GetComponentInParent<Piece>();
					if (other == null || !other.gameObject.activeInHierarchy || _floodSelection.Contains(other)) continue;

					_floodSelection.Add(other);
					HighlightPiece(other, true);
					queue.Enqueue(other);
				}
			}
		}

		private void HighlightPiece(Piece p, bool on)
		{
			if (p == null) return;
			if (_highlightMat == null) _highlightMat = Util.CreateGhostMaterial(new Color(0.2f, 1f, 0.2f, 0.55f));
			if (_highlightMat == null) return;

			foreach (var r in p.GetComponentsInChildren<Renderer>(true))
			{
				if (on)
				{
					if (!_originalMaterials.ContainsKey(r)) _originalMaterials[r] = r.sharedMaterials;
					var mats = new Material[r.sharedMaterials.Length];
					for (int i = 0; i < mats.Length; i++) mats[i] = _highlightMat;
					r.sharedMaterials = mats;
				}
				else if (_originalMaterials.TryGetValue(r, out var original))
				{
					r.sharedMaterials = original;
					_originalMaterials.Remove(r);
				}
			}
		}

		private void ClearFloodSelection()
		{
			foreach (var p in _floodSelection.ToList()) HighlightPiece(p, false);
			_floodSelection.Clear();
		}

		private void ConfirmFloodFillSelection()
		{
			if (_floodSelection.Count == 0) return;

			var pieces = _floodSelection
				.OrderBy(p => p.transform.position.y)
				.ThenBy(p => p.transform.position.x)
				.ToList();
			Vector3 origin = pieces[0].transform.position;

			foreach (var p in _floodSelection.ToList()) HighlightPiece(p, false);
			_floodSelection.Clear();

			FinalizeScan(pieces, origin);
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
			ClearFloodSelection();
			if (_radiusPreviewSphere != null) { Destroy(_radiusPreviewSphere); _radiusPreviewSphere = null; }
			_radiusCenter = null;
			_mode = SelectionMode.Box; // default - keeps existing console commands (scanblueprint etc.) behaving the same

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

				// Style it - use a shader guaranteed present in Valheim's build (see Util.CreateGhostMaterial)
				// instead of poking Standard-only properties on the default primitive material, which is what
				// caused these markers/box to render pink.
				var renderer = marker.GetComponent<Renderer>();
				if (renderer != null)
				{
					Color markerColor = _markers.Count == 0 ? Color.green : Color.red;
					var ghostMat = Util.CreateGhostMaterial(markerColor);
					if (ghostMat != null) renderer.material = ghostMat;
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

			// Make it transparent cyan using a shader guaranteed present in Valheim's build (see
			// Util.CreateGhostMaterial) - the old code assumed Built-in-RP/Standard shader property names
			// (_SrcBlend, _ALPHABLEND_ON, etc.) which render pink if that shader is stripped from the build.
			var renderer = box.GetComponent<Renderer>();
			if (renderer != null)
			{
				Color cyan = new Color(0f, 0.8f, 1f, 0.4f);
				var mat = Util.CreateGhostMaterial(cyan);
				if (mat != null)
				{
					renderer.material = mat;
					DBG.blogInfo($"[BlueprintSelector] Created transparent preview box with shader: {mat.shader.name}");
				}
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

			// Find all pieces in bounds - use radius scan then filter to box
			float scanRadius = (max - min).magnitude / 2f + 5f;
			var tempPieces = new List<Piece>();
			Piece.GetAllPiecesInRadius(center, scanRadius, tempPieces);
			var piecesInBounds = tempPieces
				.Where(p => p.gameObject.activeInHierarchy)
				.Where(p => IsInBounds(p.transform.position, min, max))
				.OrderBy(p => p.transform.position.y)
				.ThenBy(p => p.transform.position.x)
				.ToList();

			// Use center of bounds as origin
			Vector3 origin = center;
			if (piecesInBounds.Count > 0) origin.y = piecesInBounds[0].transform.position.y; // Match ground level

			FinalizeScan(piecesInBounds, origin);
		}

		/// <summary>
		/// Shared by all three selection tools (Box/Radius/FloodFill): computes resource costs from the
		/// given pieces, exports the YAML blueprint, and reports/cleans up.
		/// </summary>
		private void FinalizeScan(List<Piece> piecesInBounds, Vector3 origin)
		{
			if (piecesInBounds.Count == 0)
			{
				Player.m_localPlayer.Message(MessageHud.MessageType.Center,
					"No pieces found in selection!");
				DBG.blogWarning("[BlueprintSelector] No pieces in selection");
				CancelSelection();
				return;
			}

			Player.m_localPlayer.Message(MessageHud.MessageType.Center,
				$"Scanning {piecesInBounds.Count} pieces...");

			// Calculate actual costs from piece requirements - keyed by the resource's real prefab
			// name (not a hardcoded whitelist) so any vanilla or modded resource is captured distinctly.
			Dictionary<string, int> totalCosts = new Dictionary<string, int>();
			foreach (var piece in piecesInBounds)
			{
				// Get the actual piece requirements
				if (piece.m_resources != null)
				{
					foreach (var req in piece.m_resources)
					{
						if (req.m_resItem == null) continue;

						string key = req.m_resItem.gameObject.name;
						int amount = req.m_amount;

						if (!totalCosts.ContainsKey(key))
							totalCosts[key] = 0;
						totalCosts[key] += amount;
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

		private void CancelSelection()
		{
			_isPlacingMarkers = false;
			ClearMarkers();
			ClearFloodSelection();
			if (_radiusPreviewSphere != null) { Destroy(_radiusPreviewSphere); _radiusPreviewSphere = null; }
			_radiusCenter = null;
			Player.m_localPlayer.Message(MessageHud.MessageType.Center, "Blueprint selection cancelled");
			DBG.blogInfo("[BlueprintSelector] Selection cancelled");
		}

		private void FinishSelection()
		{
			_isPlacingMarkers = false;
			ClearMarkers();
			ClearFloodSelection();
			if (_radiusPreviewSphere != null) { Destroy(_radiusPreviewSphere); _radiusPreviewSphere = null; }
			_radiusCenter = null;
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
