namespace OdinPlus
{
	// ponytail: Harmony patches on per-frame methods (ZDOMan.Update, ZoneSystem.Update,
	// ZNetScene.RemoveObjects) removed — they added dispatch overhead to hot paths and
	// used a shared Stopwatch (incorrect if overlapping). Enable via #define PERF_PROFILE if needed.
	public static class PerformanceProfiler { }
}
