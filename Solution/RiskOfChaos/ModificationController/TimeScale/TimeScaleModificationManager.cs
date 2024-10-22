using RiskOfChaos.Content;
using RiskOfChaos.Content.AssetCollections;
using RiskOfChaos.Utilities;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.ModificationController.TimeScale
{
    public sealed class TimeScaleModificationManager : MonoBehaviour
    {
        static TimeScaleModificationManager _instance;
        public static TimeScaleModificationManager Instance => _instance;

        [ContentInitializer]
        static void LoadContent(NetworkedPrefabAssetCollection networkPrefabs)
        {
            // GenericTimeScaleModificationProvider
            {
                GameObject prefab = Prefabs.CreateValueModificationProviderPrefab(typeof(GenericTimeScaleModificationProvider), nameof(RoCContent.NetworkedPrefabs.GenericTimeScaleModificationProvider), true);

                networkPrefabs.Add(prefab);
            }
        }

        ValueModificationProviderHandler<ITimeScaleModificationProvider> _modificationProviderHandler;

        public static event Action OnPlayerCompensatedTimeScaleChanged;

        public float CurrentTimeScale { get; private set; }

        public float PlayerCompensatedTimeScale { get; private set; }

        void OnEnable()
        {
            SingletonHelper.Assign(ref _instance, this);

            CurrentTimeScale = TimeUtils.UnpausedTimeScale;
            PlayerCompensatedTimeScale = CurrentTimeScale;

            _modificationProviderHandler = new ValueModificationProviderHandler<ITimeScaleModificationProvider>(refreshValueModifications, false);
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

        void Update()
        {
            _modificationProviderHandler?.Update();
        }

        void refreshValueModifications(IReadOnlyCollection<ITimeScaleModificationProvider> modificationProviders)
        {
            float previousPlayerCompensatedTimeScale = PlayerCompensatedTimeScale;

            float baseTimeScale = 1f;

            float timeScale = baseTimeScale;
            float playerCompensatedTimeScale = baseTimeScale;

            foreach (ITimeScaleModificationProvider modificationProvider in modificationProviders)
            {
                if (modificationProvider.TryGetTimeScaleModification(out TimeScaleModificationInfo modificationInfo))
                {
                    timeScale *= modificationInfo.TimeScaleMultiplier;

                    if (modificationInfo.CompensatePlayerSpeed)
                    {
                        playerCompensatedTimeScale *= modificationInfo.TimeScaleMultiplier;
                    }
                }
            }

            if (NetworkServer.active)
            {
                TimeUtils.UnpausedTimeScale = timeScale;
            }

            CurrentTimeScale = timeScale;
            PlayerCompensatedTimeScale = playerCompensatedTimeScale;

            if (PlayerCompensatedTimeScale != previousPlayerCompensatedTimeScale)
            {
                OnPlayerCompensatedTimeScaleChanged?.Invoke();
            }
        }
    }
}
