using RiskOfChaos.Content;
using RiskOfChaos.Content.AssetCollections;
using System.Collections.Generic;
using UnityEngine;

namespace RiskOfChaos.ModificationController.Pickups
{
    public sealed class PickupModificationManager : MonoBehaviour
    {
        static PickupModificationManager _instance;
        public static PickupModificationManager Instance => _instance;

        [ContentInitializer]
        static void LoadContent(NetworkedPrefabAssetCollection networkPrefabs)
        {
            // PickupModificationProvider
            {
                GameObject prefab = Prefabs.CreateValueModificationProviderPrefab(typeof(PickupModificationProvider), nameof(RoCContent.NetworkedPrefabs.PickupModificationProvider), false);

                networkPrefabs.Add(prefab);
            }
        }

        public int BounceCount { get; private set; }

        public int ExtraSpawnCount { get; private set; }

        ValueModificationProviderHandler<PickupModificationProvider> _modificationProviderHandler;

        void OnEnable()
        {
            SingletonHelper.Assign(ref _instance, this);

            _modificationProviderHandler = new ValueModificationProviderHandler<PickupModificationProvider>(refreshValueModifications);
        }

        void OnDisable()
        {
            SingletonHelper.Unassign(ref _instance, this);

            if (_modificationProviderHandler != null)
            {
                _modificationProviderHandler.Dispose();
                _modificationProviderHandler = null;
            }
        }

        void refreshValueModifications(IReadOnlyCollection<PickupModificationProvider> modificationProviders)
        {
            int bounceCount = 0;
            float spawnCountMultiplier = 1f;

            foreach (PickupModificationProvider modificationProvider in modificationProviders)
            {
                bounceCount += modificationProvider.BounceCount;
                spawnCountMultiplier *= modificationProvider.SpawnCountMultiplier;
            }

            BounceCount = bounceCount;
            ExtraSpawnCount = Mathf.Clamp(Mathf.RoundToInt(spawnCountMultiplier - 1f), 0, 255);
        }
    }
}
