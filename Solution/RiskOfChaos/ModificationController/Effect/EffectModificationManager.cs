using RiskOfChaos.Components;
using RiskOfChaos.Content;
using RiskOfChaos.Content.AssetCollections;
using RiskOfChaos.EffectHandling;
using RoR2;
using System.Collections.Generic;
using UnityEngine;

namespace RiskOfChaos.ModificationController.Effect
{
    public sealed class EffectModificationManager : MonoBehaviour
    {
        static EffectModificationManager _instance;
        public static EffectModificationManager Instance => _instance;

        [ContentInitializer]
        static void LoadContent(NetworkedPrefabAssetCollection networkedPrefabs)
        {
            // EffectModificationProvider
            {
                GameObject prefab = Prefabs.CreateNetworkedPrefab(nameof(RoCContent.NetworkedPrefabs.EffectModificationProvider), [
                    typeof(SetDontDestroyOnLoad),
                    typeof(DestroyOnRunEnd),
                    typeof(EffectModificationProvider)
                ]);

                networkedPrefabs.Add(prefab);
            }
        }

        public float DurationMultiplier { get; private set; }

        ValueModificationProviderHandler<EffectModificationProvider> _modificationProviderHandler;

        void OnEnable()
        {
            SingletonHelper.Assign(ref _instance, this);

            _modificationProviderHandler = new ValueModificationProviderHandler<EffectModificationProvider>(refreshValueModifications);
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

        void refreshValueModifications(IReadOnlyCollection<EffectModificationProvider> modificationProviders)
        {
            float durationMultiplier = 1f;

            foreach (EffectModificationProvider modificationProvider in modificationProviders)
            {
                durationMultiplier *= modificationProvider.DurationMultiplier;
            }

            DurationMultiplier = durationMultiplier;
        }

        public bool TryModifyDuration(TimedEffectInfo effectInfo, ref float duration)
        {
            if (effectInfo == null || effectInfo.IgnoreDurationModifiers || DurationMultiplier == 1f)
                return false;

            duration *= DurationMultiplier;
            return true;
        }
    }
}
