using RiskOfChaos.Content;
using RiskOfChaos.Content.AssetCollections;
using System.Collections.Generic;
using UnityEngine;

namespace RiskOfChaos.ModificationController.UI
{
    public sealed class UIModificationManager : MonoBehaviour
    {
        static UIModificationManager _instance;
        public static UIModificationManager Instance => _instance;

        [ContentInitializer]
        static void LoadContent(LocalPrefabAssetCollection localPrefabs)
        {
            // UIModificationProvider
            {
                GameObject prefab = Prefabs.CreateLocalValueModificationProviderPrefab(typeof(UIModificationProvider), nameof(RoCContent.LocalPrefabs.UIModificationProvider), true);

                localPrefabs.Add(prefab);
            }
        }

        public delegate void OnHudScaleMultiplierChangedDelegate(float newScaleMultiplier);
        public static event OnHudScaleMultiplierChangedDelegate OnHudScaleMultiplierChanged;

        float _hudScaleMultiplier = 1f;
        public float HudScaleMultiplier
        {
            get
            {
                return _hudScaleMultiplier;
            }
            set
            {
                if (_hudScaleMultiplier == value)
                    return;

                _hudScaleMultiplier = value;

                OnHudScaleMultiplierChanged?.Invoke(_hudScaleMultiplier);
            }
        }

        ValueModificationProviderHandler<UIModificationProvider> _modificationProviderHandler;

        void OnEnable()
        {
            SingletonHelper.Assign(ref _instance, this);

            _modificationProviderHandler = new ValueModificationProviderHandler<UIModificationProvider>(refreshValueModifications);
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

        void refreshValueModifications(IReadOnlyCollection<UIModificationProvider> modificationProviders)
        {
            float hudScaleMultiplier = 1f;

            foreach (UIModificationProvider modificationProvider in modificationProviders)
            {
                hudScaleMultiplier *= modificationProvider.HudScaleMultiplier;
            }

            HudScaleMultiplier = hudScaleMultiplier;
        }
    }
}
