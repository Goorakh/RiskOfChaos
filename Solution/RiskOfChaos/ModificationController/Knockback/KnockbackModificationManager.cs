using RiskOfChaos.Content;
using RiskOfChaos.Content.AssetCollections;
using System.Collections.Generic;
using UnityEngine;

namespace RiskOfChaos.ModificationController.Knockback
{
    public sealed class KnockbackModificationManager : MonoBehaviour
    {
        static KnockbackModificationManager _instance;
        public static KnockbackModificationManager Instance => _instance;

        [ContentInitializer]
        static void LoadContent(NetworkedPrefabAssetCollection networkPrefabs)
        {
            // KnockbackModificationProvider
            {
                GameObject prefab = Prefabs.CreateValueModificationProviderPrefab(typeof(KnockbackModificationProvider), nameof(RoCContent.NetworkedPrefabs.KnockbackModificationProvider));

                networkPrefabs.Add(prefab);
            }
        }

        ValueModificationProviderHandler<KnockbackModificationProvider> _modificationProviderHandler;

        public bool AnyModificationActive { get; private set; }

        public float TotalKnockbackMultiplier { get; private set; }

        void OnEnable()
        {
            SingletonHelper.Assign(ref _instance, this);

            _modificationProviderHandler = new ValueModificationProviderHandler<KnockbackModificationProvider>(refreshModifications);
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

        void refreshModifications(IReadOnlyCollection<KnockbackModificationProvider> modificationProviders)
        {
            bool anyModificationActive = false;
            float knockbackMultiplier = 1f;

            foreach (KnockbackModificationProvider modificationProvider in modificationProviders)
            {
                anyModificationActive = true;
                knockbackMultiplier *= modificationProvider.KnockbackMultiplier;
            }

            AnyModificationActive = anyModificationActive;
            TotalKnockbackMultiplier = knockbackMultiplier;
        }
    }
}
