using RiskOfChaos.Content;
using RiskOfChaos.Content.AssetCollections;
using RiskOfChaos.Networking.Components;
using RiskOfChaos.Trackers;
using RoR2;
using System.Collections.Generic;
using UnityEngine;

namespace RiskOfChaos.ModificationController.HoldoutZone
{
    public sealed class HoldoutZoneModificationManager : MonoBehaviour
    {
        static HoldoutZoneModificationManager _instance;
        public static HoldoutZoneModificationManager Instance => _instance;

        [ContentInitializer]
        static void LoadContent(NetworkedPrefabAssetCollection networkPrefabs)
        {
            // SimpleHoldoutZoneModificationProvider
            {
                GameObject prefab = Prefabs.CreateNetworkedValueModificationProviderPrefab(typeof(SimpleHoldoutZoneModificationProvider), nameof(RoCContent.NetworkedPrefabs.SimpleHoldoutZoneModificationProvider), false);

                networkPrefabs.Add(prefab);
            }
        }

        ValueModificationProviderHandler<IHoldoutZoneModificationProvider> _modificationProviderHandler;

        void OnEnable()
        {
            SingletonHelper.Assign(ref _instance, this);

            _modificationProviderHandler = new ValueModificationProviderHandler<IHoldoutZoneModificationProvider>(refreshAllHoldoutZoneModifications);

            HoldoutZoneTracker.OnHoldoutZoneStartGlobal += onHoldoutZoneStartGlobal;
        }

        void OnDisable()
        {
            SingletonHelper.Unassign(ref _instance, this);

            HoldoutZoneTracker.OnHoldoutZoneStartGlobal -= onHoldoutZoneStartGlobal;

            if (_modificationProviderHandler != null)
            {
                _modificationProviderHandler.Dispose();
                _modificationProviderHandler = null;
            }
        }

        void onHoldoutZoneStartGlobal(HoldoutZoneTracker holdoutZoneTracker)
        {
            _modificationProviderHandler?.MarkValueModificationsDirty();
        }

        void refreshAllHoldoutZoneModifications(IReadOnlyCollection<IHoldoutZoneModificationProvider> modificationProviders)
        {
            foreach (HoldoutZoneModifier zoneModifier in InstanceTracker.GetInstancesList<HoldoutZoneModifier>())
            {
                refreshModifications(zoneModifier, modificationProviders);
            }
        }

        void refreshModifications(HoldoutZoneModifier holdoutZoneModifier, IReadOnlyCollection<IHoldoutZoneModificationProvider> modificationProviders)
        {
            HoldoutZoneController holdoutZoneController = holdoutZoneModifier.HoldoutZoneController;

            HoldoutZoneModificationInfo totalModificationInfo = new HoldoutZoneModificationInfo();

            foreach (IHoldoutZoneModificationProvider modificationProvider in modificationProviders)
            {
                HoldoutZoneModificationInfo modificationInfo = modificationProvider.GetHoldoutZoneModifications(holdoutZoneController);

                totalModificationInfo.RadiusMultiplier *= modificationInfo.RadiusMultiplier;
                totalModificationInfo.ChargeRateMultiplier *= modificationInfo.ChargeRateMultiplier;
            }

            holdoutZoneModifier.RadiusMultiplier = totalModificationInfo.RadiusMultiplier;
            holdoutZoneModifier.ChargeRateMultiplier = totalModificationInfo.ChargeRateMultiplier;
        }
    }
}
