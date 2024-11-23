using RiskOfChaos.Content;
using System.Collections;
using System.IO;
using UnityEngine;

namespace RiskOfChaos.Utilities.Assets
{
    public static class AssetLoader
    {
        const string ASSET_BUNDLE_NAME = "riskofchaos";

        static AssetBundle _assetBundle;

        [ContentInitializer]
        static IEnumerator LoadContent()
        {
            string assetBundlePath = Path.Combine(Main.ModDirectory, ASSET_BUNDLE_NAME);
            AssetBundleCreateRequest assetBundleLoad = AssetBundle.LoadFromFileAsync(assetBundlePath);
            yield return assetBundleLoad;

            AssetBundle assetBundle = assetBundleLoad.assetBundle;
            if (assetBundle)
            {
                _assetBundle = assetBundle;

                Log.Debug("Loaded asset bundle");
            }
            else
            {
                Log.Error($"Failed to load asset bundle");
            }
        }

        public static AssetLoadOperation<T> LoadAssetAsync<T>(string name) where T : UnityEngine.Object
        {
            if (!_assetBundle)
                return null;

            return new AssetLoadOperation<T>(_assetBundle.LoadAssetAsync<T>(name));
        }
    }
}
