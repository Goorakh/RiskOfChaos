using RiskOfChaos.Components;
using RiskOfChaos.Content;
using RiskOfChaos.Content.AssetCollections;
using RoR2;
using System.Collections.Generic;
using UnityEngine;

namespace RiskOfChaos.ModificationController.AttackDelay
{
    public sealed class AttackDelayModificationManager : MonoBehaviour
    {
        static AttackDelayModificationManager _instance;
        public static AttackDelayModificationManager Instance => _instance;

        [ContentInitializer]
        static void LoadContent(NetworkedPrefabAssetCollection networkPrefabs)
        {
            // AttackDelayModificationProvider
            {
                GameObject prefab = Prefabs.CreateNetworkedPrefab(nameof(RoCContent.NetworkedPrefabs.AttackDelayModificationProvider), [
                    typeof(SetDontDestroyOnLoad),
                    typeof(DestroyOnRunEnd),
                    typeof(AttackDelayModificationProvider)
                ]);

                networkPrefabs.Add(prefab);
            }
        }

        public bool AnyModificationActive { get; private set; }

        public float TotalDelay { get; private set; }

        ValueModificationProviderHandler<AttackDelayModificationProvider> _modificationProviderHandler;

        void OnEnable()
        {
            SingletonHelper.Assign(ref _instance, this);

            _modificationProviderHandler = new ValueModificationProviderHandler<AttackDelayModificationProvider>(refreshValueModifications);
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

        void refreshValueModifications(IReadOnlyCollection<AttackDelayModificationProvider> modificationProviders)
        {
            bool anyModificationActive = false;
            float totalDelay = 0f;

            foreach (AttackDelayModificationProvider attackDelayModification in modificationProviders)
            {
                anyModificationActive = true;

                totalDelay += attackDelayModification.Delay;
            }

            AnyModificationActive = anyModificationActive;
            TotalDelay = totalDelay;
        }
    }
}
