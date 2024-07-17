using RiskOfChaos.Components.CostProviders;
using RiskOfChaos.Utilities.Interpolation;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.ModifierController.Cost
{
    [ValueModificationManager]
    public class CostModificationManager : ValueModificationManager<CostModificationInfo>
    {
        static CostModificationManager _instance;
        public static CostModificationManager Instance => _instance;

        protected override void OnEnable()
        {
            base.OnEnable();

            SingletonHelper.Assign(ref _instance, this);

            OriginalCostProvider.OnOriginalCostInitialized += modifyCost;
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            SingletonHelper.Unassign(ref _instance, this);

            OriginalCostProvider.OnOriginalCostInitialized -= modifyCost;
        }

        public override CostModificationInfo InterpolateValue(in CostModificationInfo a, in CostModificationInfo b, float t)
        {
            return CostModificationInfo.Interpolate(a, b, t, ValueInterpolationFunctionType.Linear);
        }

        public override void UpdateValueModifications()
        {
            if (!NetworkServer.active)
            {
                Log.Warning("Called on client");
                return;
            }

            foreach (OriginalCostProvider originalCostProvider in InstanceTracker.GetInstancesList<OriginalCostProvider>())
            {
                modifyCost(originalCostProvider);
            }
        }

        void modifyCost(OriginalCostProvider originalCostProvider)
        {
            if (!NetworkServer.active)
            {
                Log.Warning("Called on client");
                return;
            }

            CostModificationInfo modificationInfo = GetModifiedValue(new CostModificationInfo(originalCostProvider));

            ICostProvider activeCostProvider = originalCostProvider.ActiveCostProvider;

            activeCostProvider.CostType = modificationInfo.CostType;

            int cost = Mathf.RoundToInt(modificationInfo.CurrentCost);
            if (!modificationInfo.AllowZeroCostResult)
            {
                cost = Mathf.Max(1, cost);
            }

            if (modificationInfo.CostType == CostTypeIndex.PercentHealth)
            {
                cost = Mathf.Min(cost, 99);
            }

            activeCostProvider.Cost = cost;
        }
    }
}
