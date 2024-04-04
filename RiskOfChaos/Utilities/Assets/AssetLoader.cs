using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace RiskOfChaos.Utilities.Assets
{
    public static class AssetLoader
    {
        class AssetBundleInfo
        {
            public readonly AssetBundle AssetBundle;

            readonly Dictionary<string, UnityEngine.Object> _cachedAssets = [];

            public AssetBundleInfo(AssetBundle assetBundle)
            {
                AssetBundle = assetBundle;
            }

            public T LoadAssetCached<T>(string assetName) where T : UnityEngine.Object
            {
                if (!_cachedAssets.TryGetValue(assetName, out UnityEngine.Object asset))
                {
                    asset = AssetBundle.LoadAsset(assetName);
                    _cachedAssets.Add(assetName, asset);
                }

                if (asset is T)
                {
                    return (T)asset;
                }
                else
                {
                    Log.Error($"Asset type mismatch: {assetName} is of type {asset.GetType().FullName} but {typeof(T).FullName} was requested");
                    return null;
                }
            }
        }

        static readonly Dictionary<string, AssetBundleInfo> _cachedAssetBundles = [];

        static AssetBundleInfo getAssetBundleInfoCached(string bundlePath)
        {
            if (_cachedAssetBundles.TryGetValue(bundlePath, out AssetBundleInfo cachedBundleInfo))
                return cachedBundleInfo;

            string fullPath = Path.Combine(Main.ModDirectory, bundlePath);
            AssetBundle assetBundle = AssetBundle.LoadFromFile(fullPath);
            if (!assetBundle)
            {
                Log.Error($"Failed to load asset bundle file '{bundlePath}' ({fullPath})");
                return null;
            }

            AssetBundleInfo assetBundleInfo = new AssetBundleInfo(assetBundle);
            _cachedAssetBundles.Add(bundlePath, assetBundleInfo);
            return assetBundleInfo;
        }

        public static AssetBundle GetAssetBundleCached(string bundlePath)
        {
            return getAssetBundleInfoCached(bundlePath)?.AssetBundle;
        }

        public static T LoadAssetCached<T>(string bundlePath, string assetName) where T : UnityEngine.Object
        {
            return getAssetBundleInfoCached(bundlePath)?.LoadAssetCached<T>(assetName);
        }
    }
}
