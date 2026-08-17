using System.Reflection;
using UnityEngine;

namespace OdinPlus
{
	/// <summary>
	/// Loads blueprint browser UI from embedded AssetBundle (PlanBuild pattern)
	/// </summary>
	public static class BlueprintBrowserAssets
	{
		public static GameObject BlueprintBrowserPrefab { get; private set; }

		public static void Load()
		{
			var assembly = Assembly.GetExecutingAssembly();
			var bundleData = Util.GetResource(assembly, "OdinPlus.Resources.blueprintui");

			if(bundleData == null || bundleData.Length == 0)
			{
				DBG.blogError("[BlueprintBrowserAssets] Failed to load blueprintui bundle from embedded resources");
				return;
			}

			var bundle = AssetBundle.LoadFromMemory(bundleData);
			if(bundle == null)
			{
				DBG.blogError("[BlueprintBrowserAssets] Failed to load AssetBundle from memory");
				return;
			}

			// Load the prefab (try v2 first, fallback to v1)
			BlueprintBrowserPrefab = bundle.LoadAsset<GameObject>("BlueprintBrowserGUI_v2");
			if(BlueprintBrowserPrefab == null)
				BlueprintBrowserPrefab = bundle.LoadAsset<GameObject>("BlueprintBrowserGUI");
			if(BlueprintBrowserPrefab == null)
			{
				DBG.blogError("[BlueprintBrowserAssets] BlueprintBrowserGUI prefab not found in bundle");
				bundle.Unload(true);
				return;
			}

			bundle.Unload(false); // Keep assets loaded

			DBG.blogInfo($"[BlueprintBrowserAssets] Loaded BlueprintBrowserGUI prefab from AssetBundle");
		}
	}
}
