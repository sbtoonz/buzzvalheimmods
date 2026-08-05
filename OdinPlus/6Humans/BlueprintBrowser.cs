using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace OdinPlus
{
	/// <summary>
	/// Blueprint browser UI using AssetBundle prefab (PlanBuild pattern).
	/// Instantiates custom-built UI instead of cloning vanilla windows.
	/// </summary>
	public class BlueprintBrowser : MonoBehaviour, TextReceiver
	{
		private static BlueprintBrowser _instance;

		public static bool IsVisible => _instance != null && _instance._root != null && _instance._root.activeSelf;
		public static string ArmedBlueprintName { get; private set; }

		private GameObject _canvas; // Canvas root - always stays active
		private GameObject _root;   // Window - what we show/hide
		private RectTransform _iconListRoot;
		private GameObject _iconTemplate;
		private GameObject _blueprintsTab;
		private GameObject _createTab;
		private readonly List<GameObject> _icons = new List<GameObject>();
		private bool _built;
		private bool _showingCreate = false;

		public static void Toggle()
		{
			EnsureInstance();
			_instance.ToggleInternal();
		}

		public static void Hide()
		{
			if (_instance != null) _instance.HideInternal();
		}

		public static void Init()
		{
			EnsureInstance();
		}

		private static void EnsureInstance()
		{
			if (_instance != null) return;
			var go = new GameObject("OdinPlusBlueprintBrowser");
			DontDestroyOnLoad(go);
			_instance = go.AddComponent<BlueprintBrowser>();
		}

		private void Update()
		{
			bool blueprintToolActive = false;
			if (Player.m_localPlayer != null && Player.m_localPlayer.InPlaceMode())
			{
				Player.m_localPlayer.GetBuildSelection(out _, out _, out _, out _, out PieceTable currentTable);
				blueprintToolActive = currentTable == OdinItem.BlueprintToolPieceTable;
			}

			// Update cached flag for the suppressor (one check per frame, not per SetActive call)
			BuildHudSuppressor.BlueprintToolActive = blueprintToolActive;

			// Disable vanilla BuildHud children when blueprint tool active
			if (blueprintToolActive && Hud.instance != null && Hud.instance.m_buildHud != null)
			{
				var bh = Hud.instance.m_buildHud.transform;
				for (int i = 0; i < bh.childCount; i++)
				{
					var child = bh.GetChild(i);
					if (child.name == "BlueprintBrowserGUI") continue;
					if (child.gameObject.activeSelf)
						child.gameObject.SetActive(false);
				}
			}

			if (!_built || _root == null || !_root.activeSelf) return;

			if (!blueprintToolActive)
			{
				HideInternal();
			}
		}

		private void ToggleInternal()
		{
			if (!_built && !TryBuildFromAssetBundle())
			{
				DBG.blogWarning("[BlueprintBrowser] Failed to build UI from AssetBundle");
				return;
			}

			if (_root.activeSelf) HideInternal();
			else ShowInternal();
		}

		private void ShowInternal()
		{
			if (_canvas != null && !_canvas.activeSelf)
				_canvas.SetActive(true);

			RefreshList();
			_root.SetActive(true);

			// Disable ValheimRadial (blocks clicks)
			var radial = GameObject.Find("ValheimRadial");
			if (radial != null) radial.SetActive(false);
		}

		private void HideInternal()
		{
			if (_root != null) _root.SetActive(false);

			// Re-enable all vanilla BuildHud children
			if (Hud.instance != null && Hud.instance.m_buildHud != null)
			{
				var bh = Hud.instance.m_buildHud.transform;
				for (int i = 0; i < bh.childCount; i++)
				{
					var child = bh.GetChild(i);
					if (child.name == "BlueprintBrowserGUI") continue;
					if (!child.gameObject.activeSelf)
						child.gameObject.SetActive(true);
				}
			}

			// Re-enable ValheimRadial
			var radial = GameObject.Find("ValheimRadial");
			if (radial != null) radial.SetActive(true);
		}

		/// <summary>
		/// Instantiate UI from AssetBundle prefab (PlanBuild pattern)
		/// </summary>
		private bool TryBuildFromAssetBundle()
		{
			if (BlueprintBrowserAssets.BlueprintBrowserPrefab == null)
			{
				DBG.blogError("[BlueprintBrowser] BlueprintBrowserGUI prefab not loaded");
				return false;
			}

			// Instantiate the prefab (Canvas root)
			var prefabInstance = Instantiate(BlueprintBrowserAssets.BlueprintBrowserPrefab);
			prefabInstance.name = "BlueprintBrowserGUI";

			// Parent to BuildHud (our suppressor will keep OTHER children disabled)
			if (Hud.instance != null && Hud.instance.m_buildHud != null)
			{
				prefabInstance.transform.SetParent(Hud.instance.m_buildHud.transform, false);
			}
			else
			{
				DontDestroyOnLoad(prefabInstance);
			}

			// Setup Canvas
			var canvas = prefabInstance.GetComponent<Canvas>();
			if (canvas != null)
			{
				canvas.sortingOrder = 100; // Above other elements
			}

			// Find the Window child (our main panel)
			var window = prefabInstance.transform.Find("Window");
			if (window == null)
			{
				DBG.blogError("[BlueprintBrowser] Window not found in prefab");
				Destroy(prefabInstance);
				return false;
			}

			// Store both Canvas and Window
			_canvas = prefabInstance; // Canvas - always stays active
			_root = window.gameObject; // Window - what we show/hide

			// Keep Canvas always active
			_canvas.SetActive(true);

			// Fix Window background - make it transparent or it renders as black circle
			var windowImg = window.GetComponent<Image>();
			if (windowImg != null && windowImg.sprite == null)
			{
				windowImg.color = new Color(0.2f, 0.15f, 0.1f, 0.95f); // Semi-transparent brown
			}

			// Apply Valheim materials (PlanBuild pattern - makes it look native!)
			ApplyValheimMaterials(window);

			// Find tabs
			var tabs = window.Find("Tabs");
			if (tabs != null)
			{
				_blueprintsTab = tabs.Find("BlueprintsTab")?.gameObject;
				_createTab = tabs.Find("CreateTab")?.gameObject;

				// Wire up button listeners
				if (_blueprintsTab != null)
				{
					var btn = _blueprintsTab.GetComponent<Button>();
					if (btn != null)
					{
						btn.onClick.RemoveAllListeners();
						btn.onClick.AddListener(ShowBlueprintsTab);
					}
				}

				if (_createTab != null)
				{
					var btn = _createTab.GetComponent<Button>();
					if (btn != null)
					{
						btn.onClick.RemoveAllListeners();
						btn.onClick.AddListener(ShowCreateTab);
					}
				}
			}

			// Find content/viewport/icon list (v2 prefab structure)
			var content = window.Find("Content");
			if (content != null)
			{
				var viewport = content.Find("Viewport");
				if (viewport != null)
				{
					_iconListRoot = viewport.Find("IconList") as RectTransform;
				}
			}

			if (_iconListRoot == null)
			{
				DBG.blogError("[BlueprintBrowser] IconList not found at Content/Viewport/IconList");
				_iconListRoot = content as RectTransform; // Fallback to content itself
			}
			else
			{
				DBG.blogInfo($"[BlueprintBrowser] Found IconList at Content/Viewport/IconList, rect: {_iconListRoot.rect}, childCount: {_iconListRoot.childCount}");

				// Find and store the IconTemplate
				var template = _iconListRoot.Find("IconTemplate");
				if (template != null)
				{
					_iconTemplate = template.gameObject;
					DBG.blogInfo($"[BlueprintBrowser] Found IconTemplate in prefab, active: {template.gameObject.activeSelf}, childCount: {template.childCount}");
				}
				else
				{
					DBG.blogError("[BlueprintBrowser] IconTemplate NOT found in IconList - listing children:");
					for (int i = 0; i < _iconListRoot.childCount; i++)
					{
						DBG.blogError($"  Child {i}: {_iconListRoot.GetChild(i).name}");
					}
				}
			}

			_root.SetActive(false);
			_built = true;

			DBG.blogInfo("[BlueprintBrowser] UI built from AssetBundle successfully");
			return true;
		}

		private void ShowBlueprintsTab()
		{
			_showingCreate = false;
			RefreshList();
		}

		private void ShowCreateTab()
		{
			_showingCreate = true;
			BlueprintPlacer.Clear();
			RefreshList();
		}

		private void RefreshList()
		{
			foreach (var icon in _icons) Destroy(icon);
			_icons.Clear();

			if (_iconListRoot == null) return;

			if (_showingCreate)
			{
				ShowCreateMode();
			}
			else
			{
				ShowBlueprintsList();
			}
		}

		private void ShowBlueprintsList()
		{
			var blueprints = BlueprintConfig.GetAllBlueprints();
			DBG.blogInfo($"[BlueprintBrowser] Showing {blueprints.Count} blueprints");

			// Use IconTemplate from prefab
			if (_iconTemplate == null)
			{
				DBG.blogError("[BlueprintBrowser] IconTemplate not found in prefab - cannot show icons");
				return;
			}

			DBG.blogInfo($"[BlueprintBrowser] Using IconTemplate, creating {blueprints.Count} icons");

			for (int i = 0; i < blueprints.Count; i++)
			{
				var bp = blueprints[i];

				// Clone template
				var iconGo = Instantiate(_iconTemplate, _iconListRoot);
				iconGo.SetActive(true);
				iconGo.name = $"Icon_{bp.name}";

				// Set icon sprite
				var iconImg = iconGo.transform.Find("Icon")?.GetComponent<Image>();
				if (iconImg != null)
				{
					var sprite = BlueprintIconRenderer.GetIcon(bp);
					iconImg.sprite = sprite;

					if (iconImg.rectTransform != null)
					{
						DBG.blogInfo($"[BlueprintBrowser] Icon {i}: {bp.name}, sprite={(sprite != null ? sprite.name : "NULL")}, rect={iconImg.rectTransform.rect}, active={iconImg.gameObject.activeSelf}");
					}
				}
				else
				{
					DBG.blogError($"[BlueprintBrowser] Icon {i}: Could not find Icon child in template");
				}

				// Wire up click handler
				string capturedName = bp.name;
				var btn = iconGo.GetComponent<Button>();
				if (btn != null)
				{
					btn.onClick.RemoveAllListeners();
					btn.onClick.AddListener(() => OnBlueprintClicked(capturedName));
				}

				_icons.Add(iconGo);
			}
		}

		private void ShowCreateMode()
		{
			DBG.blogInfo("[BlueprintBrowser] Showing Create mode");

			if (Hud.instance == null || Hud.instance.m_pieceIconPrefab == null) return;

			// Create button
			var buttonGo = Instantiate(Hud.instance.m_pieceIconPrefab, _iconListRoot);
			buttonGo.SetActive(true);
			// GridLayoutGroup handles positioning

			var icon = buttonGo.transform.Find("icon")?.GetComponent<Image>();
			if (icon != null)
			{
				icon.enabled = true;
				icon.sprite = OdinPlus.CoinsIcon;
				icon.color = Color.green;
				icon.raycastTarget = true;
			}

			var tooltip = buttonGo.GetComponent<UITooltip>();
			if (tooltip != null) tooltip.m_text = "Create Blueprint\n\nClick to start visual selection";

			buttonGo.transform.Find("selected")?.gameObject.SetActive(false);
			buttonGo.transform.Find("upgrade")?.gameObject.SetActive(false);

			var btn = buttonGo.GetComponent<Button>();
			if (btn != null)
			{
				btn.onClick.RemoveAllListeners();
				btn.onClick.AddListener(StartVisualSelection);
			}

			_icons.Add(buttonGo);
		}

		private void StartVisualSelection()
		{
			if (Player.m_localPlayer == null)
			{
				DBG.blogWarning("[BlueprintBrowser] No local player");
				return;
			}

			DBG.blogInfo("[BlueprintBrowser] Starting visual selection");
			HideInternal();
			TextInput.instance.RequestText(this, "$op_blueprint_name", 20);
		}

		// TextReceiver implementation
		public string GetText()
		{
			return "";
		}

		public void SetText(string text)
		{
			if (string.IsNullOrWhiteSpace(text))
			{
				if (Player.m_localPlayer != null)
					Player.m_localPlayer.Message(MessageHud.MessageType.Center, "Blueprint name cannot be empty");
				return;
			}

			BlueprintSelector.Instance.StartSelection(text);
			HideInternal();
		}

		private static string CostSummary(Blueprint bp)
		{
			var parts = new List<string>();
			foreach (var cost in bp.resourceCosts)
				parts.Add(cost.Value + " " + cost.Key);
			return parts.Count > 0 ? string.Join(", ", parts) : "free";
		}

		private void OnBlueprintClicked(string blueprintName)
		{
			ArmedBlueprintName = blueprintName;
			BlueprintPlacer.SetArmed(blueprintName);
			if (Player.m_localPlayer != null)
				Player.m_localPlayer.Message(MessageHud.MessageType.Center, "Blueprint armed: " + blueprintName + " - click to place, right-click to cancel");
			HideInternal();
		}

		/// <summary>
		/// Apply Valheim's native materials to make UI look like vanilla (PlanBuild pattern)
		/// </summary>
		private void ApplyValheimMaterials(Transform window)
		{
			var litpanel = FindLitPanelMaterial();

			// Apply litpanel to main window background
			var windowImg = window.GetComponent<Image>();
			if (windowImg != null && litpanel != null)
			{
				windowImg.material = litpanel;
				windowImg.color = new Color(0.7f, 0.6f, 0.5f, 1f); // Warm wood tone
			}

			// Apply to tab buttons
			var tabs = window.Find("Tabs");
			if (tabs != null)
			{
				foreach (Transform tab in tabs)
				{
					var tabImg = tab.GetComponent<Image>();
					if (tabImg != null && litpanel != null)
					{
						tabImg.material = litpanel;
						tabImg.color = new Color(0.5f, 0.4f, 0.3f, 1f); // Darker wood for tabs
					}
				}
			}

			// Apply to content background
			var content = window.Find("Content");
			if (content != null)
			{
				var contentImg = content.GetComponent<Image>();
				if (contentImg != null && litpanel != null)
				{
					contentImg.material = litpanel;
					contentImg.color = new Color(0.3f, 0.25f, 0.2f, 0.9f); // Very dark for contrast
				}
			}

			DBG.blogInfo($"[BlueprintBrowser] Applied Valheim materials (litpanel={litpanel != null})");
		}

		/// <summary>
		/// Find Valheim's litpanel material (same as FactionGui)
		/// </summary>
		private static Material FindLitPanelMaterial()
		{
			var materials = Resources.FindObjectsOfTypeAll<Material>();
			foreach (var mat in materials)
			{
				if (mat != null && mat.name.IndexOf("litpanel", StringComparison.OrdinalIgnoreCase) >= 0)
					return mat;
			}
			return null;
		}
	}
}
