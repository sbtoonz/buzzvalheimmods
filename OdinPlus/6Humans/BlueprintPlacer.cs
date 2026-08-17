using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace OdinPlus
{
	public class BlueprintPlacer : MonoBehaviour
	{
		static BlueprintPlacer _instance;

		readonly List<GameObject> _ghosts = new();
		Blueprint _armed;
		float _yaw;
		float _verticalOffset;
		const float PlaceDistance = 20f;
		const float VerticalStep = 0.5f;

		string _selectedFaction = "";
		int _factionIndex;

		public static void SetArmed(string blueprintName)
		{
			EnsureInstance();
			_instance.SetArmedInternal(blueprintName);
		}

		public static void Clear()
		{
			if(_instance != null) _instance.ClearInternal();
		}

		static void EnsureInstance()
		{
			if(_instance != null) return;
			var go = new GameObject("OdinPlusBlueprintPlacer");
			DontDestroyOnLoad(go);
			_instance = go.AddComponent<BlueprintPlacer>();
		}

		void SetArmedInternal(string blueprintName)
		{
			ClearGhosts();
			_armed = Blueprints.All.FirstOrDefault(b => b.name == blueprintName);
			_yaw = Player.m_localPlayer != null ? Player.m_localPlayer.transform.eulerAngles.y : 0f;
			_factionIndex = 0;
			_selectedFaction = GetFactionByIndex(0);
		}

		void ClearInternal()
		{
			_armed = null;
			ClearGhosts();
		}

		void ClearGhosts()
		{
			foreach(var ghost in _ghosts)
			{
				if(ghost != null) Destroy(ghost);
			}
			_ghosts.Clear();
		}

		void Update()
		{
			var player = Player.m_localPlayer;
			if(player == null || _armed == null)
			{
				if(_ghosts.Count > 0) ClearGhosts();
				return;
			}

			PieceTable pieceTable = null;
			if(player.InPlaceMode())
				player.GetBuildSelection(out _, out _, out _, out _, out pieceTable);

			// Only ever active while BlueprintTool is actually equipped and the Browser isn't covering
			// the screen - unequipping drops the armed blueprint entirely rather than leaving ghosts
			// floating around.
			if(pieceTable != OdinItem.BlueprintToolPieceTable)
			{
				ClearInternal();
				return;
			}
			if(BlueprintBrowser.IsVisible)
			{
				if(_ghosts.Count > 0) ClearGhosts();
				return;
			}

			// Tab: cycle faction assignment
			if(Input.GetKeyDown(KeyCode.Tab))
			{
				_factionIndex++;
				_selectedFaction = GetFactionByIndex(_factionIndex);
				if(Player.m_localPlayer != null)
					Player.m_localPlayer.Message(MessageHud.MessageType.Center, $"Faction: {_selectedFaction}");
			}

			// Scroll wheel: rotate (default) or adjust height (with Shift held)
			if(Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
			{
				if(Input.mouseScrollDelta.y > 0f) _verticalOffset += VerticalStep;
				else if(Input.mouseScrollDelta.y < 0f) _verticalOffset -= VerticalStep;
			}
			else
			{
				if(Input.mouseScrollDelta.y > 0f) _yaw += 22.5f;
				else if(Input.mouseScrollDelta.y < 0f) _yaw -= 22.5f;
			}

			if(!TryGetPlacementPoint(out var point))
			{
				if(_ghosts.Count > 0) ClearGhosts();
				return;
			}

			var rot = Quaternion.Euler(0f, _yaw, 0f);
			var placementPoint = point + Vector3.up * _verticalOffset;
			EnsureGhosts();
			for(int i = 0; i < _ghosts.Count && i < _armed.pieces.Length; i++)
			{
				if(_ghosts[i] == null) continue;
				var piece = _armed.pieces[i];
				_ghosts[i].transform.SetPositionAndRotation(placementPoint + rot * piece.localPosition, rot * Quaternion.Euler(piece.rotation));
			}

			// Left click: place blueprint
			if(Input.GetMouseButtonDown(0) && GUIUtility.hotControl == 0)
				PlaceBlueprint(placementPoint, rot);
			// Right click: cancel placement
			else if(Input.GetMouseButtonDown(1))
				ClearInternal();
			// Middle mouse (delete mode): remove nearest build site
			else if(Input.GetMouseButtonDown(2) && GUIUtility.hotControl == 0)
				DeleteNearestBuildSite(placementPoint);
		}

		void EnsureGhosts()
		{
			if(_ghosts.Count == _armed.pieces.Length) return;
			ClearGhosts();
			foreach(var piece in _armed.pieces)
			{
				var prefab = ZNetScene.instance.GetPrefab(piece.prefabName);
				if(prefab == null) continue;
				var ghost = BlueprintGhost.Create(prefab, Vector3.zero, Quaternion.identity, null);
				_ghosts.Add(ghost);
			}
		}

		bool TryGetPlacementPoint(out Vector3 point)
		{
			point = Vector3.zero;
			var cam = GameCamera.instance != null ? GameCamera.instance.GetComponent<Camera>() : null;
			if(cam == null) return false;

			var ray = cam.ScreenPointToRay(new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0f));
			if(Physics.Raycast(ray, out var hit, PlaceDistance))
			{
				point = hit.point;
				return true;
			}
			return false;
		}

		void PlaceBlueprint(Vector3 origin, Quaternion rot)
		{
			BuildSiteManager.CreateSite(_armed, origin, rot, _selectedFaction);

			if(Player.m_localPlayer != null)
				Player.m_localPlayer.Message(MessageHud.MessageType.Center,
					$"Build site placed: {_armed.name}\nFaction: {_selectedFaction}\nGive resources to their Builder NPC!");
		}

		static string GetFactionByIndex(int index)
		{
			var factions = new List<string>();
			foreach(var kv in FactionManager.Factions)
			{
				if(kv.Key == "Neutral") continue;
				factions.Add(kv.Key);
			}
			if(factions.Count == 0) return "Villagers";
			return factions[((index % factions.Count) + factions.Count) % factions.Count];
		}

		void DeleteNearestBuildSite(Vector3 pos)
		{
			BuildSite nearest = null;
			var nearestDist = 10f; // Max 10m deletion range

			foreach(var site in BuildSiteManager.Sites)
			{
				if(site.claimedBy != null) continue; // Can't delete claimed sites (being built)
				var dist = Vector3.Distance(pos, site.origin);
				if(dist < nearestDist)
				{
					nearestDist = dist;
					nearest = site;
				}
			}

			if(nearest != null)
			{
				BuildSiteManager.RemoveSite(nearest);
				if(Player.m_localPlayer != null)
					Player.m_localPlayer.Message(MessageHud.MessageType.Center, $"Deleted build site: {nearest.blueprint.name}");
			}
			else
			{
				if(Player.m_localPlayer != null)
					Player.m_localPlayer.Message(MessageHud.MessageType.Center, "No build site nearby to delete");
			}
		}
	}
}
