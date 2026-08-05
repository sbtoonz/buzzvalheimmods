using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace OdinPlus
{
	// The only construction behavior BlueprintTool performs: clone a whole armed blueprint into the
	// world as one stamp. No single-piece placement, no repair/removal (see OdinItem's
	// BlueprintToolPieceTable.m_canRemovePieces = false) - this is deliberately not a general hammer.
	public class BlueprintPlacer : MonoBehaviour
	{
		private static BlueprintPlacer _instance;

		private readonly List<GameObject> _ghosts = new List<GameObject>();
		private Blueprint _armed;
		private float _yaw;
		private const float PlaceDistance = 20f;

		public static void SetArmed(string blueprintName)
		{
			EnsureInstance();
			_instance.SetArmedInternal(blueprintName);
		}

		public static void Clear()
		{
			if (_instance != null) _instance.ClearInternal();
		}

		private static void EnsureInstance()
		{
			if (_instance != null) return;
			var go = new GameObject("OdinPlusBlueprintPlacer");
			DontDestroyOnLoad(go);
			_instance = go.AddComponent<BlueprintPlacer>();
		}

		private void SetArmedInternal(string blueprintName)
		{
			ClearGhosts();
			_armed = Blueprints.All.FirstOrDefault(b => b.name == blueprintName);
			_yaw = Player.m_localPlayer != null ? Player.m_localPlayer.transform.eulerAngles.y : 0f;
		}

		private void ClearInternal()
		{
			_armed = null;
			ClearGhosts();
		}

		private void ClearGhosts()
		{
			foreach (var ghost in _ghosts)
			{
				if (ghost != null) Destroy(ghost);
			}
			_ghosts.Clear();
		}

		private void Update()
		{
			var player = Player.m_localPlayer;
			if (player == null || _armed == null)
			{
				if (_ghosts.Count > 0) ClearGhosts();
				return;
			}

			PieceTable pieceTable = null;
			if (player.InPlaceMode())
				player.GetBuildSelection(out _, out _, out _, out _, out pieceTable);

			// Only ever active while BlueprintTool is actually equipped and the Browser isn't covering
			// the screen - unequipping drops the armed blueprint entirely rather than leaving ghosts
			// floating around.
			if (pieceTable != OdinItem.BlueprintToolPieceTable)
			{
				ClearInternal();
				return;
			}
			if (BlueprintBrowser.IsVisible)
			{
				if (_ghosts.Count > 0) ClearGhosts();
				return;
			}

			if (Input.mouseScrollDelta.y > 0f) _yaw += 22.5f;
			else if (Input.mouseScrollDelta.y < 0f) _yaw -= 22.5f;

			if (!TryGetPlacementPoint(out Vector3 point))
			{
				if (_ghosts.Count > 0) ClearGhosts();
				return;
			}

			Quaternion rot = Quaternion.Euler(0f, _yaw, 0f);
			EnsureGhosts();
			for (int i = 0; i < _ghosts.Count && i < _armed.pieces.Length; i++)
			{
				if (_ghosts[i] == null) continue;
				var piece = _armed.pieces[i];
				_ghosts[i].transform.SetPositionAndRotation(point + rot * piece.localPosition, rot * Quaternion.Euler(piece.rotation));
			}

			if (Input.GetMouseButtonDown(0) && GUIUtility.hotControl == 0)
			{
				PlaceBlueprint(point, rot);
			}
			else if (Input.GetMouseButtonDown(1))
			{
				ClearInternal();
			}
		}

		private void EnsureGhosts()
		{
			if (_ghosts.Count == _armed.pieces.Length) return;
			ClearGhosts();
			foreach (var piece in _armed.pieces)
			{
				var prefab = ZNetScene.instance.GetPrefab(piece.prefabName);
				if (prefab == null) continue;
				var ghost = BlueprintGhost.Create(prefab, Vector3.zero, Quaternion.identity, null);
				_ghosts.Add(ghost);
			}
		}

		private bool TryGetPlacementPoint(out Vector3 point)
		{
			point = Vector3.zero;
			var cam = GameCamera.instance != null ? GameCamera.instance.GetComponent<Camera>() : null;
			if (cam == null) return false;

			Ray ray = cam.ScreenPointToRay(new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0f));
			if (Physics.Raycast(ray, out RaycastHit hit, PlaceDistance))
			{
				point = hit.point;
				return true;
			}
			return false;
		}

		private void PlaceBlueprint(Vector3 origin, Quaternion rot)
		{
			foreach (var piece in _armed.pieces)
			{
				var prefab = ZNetScene.instance.GetPrefab(piece.prefabName);
				if (prefab == null) continue;
				Vector3 pos = origin + rot * piece.localPosition;
				Quaternion pieceRot = rot * Quaternion.Euler(piece.rotation);
				Instantiate(prefab, pos, pieceRot).SetActive(true);
			}

			if (Player.m_localPlayer != null)
				Player.m_localPlayer.Message(MessageHud.MessageType.Center, "Placed " + _armed.name);

			// Keep the blueprint armed so the player can stamp several copies in a row (right-click to
			// cancel) - matches the "just clone a blueprint" framing rather than a one-shot action.
		}
	}
}
