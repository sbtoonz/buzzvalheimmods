namespace OdinPlus
{
	// ponytail: SetActive patch removed — patching one of Unity's most-called methods
	// added trampoline overhead to thousands of calls/frame. BlueprintBrowser.Update()
	// already disables BuildHud children directly every frame when the tool is active.
	public static class BuildHudSuppressor
	{
		internal static bool BlueprintToolActive;
	}
}
