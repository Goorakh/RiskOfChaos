using RiskOfChaos.Content;
using RiskOfChaos.Content.AssetCollections;
using System.Collections.Generic;
using UnityEngine;

namespace RiskOfChaos.ModificationController.Director
{
    public sealed class DirectorModificationManager : MonoBehaviour
    {
        [ContentInitializer]
        static void LoadContent(NetworkedPrefabAssetCollection networkPrefabs)
        {
            // DirectorModificationProvider
            {
                GameObject prefab = Prefabs.CreateNetworkedValueModificationProviderPrefab(typeof(DirectorModificationProvider), nameof(RoCContent.NetworkedPrefabs.DirectorModificationProvider), false);

                networkPrefabs.Add(prefab);
            }
        }

        static DirectorModificationManager _instance;
        public static DirectorModificationManager Instance => _instance;

        ValueModificationProviderHandler<DirectorModificationProvider> _modificationProviderHandler;

        public float CombatDirectorCreditMultiplier { get; private set; }

        void OnEnable()
        {
            SingletonHelper.Assign(ref _instance, this);

            _modificationProviderHandler = new ValueModificationProviderHandler<DirectorModificationProvider>(refreshAllModifications);
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

        void refreshAllModifications(IReadOnlyCollection<DirectorModificationProvider> modificationProviders)
        {
            float combatDirectorCreditMultiplier = 1f;

            foreach (DirectorModificationProvider modificationProvider in modificationProviders)
            {
                combatDirectorCreditMultiplier *= modificationProvider.CombatDirectorCreditMultiplier;
            }

            CombatDirectorCreditMultiplier = Mathf.Max(0f, combatDirectorCreditMultiplier);
        }
    }
}
