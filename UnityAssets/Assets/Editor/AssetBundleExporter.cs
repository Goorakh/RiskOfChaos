﻿using System.IO;
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

        Debug.Log($"Exported {manifest.GetAllAssetBundles().Length} AssetBundle(s)");
    }
}
