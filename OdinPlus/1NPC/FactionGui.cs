using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace OdinPlus
{
	// F7 faction reputation overlay - documented extensively (CLAUDE.md/README.md: "Press F7 to see
	// your reputation with all factions") but the code implementing it never actually existed anywhere
	// in the project. Built from scratch to match that documented spec.
	public class FactionGui : MonoBehaviour
	{
		#region Fields

		static FactionGui _instance;

		GameObject _root;
		RectTransform _panelRect;
		RectTransform _listRoot;
		Font _font;
		readonly List<GameObject> _rows = new();
		bool _isDragging = false;
		Vector2 _dragOffset;

		#endregion Fields

		#region Init

		public static void Init()
		{
			if(_instance != null) return;
			var go = new GameObject("OdinPlusFactionGui");
			DontDestroyOnLoad(go);
			_instance = go.AddComponent<FactionGui>();
		}

		#endregion Init

		#region Mono

		void Update()
		{
			if(Player.m_localPlayer == null) return;

			// Toggle visibility with F7
			if(Input.GetKeyDown(KeyCode.F7))
			{
				if(_root == null) BuildUI();
				var show = !_root.activeSelf;
				_root.SetActive(show);
				if(show) RefreshList();
			}

			// Handle dragging
			if(_root != null && _root.activeSelf && _panelRect != null)
			{
				if(Input.GetMouseButtonDown(0))
				{
					Vector2 mousePos = Input.mousePosition;
					if(RectTransformUtility.RectangleContainsScreenPoint(_panelRect, mousePos))
					{
						RectTransformUtility.ScreenPointToLocalPointInRectangle(
							_panelRect.parent as RectTransform,
							mousePos,
							null,
							out var localPoint);
						_dragOffset = localPoint - _panelRect.anchoredPosition;
						_isDragging = true;
					}
				}

				if(Input.GetMouseButtonUp(0))
					_isDragging = false;

				if(_isDragging)
				{
					RectTransformUtility.ScreenPointToLocalPointInRectangle(
						_panelRect.parent as RectTransform,
						Input.mousePosition,
						null,
						out var localPoint);
					_panelRect.anchoredPosition = localPoint - _dragOffset;
				}
			}
		}

		#endregion Mono

		#region UI Logic

		void RefreshList()
		{
			foreach(var row in _rows) Destroy(row);
			_rows.Clear();

			var playerID = Player.m_localPlayer.GetZDOID().ToString();
			var y = 0f;
			foreach(var factionName in FactionManager.GetAllFactions())
			{
				var rep = FactionManager.GetReputation(playerID, factionName);
				var tier = FactionManager.GetReputationTier(playerID, factionName);
				AddRow($"{factionName}: {tier} ({rep})", TierColor(tier), y);
				y -= 26f;
			}
		}

		static Color TierColor(ReputationTier tier)
		{
			switch(tier)
			{
				case ReputationTier.Hostile: return new Color(0.9f, 0.25f, 0.25f);
				case ReputationTier.Unfriendly: return new Color(0.9f, 0.6f, 0.2f);
				case ReputationTier.Friendly: return new Color(0.4f, 0.9f, 0.4f);
				case ReputationTier.Honored: return new Color(0.3f, 0.7f, 1f);
				default: return Color.white;
			}
		}

		void AddRow(string text, Color color, float y)
		{
			var row = new GameObject("Row").AddComponent<Text>();
			row.transform.SetParent(_listRoot, false);
			row.font = _font;
			row.fontSize = 18;
			row.color = color;
			row.alignment = TextAnchor.MiddleLeft;
			row.text = text;
			var rect = row.rectTransform;
			rect.anchorMin = new Vector2(0f, 1f);
			rect.anchorMax = new Vector2(1f, 1f);
			rect.pivot = new Vector2(0f, 1f);
			rect.anchoredPosition = new Vector2(16f, y);
			rect.sizeDelta = new Vector2(-32f, 24f);

			_rows.Add(row.gameObject);
		}

		void BuildUI()
		{
			_font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

			var canvasGo = new GameObject("Canvas");
			canvasGo.transform.SetParent(transform, false);
			var canvas = canvasGo.AddComponent<Canvas>();
			canvas.renderMode = RenderMode.ScreenSpaceOverlay;
			canvas.sortingOrder = 100;
			var scaler = canvasGo.AddComponent<CanvasScaler>();
			scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
			scaler.referenceResolution = new Vector2(1920f, 1080f);
			canvasGo.AddComponent<GraphicRaycaster>();

			_root = new GameObject("Panel");
			_root.transform.SetParent(canvasGo.transform, false);
			_panelRect = _root.AddComponent<RectTransform>();
			// Position above minimap (top-right, but below top edge to make room for minimap)
			_panelRect.anchorMin = new Vector2(1f, 1f);
			_panelRect.anchorMax = new Vector2(1f, 1f);
			_panelRect.pivot = new Vector2(1f, 1f);
			_panelRect.sizeDelta = new Vector2(320f, 220f);
			_panelRect.anchoredPosition = new Vector2(-20f, -280f); // Below minimap (minimap is ~260px)

			var panelBg = _root.AddComponent<Image>();
			var litpanel = FindLitPanelMaterial();
			if(litpanel != null)
			{
				panelBg.material = litpanel;
				panelBg.color = Color.white;
			}
			else
			{
				panelBg.color = new Color(0.05f, 0.05f, 0.05f, 0.85f);
			}

			var title = new GameObject("Title").AddComponent<Text>();
			title.transform.SetParent(_root.transform, false);
			title.font = _font;
			title.fontSize = 20;
			title.fontStyle = FontStyle.Bold;
			title.color = Color.white;
			title.alignment = TextAnchor.MiddleCenter;
			title.text = "Faction Reputation";
			var titleRect = title.rectTransform;
			titleRect.anchorMin = new Vector2(0f, 1f);
			titleRect.anchorMax = new Vector2(1f, 1f);
			titleRect.pivot = new Vector2(0.5f, 1f);
			titleRect.sizeDelta = new Vector2(0f, 32f);
			titleRect.anchoredPosition = Vector2.zero;

			var listGo = new GameObject("List");
			listGo.transform.SetParent(_root.transform, false);
			_listRoot = listGo.AddComponent<RectTransform>();
			_listRoot.anchorMin = new Vector2(0f, 0f);
			_listRoot.anchorMax = new Vector2(1f, 1f);
			_listRoot.offsetMin = new Vector2(0f, 22f);
			_listRoot.offsetMax = new Vector2(0f, -36f);

			var hint = new GameObject("Hint").AddComponent<Text>();
			hint.transform.SetParent(_root.transform, false);
			hint.font = _font;
			hint.fontSize = 14;
			hint.color = new Color(1f, 1f, 1f, 0.6f);
			hint.alignment = TextAnchor.LowerCenter;
			hint.text = "[F7] Close";
			var hintRect = hint.rectTransform;
			hintRect.anchorMin = new Vector2(0f, 0f);
			hintRect.anchorMax = new Vector2(1f, 0f);
			hintRect.pivot = new Vector2(0.5f, 0f);
			hintRect.sizeDelta = new Vector2(0f, 20f);
			hintRect.anchoredPosition = Vector2.zero;

			_root.SetActive(false);
		}

		static Material FindLitPanelMaterial()
		{
			var materials = Resources.FindObjectsOfTypeAll<Material>();
			foreach(var mat in materials)
			{
				if(mat != null && mat.name.IndexOf("litpanel", StringComparison.OrdinalIgnoreCase) >= 0)
					return mat;
			}
			return null;
		}

		#endregion UI Logic
	}
}
