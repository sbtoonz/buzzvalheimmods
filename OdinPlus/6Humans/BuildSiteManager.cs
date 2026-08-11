using System.Collections.Generic;
using UnityEngine;

namespace OdinPlus
{
	public class BuildSite
	{
		public Blueprint blueprint;
		public string faction; // which faction's builder should claim this
		public Vector3 origin;
		public Quaternion rotation;
		public List<GameObject> ghosts = new List<GameObject>();
		public BuilderNPC claimedBy;

		public void SpawnGhosts()
		{
			ClearGhosts();
			foreach (var piece in blueprint.pieces)
			{
				var prefab = ZNetScene.instance.GetPrefab(piece.prefabName);
				if (prefab == null) continue;
				Vector3 pos = origin + rotation * piece.localPosition;
				Quaternion rot = rotation * Quaternion.Euler(piece.rotation);
				var ghost = BlueprintGhost.Create(prefab, pos, rot, null);
				ghosts.Add(ghost);
			}
		}

		public void ClearGhosts()
		{
			foreach (var g in ghosts)
				if (g != null) Object.Destroy(g);
			ghosts.Clear();
		}
	}

	public static class BuildSiteManager
	{
		public static readonly List<BuildSite> Sites = new List<BuildSite>();

		public static void CreateSite(Blueprint bp, Vector3 origin, Quaternion rotation, string faction)
		{
			var site = new BuildSite
			{
				blueprint = bp,
				origin = origin,
				rotation = rotation,
				faction = faction
			};
			site.SpawnGhosts();
			Sites.Add(site);
			DBG.blogInfo($"[BuildSiteManager] Created site for '{bp.name}' faction='{faction}' at {origin} ({bp.pieces.Length} pieces, cost: {FormatCosts(bp)})");
		}

		public static BuildSite GetUnclaimedSite(Vector3 npcPos, float maxRange, string npcFaction)
		{
			BuildSite best = null;
			float bestDist = maxRange;
			foreach (var site in Sites)
			{
				if (site.claimedBy != null) continue;
				// Only match if site has no faction (anyone can claim) or matches this NPC's faction
				if (!string.IsNullOrEmpty(site.faction) && site.faction != npcFaction) continue;
				float dist = Vector3.Distance(npcPos, site.origin);
				if (dist < bestDist)
				{
					bestDist = dist;
					best = site;
				}
			}
			return best;
		}

		public static void RemoveSite(BuildSite site)
		{
			site.ClearGhosts();
			Sites.Remove(site);
		}

		public static void Clear()
		{
			foreach (var site in Sites) site.ClearGhosts();
			Sites.Clear();
		}

		private static string FormatCosts(Blueprint bp)
		{
			var parts = new List<string>();
			foreach (var kv in bp.resourceCosts)
				parts.Add($"{kv.Value} {kv.Key}");
			return string.Join(", ", parts);
		}
	}
}
