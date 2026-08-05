using System.Collections.Generic;
using UnityEngine;

namespace OdinPlus
{
	public static class BlueprintIconRenderer
	{
		private static Dictionary<string, Sprite> _cache = new Dictionary<string, Sprite>();
		private static readonly Vector3 StagePos = new Vector3(0f, -500f, 0f);
		private const int IconSize = 128;

		public static void ClearCache() => _cache.Clear();

		public static Sprite GetIcon(Blueprint bp)
		{
			if (bp == null || bp.pieces == null || bp.pieces.Length == 0)
			{
				DBG.blogWarning($"[BlueprintIconRenderer] GetIcon: Invalid blueprint");
				return null;
			}
			if (_cache.TryGetValue(bp.name, out var cached))
			{
				DBG.blogInfo($"[BlueprintIconRenderer] GetIcon: Using cached sprite for '{bp.name}'");
				return cached;
			}

			DBG.blogInfo($"[BlueprintIconRenderer] GetIcon: Rendering new sprite for '{bp.name}' with {bp.pieces.Length} pieces");
			var sprite = RenderBlueprint(bp);
			if (sprite != null)
			{
				_cache[bp.name] = sprite;
				DBG.blogInfo($"[BlueprintIconRenderer] GetIcon: Successfully rendered '{bp.name}'");
			}
			else
			{
				DBG.blogWarning($"[BlueprintIconRenderer] GetIcon: Failed to render '{bp.name}'");
			}
			return sprite;
		}

		private static Sprite RenderBlueprint(Blueprint bp)
		{
			var spawned = new List<GameObject>();

			foreach (var piece in bp.pieces)
			{
				var prefab = ZNetScene.instance != null ? ZNetScene.instance.GetPrefab(piece.prefabName) : null;
				if (prefab == null) continue;

				ZNetView.m_forceDisableInit = true;
				var go = Object.Instantiate(prefab, StagePos + piece.localPosition, Quaternion.Euler(piece.rotation));
				ZNetView.m_forceDisableInit = false;

				foreach (var znv in go.GetComponentsInChildren<ZNetView>(true))
				{
					Object.DestroyImmediate(znv);
				}

				// Strip physics/gameplay components
				StripNetworkingComponents(go);
				spawned.Add(go);
			}

			if (spawned.Count == 0) return null;

			// Compute bounds
			var bounds = new Bounds(spawned[0].transform.position, Vector3.zero);
			foreach (var go in spawned)
			{
				foreach (var r in go.GetComponentsInChildren<Renderer>())
					bounds.Encapsulate(r.bounds);
			}

			// Setup camera
			var camGo = new GameObject("_BlueprintIconCam");
			var cam = camGo.AddComponent<Camera>();
			cam.clearFlags = CameraClearFlags.SolidColor;
			cam.backgroundColor = new Color(0.2f, 0.2f, 0.2f, 0f);
			cam.orthographic = true;
			cam.enabled = false;
			cam.cullingMask = ~0;
			cam.nearClipPlane = 0.01f;

			// Position camera looking at the blueprint from a 45-degree angle above
			float size = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
			cam.orthographicSize = size * 0.6f;
			cam.farClipPlane = size * 4f;
			Vector3 offset = (Vector3.back + Vector3.up).normalized * size * 1.5f;
			camGo.transform.position = bounds.center + offset;
			camGo.transform.LookAt(bounds.center);

			// Render
			var rt = RenderTexture.GetTemporary(IconSize, IconSize, 16, RenderTextureFormat.ARGB32);
			cam.targetTexture = rt;
			cam.Render();

			// Read pixels
			var prev = RenderTexture.active;
			RenderTexture.active = rt;
			var tex = new Texture2D(IconSize, IconSize, TextureFormat.RGBA32, false);
			tex.ReadPixels(new Rect(0, 0, IconSize, IconSize), 0, 0);
			tex.Apply();
			RenderTexture.active = prev;

			// Cleanup
			cam.targetTexture = null;
			RenderTexture.ReleaseTemporary(rt);
			Object.Destroy(camGo);
			foreach (var go in spawned) Object.Destroy(go);

			var sprite = Sprite.Create(tex, new Rect(0, 0, IconSize, IconSize), new Vector2(0.5f, 0.5f), 100f);
			sprite.name = "bp_icon_" + bp.name;
			return sprite;
		}

		private static void StripNetworkingComponents(GameObject go)
		{
			// Remove physics and gameplay components (ZNetView already destroyed above)
			foreach (var rb in go.GetComponentsInChildren<Rigidbody>(true))
				Object.DestroyImmediate(rb);
			foreach (var col in go.GetComponentsInChildren<Collider>(true))
				Object.DestroyImmediate(col);
			foreach (var piece in go.GetComponentsInChildren<Piece>(true))
				Object.DestroyImmediate(piece);
			foreach (var wnt in go.GetComponentsInChildren<WearNTear>(true))
				Object.DestroyImmediate(wnt);

			// Also destroy ZDO components that might reference ZNetView
			foreach (var comp in go.GetComponentsInChildren<Component>(true))
			{
				if (comp == null) continue;
				var typeName = comp.GetType().Name;
				if (typeName.Contains("ZDO") || typeName.Contains("ZSync"))
				{
					Object.DestroyImmediate(comp);
				}
			}
		}
	}
}
