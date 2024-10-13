using RiskOfChaos.Components;
using RiskOfChaos.Content;
using RiskOfChaos.Content.AssetCollections;
using RiskOfChaos.ModificationController.AttackDelay;
using RiskOfChaos.ModificationController.Camera;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.ModificationController
{
    public sealed class ValueModificationManager : MonoBehaviour
    {
        static ValueModificationManager _instance;
        public static ValueModificationManager Instance => _instance;

        [ContentInitializer]
        static void LoadContent(NetworkedPrefabAssetCollection networkPrefabs)
        {
            GameObject prefab = Prefabs.CreateNetworkedPrefab(nameof(RoCContent.NetworkedPrefabs.ValueModificationManager), [
                typeof(SetDontDestroyOnLoad),
                typeof(DestroyOnRunEnd),
                typeof(ValueModificationManager),
                typeof(AttackDelayModificationManager),
                typeof(CameraModificationManager)
            ]);

            networkPrefabs.Add(prefab);
        }

        [SystemInitializer]
        static void Init()
        {
            Run.onRunStartGlobal += onRunStartGlobal;
        }

        static void onRunStartGlobal(Run run)
        {
            if (!NetworkServer.active)
                return;

            GameObject valueModificationManager = Instantiate(RoCContent.NetworkedPrefabs.ValueModificationManager);
            NetworkServer.Spawn(valueModificationManager);
        }

        void OnEnable()
        {
            SingletonHelper.Assign(ref _instance, this);
        }

        void OnDisable()
        {
            SingletonHelper.Unassign(ref _instance, this);
        }
    }
}
