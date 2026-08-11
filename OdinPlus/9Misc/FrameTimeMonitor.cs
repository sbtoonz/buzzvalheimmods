using UnityEngine;

namespace OdinPlus
{
	/// <summary>
	/// Monitors frame time and logs when stutters occur
	/// </summary>
	public class FrameTimeMonitor : MonoBehaviour
	{
		private float _lastFrameTime;
		private const float STUTTER_THRESHOLD_MS = 50f; // Log frames >50ms (20 FPS)

		// DISABLED - this component makes stutters WORSE while trying to measure them.
		// Allocates strings + calls expensive APIs (GetActiveZones, NrOfObjects) during
		// the exact frame that's already performance-critical.
		/*
		private void Update()
		{
			float currentFrameTime = Time.deltaTime * 1000f;

			if (currentFrameTime > STUTTER_THRESHOLD_MS)
			{
				DBG.blogWarning($"[FrameTime] STUTTER: {currentFrameTime:F1}ms (previous frame: {_lastFrameTime:F1}ms)");

				// Log what's currently active
				LogActiveExpensiveSystems();
			}

			_lastFrameTime = currentFrameTime;
		}
		*/

		private void LogActiveExpensiveSystems()
		{
			// Check if autosave is running
			if (ZNet.instance != null && ZNet.instance.IsServer())
			{
				// Check if world save is in progress (no direct API, but we can infer)
				DBG.blogWarning("[FrameTime] Server active - possible autosave");
			}

			// Check active ZDO count
			if (ZDOMan.instance != null)
			{
				DBG.blogWarning($"[FrameTime] ZDOs: {ZDOMan.instance.NrOfObjects()}");
			}

			// Check active zones
			if (ZoneSystem.instance != null)
			{
				var activeZones = ZoneSystem.instance.GetActiveZones();
				DBG.blogWarning($"[FrameTime] Active zones: {activeZones.Count}");
			}
		}

		public static void Init()
		{
			if (OdinPlus.Root == null) return;

			var existing = OdinPlus.Root.GetComponent<FrameTimeMonitor>();
			if (existing == null)
			{
				OdinPlus.Root.AddComponent<FrameTimeMonitor>();
				DBG.blogInfo("[FrameTimeMonitor] Started monitoring frame times");
			}
		}
	}
}
