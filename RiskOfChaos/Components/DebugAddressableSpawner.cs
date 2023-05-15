using RoR2;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace RiskOfChaos.Components
{
#if DEBUG
    public class DebugAddressableSpawner : MonoBehaviour
    {
        [SystemInitializer]
        static void Init()
        {
            GameObject debugAddressableSpawner = new GameObject("DebugAddressableSpawner");
            DontDestroyOnLoad(debugAddressableSpawner);

            debugAddressableSpawner.AddComponent<DebugAddressableSpawner>();
        }

        public GameObject SpawnAddressablePrefab(string path)
        {
            GameObject prefab = Addressables.LoadAssetAsync<GameObject>(path).WaitForCompletion();
            return Instantiate(prefab);
        }
    }
#endif
}
