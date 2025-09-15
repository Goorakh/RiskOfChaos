using RiskOfChaos.Components.CostProviders;
using RiskOfChaos.Content;
using RiskOfChaos.Utilities;
using RoR2;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.ModificationController.Cost
{
    public sealed class CostModificationManager : MonoBehaviour
    {
        [ContentInitializer]
        static void LoadContent(ContentIntializerArgs args)
        {
            // CostModificationProvider
            {
                GameObject prefab = Prefabs.CreateNetworkedValueModificationProviderPrefab(typeof(CostModificationProvider), nameof(RoCContent.NetworkedPrefabs.CostModificationProvider), false);

                args.ContentPack.networkedObjectPrefabs.Add([prefab]);
            }

            // CostConversionProvider
            {
                GameObject prefab = Prefabs.CreateNetworkedValueModificationProviderPrefab(typeof(CostConversionProvider), nameof(RoCContent.NetworkedPrefabs.CostConversionProvider), false);

                args.ContentPack.networkedObjectPrefabs.Add([prefab]);
            }
        }

        static CostModificationManager _instance;
        public static CostModificationManager Instance => _instance;

        ValueModificationProviderHandler<CostModificationProvider> _modificationHandler;
        ValueModificationProviderHandler<CostConversionProvider> _conversionHandler;

        bool _anyModificationDirty;

        void Awake()
        {
            if (!NetworkServer.active)
            {
                enabled = false;
            }
        }

        void OnEnable()
        {
            SingletonHelper.Assign(ref _instance, this);

            _modificationHandler = new ValueModificationProviderHandler<CostModificationProvider>(markModificationsDirty);
            _conversionHandler = new ValueModificationProviderHandler<CostConversionProvider>(markConversionsDirty);

            OriginalCostProvider.OnOriginalCostInitialized += refreshModificationsFor;
        }

        void OnDisable()
        {
            SingletonHelper.Unassign(ref _instance, this);

            OriginalCostProvider.OnOriginalCostInitialized -= refreshModificationsFor;

            if (_modificationHandler != null)
            {
                _modificationHandler.Dispose();
                _modificationHandler = null;
            }

            if (_conversionHandler != null)
            {
                _conversionHandler.Dispose();
                _conversionHandler = null;
            }

            foreach (OriginalCostProvider costProvider in InstanceTracker.GetInstancesList<OriginalCostProvider>())
            {
                costProvider.ResetCost();
            }
        }

        void FixedUpdate()
        {
            if (_anyModificationDirty)
            {
                _anyModificationDirty = false;
                refreshAllModifications();
            }
        }

        void markModificationsDirty(IReadOnlyCollection<CostModificationProvider> modificationProviders)
        {
            _anyModificationDirty = true;
        }

        void markConversionsDirty(IReadOnlyCollection<CostConversionProvider> conversionProviders)
        {
            _anyModificationDirty = true;
        }

        void refreshAllModifications()
        {
            _anyModificationDirty = false;

            foreach (OriginalCostProvider costProvider in InstanceTracker.GetInstancesList<OriginalCostProvider>())
            {
                refreshModificationsFor(costProvider);
            }
        }

        void refreshModificationsFor(OriginalCostProvider originalCost)
        {
            IReadOnlyCollection<CostModificationProvider> modificationProviders = [];
            if (_modificationHandler != null)
            {
                modificationProviders = _modificationHandler.ActiveProviders;
            }

            IReadOnlyCollection<CostConversionProvider> conversionProviders = [];
            if (_conversionHandler != null)
            {
                conversionProviders = _conversionHandler.ActiveProviders;
            }

            refreshCostModifications(originalCost, modificationProviders, conversionProviders);
        }

        void refreshCostModifications(OriginalCostProvider originalCost, IReadOnlyCollection<CostModificationProvider> modificationProviders, IReadOnlyCollection<CostConversionProvider> conversionProviders)
        {
            ICostProvider activeCostProvider = originalCost.ActiveCostProvider;

            CostTypeIndex previousCostType = activeCostProvider.CostType;
            int previousCost = activeCostProvider.Cost;

            CostModificationInfo costModificationInfo = new CostModificationInfo(originalCost);

            bool convertedCostType = false;

            foreach (CostConversionProvider conversionProvider in conversionProviders)
            {
                convertedCostType |= conversionProvider.TryConvertCostType(ref costModificationInfo, originalCost);
            }

            bool ignoreZeroCostRestriction = false;

            foreach (CostModificationProvider modificationProvider in modificationProviders)
            {
                costModificationInfo.CostMultiplier *= modificationProvider.CostMultiplier;
                ignoreZeroCostRestriction |= modificationProvider.IgnoreZeroCostRestriction;
            }

            costModificationInfo.CostMultiplier = Mathf.Max(0f, costModificationInfo.CostMultiplier);

            bool allowZeroCost = ignoreZeroCostRestriction || CostUtils.AllowsZeroCost(costModificationInfo.CostType);

            int baseMinCost = CostUtils.GetMinCost(costModificationInfo.CostType);

            int minCost = allowZeroCost ? 0 : baseMinCost;
            int maxCost = CostUtils.GetMaxCost(costModificationInfo.CostType);

            int cost = Mathf.Clamp(Mathf.RoundToInt(costModificationInfo.CurrentCost), minCost, maxCost);

            if (cost < baseMinCost)
            {
                cost = minCost;
            }

            if (convertedCostType && costModificationInfo.CostType == CostTypeIndex.Money && Run.instance && Stage.instance)
            {
                cost = Run.instance.GetDifficultyScaledCost(cost, Stage.instance.entryDifficultyCoefficient);
            }

            activeCostProvider.Cost = cost;
            activeCostProvider.CostType = costModificationInfo.CostType;

#if DEBUG
            if (activeCostProvider.CostType != previousCostType || activeCostProvider.Cost != previousCost)
            {
                Log.Debug($"Set cost of {originalCost.name}: {previousCostType}->{activeCostProvider.CostType} ({previousCost}->{activeCostProvider.Cost})");
            }
#endif
        }
    }
}
