using RiskOfChaos.Content;
using RiskOfChaos.Content.AssetCollections;
using RiskOfChaos.EffectHandling;
using System;
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
                GameObject prefab = Prefabs.CreateNetworkedValueModificationProviderPrefab(typeof(EffectModificationProvider), nameof(RoCContent.NetworkedPrefabs.EffectModificationProvider), false);

                networkedPrefabs.Add(prefab);
            }
        }

        public static event Action OnDurationMultiplierChanged;

        float _durationMultiplier = 1f;
        public float DurationMultiplier
        {
            get
            {
                return _durationMultiplier;
            }
            private set
            {
                if (_durationMultiplier == value)
                    return;

                _durationMultiplier = value;
                OnDurationMultiplierChanged?.Invoke();
            }
        }

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

            DurationMultiplier = 1f;
        }

        void refreshValueModifications(IReadOnlyCollection<EffectModificationProvider> modificationProviders)
        {
            float durationMultiplier = 1f;

            foreach (EffectModificationProvider modificationProvider in modificationProviders)
            {
                durationMultiplier *= modificationProvider.DurationMultiplier;
            }

            DurationMultiplier = Mathf.Max(0f, durationMultiplier);
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
