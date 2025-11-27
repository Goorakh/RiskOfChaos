using RiskOfChaos.Utilities.Extensions;
using RoR2;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace RiskOfChaos.Components
{
#if DEBUG
    public sealed class DebugAddressableSpawner : MonoBehaviour
    {
        [SystemInitializer]
        static void Init()
        {
            GameObject debugAddressableSpawner = new GameObject("DebugAddressableSpawner");
            debugAddressableSpawner.SetDontDestroyOnLoad(true);

            debugAddressableSpawner.AddComponent<DebugAddressableSpawner>();
        }

        public GameObject SpawnAddressablePrefab(string path)
        {
            return Instantiate(LoadAsset<GameObject>(path));
        }

        public T LoadAsset<T>(string path)
        {
            return Addressables.LoadAssetAsync<T>(path).WaitForCompletion();
        }
    }
#endif
}
