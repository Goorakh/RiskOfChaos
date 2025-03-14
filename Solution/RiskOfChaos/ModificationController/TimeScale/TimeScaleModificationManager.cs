﻿using RiskOfChaos.Content;
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
                GameObject prefab = Prefabs.CreateNetworkedValueModificationProviderPrefab(typeof(GenericTimeScaleModificationProvider), nameof(RoCContent.NetworkedPrefabs.GenericTimeScaleModificationProvider), true);

                networkPrefabs.Add(prefab);
            }
        }

        ValueModificationProviderHandler<ITimeScaleModificationProvider> _modificationProviderHandler;

        public static event Action OnPlayerCompensatedTimeScaleChanged;

        public float CurrentTimeScale { get; private set; }

        float _playerCompensatedTimeScale;
        public float PlayerCompensatedTimeScale
        {
            get
            {
                return _playerCompensatedTimeScale;
            }
            private set
            {
                if (_playerCompensatedTimeScale == value)
                    return;

                _playerCompensatedTimeScale = value;
                OnPlayerCompensatedTimeScaleChanged?.Invoke();
            }
        }

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

            timeScale = Mathf.Max(0.001f, timeScale);
            playerCompensatedTimeScale = Mathf.Max(0.001f, playerCompensatedTimeScale);

            if (NetworkServer.active)
            {
                TimeUtils.UnpausedTimeScale = timeScale;
            }

            CurrentTimeScale = timeScale;
            PlayerCompensatedTimeScale = playerCompensatedTimeScale;
        }
    }
}
