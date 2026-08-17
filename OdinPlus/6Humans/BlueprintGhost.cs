using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace OdinPlus
{
	/// <summary>
	/// A non-solid, semi-transparent blue "hologram" stand-in for a not-yet-built blueprint piece.
	/// Spawned by BuilderNPC when a blueprint starts, one per BlueprintPiece, and destroyed/replaced by
	/// the real completed piece prefab as the NPC finishes building that step.
	///
	/// Approach: clone the REAL piece prefab (so shape/scale always matches exactly, no hand-authored
	/// placeholder meshes needed), then strip every live-gameplay component and force every renderer's
	/// material to the shared translucent-blue ghost material (Util.CreateGhostMaterial). This mirrors
	/// the "clone + strip components + swap material" pattern already used in this codebase for
	/// decorative NPC clones (NpcManager.InitOdinGod/InitShaman) and shader replacement
	/// (ShaderReplacerNew in the sibling Trader_2.0 project).
	/// </summary>
	public class BlueprintGhost : MonoBehaviour
	{
		static Material _sharedGhostMaterial;

		public static GameObject Create(GameObject realPrefab, Vector3 pos, Quaternion rot, Transform parent)
		{
			if(realPrefab == null) return null;

			// See NpcManager.InitOdinGod/InitShaman for why this guard matters: prevents
			// ZNetView.Awake() from ever registering a ZDO/m_instances entry for this purely-visual
			// clone - ghosts are never networked, they're independently (and cheaply) recreated by
			// whoever's local BuilderNPC.Awake() resumes an in-progress build.
			ZNetView.m_forceDisableInit = true;
			var go = Instantiate(realPrefab, pos, rot, parent);
			ZNetView.m_forceDisableInit = false;
			go.name = $"BlueprintGhost_{realPrefab.name}";

			var znv = go.GetComponent<ZNetView>();
			if(znv != null) DestroyImmediate(znv);

			// Strip anything that would let this hologram behave like a real piece.
			foreach(var piece in go.GetComponentsInChildren<Piece>(true)) DestroyImmediate(piece);
			foreach(var wnt in go.GetComponentsInChildren<WearNTear>(true)) DestroyImmediate(wnt);
			foreach(var cs in go.GetComponentsInChildren<CraftingStation>(true)) DestroyImmediate(cs);
			foreach(var ctn in go.GetComponentsInChildren<Container>(true)) DestroyImmediate(ctn);
			foreach(var fx in go.GetComponentsInChildren<EffectArea>(true)) DestroyImmediate(fx);
			foreach(var col in go.GetComponentsInChildren<Collider>(true)) col.enabled = false;
			foreach(var rb in go.GetComponentsInChildren<Rigidbody>(true)) DestroyImmediate(rb);
			foreach(var light in go.GetComponentsInChildren<Light>(true)) light.enabled = false;
			foreach(var particles in go.GetComponentsInChildren<ParticleSystem>(true)) particles.Stop();
			foreach(var audio in go.GetComponentsInChildren<AudioSource>(true)) audio.enabled = false;

			ApplyGhostMaterial(go);

			go.AddComponent<BlueprintGhost>();
			return go;
		}

		static void ApplyGhostMaterial(GameObject go)
		{
			if(_sharedGhostMaterial == null)
				_sharedGhostMaterial = Util.CreateGhostMaterial(new Color(0.15f, 0.55f, 1f, 0.35f));
			if(_sharedGhostMaterial == null) return; // Util already logged why

			foreach(var mr in go.GetComponentsInChildren<MeshRenderer>(true))
			{
				var mats = new Material[mr.sharedMaterials.Length];
				for(int i = 0; i < mats.Length; i++) mats[i] = _sharedGhostMaterial;
				mr.sharedMaterials = mats;
				mr.shadowCastingMode = ShadowCastingMode.Off;
			}
			foreach(var smr in go.GetComponentsInChildren<SkinnedMeshRenderer>(true))
			{
				var mats = new Material[smr.sharedMaterials.Length];
				for(int i = 0; i < mats.Length; i++) mats[i] = _sharedGhostMaterial;
				smr.sharedMaterials = mats;
				smr.shadowCastingMode = ShadowCastingMode.Off;
			}
		}
	}
}
