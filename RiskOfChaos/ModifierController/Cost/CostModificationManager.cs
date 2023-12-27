using RiskOfChaos.Components.CostProviders;
using RiskOfChaos.Utilities.Interpolation;
using RoR2;
using UnityEngine;

namespace RiskOfChaos.ModifierController.Cost
{
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
            foreach (OriginalCostProvider originalCostProvider in InstanceTracker.GetInstancesList<OriginalCostProvider>())
            {
                modifyCost(originalCostProvider);
            }
        }

        void modifyCost(OriginalCostProvider originalCostProvider)
        {
            CostModificationInfo modificationInfo = GetModifiedValue(new CostModificationInfo(originalCostProvider));

            ICostProvider activeCostProvider = originalCostProvider.ActiveCostProvider;

            activeCostProvider.CostType = modificationInfo.CostType;
            activeCostProvider.Cost = Mathf.RoundToInt(originalCostProvider.Cost * modificationInfo.CostMultiplier);
        }
    }
}
