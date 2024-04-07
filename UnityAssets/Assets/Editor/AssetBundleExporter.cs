using System.IO;
using UnityEditor;
using UnityEngine;

public static class AssetBundleExporter
{
    [MenuItem("Assets/Export AssetBundle(s)")]
    static void export()
    {
        const string EXPORT_PATH = "Assets/Export/";

        if (!Directory.Exists(EXPORT_PATH))
            Directory.CreateDirectory(EXPORT_PATH);

        AssetBundleManifest manifest = BuildPipeline.BuildAssetBundles(EXPORT_PATH, BuildAssetBundleOptions.ForceRebuildAssetBundle, BuildTarget.StandaloneWindows64);
        if (!manifest)
        {
            Debug.LogError("Failed to build asset bundles");
            return;
        }

        const string TARGET_DIRECTORY = @"..\build\package\plugins\RiskOfChaos\";
        if (!Directory.Exists(TARGET_DIRECTORY))
        {
            Debug.LogError("Package build directory not found");
            return;
        }

        foreach (string assetBundleName in manifest.GetAllAssetBundles())
        {
            string assetBundlePath = EXPORT_PATH + assetBundleName;
            if (!File.Exists(assetBundlePath))
            {
                Debug.LogError($"Asset bundle '{assetBundleName}' not found at project path: {assetBundlePath}");
                continue;
            }

            File.Copy(assetBundlePath, TARGET_DIRECTORY + assetBundleName, true);
        }

        Debug.Log($"Exported {manifest.GetAllAssetBundles().Length} AssetBundle(s)");
    }
}
