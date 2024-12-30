using RoR2;
using System.Linq;
using UnityEngine;

namespace RiskOfChaos.Patches
{
    static class CostHooks
    {
        public delegate void OverrideIsAffordableDelegate(CostTypeDef costTypeDef, int cost, Interactor activator, ref bool isAffordable);
        public static event OverrideIsAffordableDelegate OverrideIsAffordable;

        public delegate void OverridePayCostDelegate(CostTypeDef costTypeDef, int cost, Interactor activator, GameObject purchasedObject, Xoroshiro128Plus rng, ItemIndex avoidedItemIndex, ref CostTypeDef.PayCostResults results);
        public static event OverridePayCostDelegate OverridePayCost;

        [SystemInitializer]
        static void Init()
        {
            On.RoR2.CostTypeDef.IsAffordable += CostTypeDef_IsAffordable;
            On.RoR2.CostTypeDef.PayCost += CostTypeDef_PayCost;
        }

        static bool CostTypeDef_IsAffordable(On.RoR2.CostTypeDef.orig_IsAffordable orig, CostTypeDef self, int cost, Interactor activator)
        {
            if (OverrideIsAffordable != null)
            {
                bool hasAnyOverride = false;
                bool isAffordable = false;

                foreach (OverrideIsAffordableDelegate overrideIsAffordable in OverrideIsAffordable.GetInvocationList().OfType<OverrideIsAffordableDelegate>())
                {
                    overrideIsAffordable(self, cost, activator, ref isAffordable);
                    hasAnyOverride = true;
                }

                if (hasAnyOverride)
                {
                    return isAffordable;
                }
            }

            return orig(self, cost, activator);
        }

        static CostTypeDef.PayCostResults CostTypeDef_PayCost(On.RoR2.CostTypeDef.orig_PayCost orig, CostTypeDef self, int cost, Interactor activator, GameObject purchasedObject, Xoroshiro128Plus rng, ItemIndex avoidedItemIndex)
        {
            if (OverridePayCost != null)
            {
                bool hasAnyOverride = false;
                CostTypeDef.PayCostResults results = new CostTypeDef.PayCostResults();

                foreach (OverridePayCostDelegate overridePayCost in OverridePayCost.GetInvocationList().OfType<OverridePayCostDelegate>())
                {
                    overridePayCost(self, cost, activator, purchasedObject, rng, avoidedItemIndex, ref results);
                    hasAnyOverride = true;
                }

                if (hasAnyOverride)
                {
                    return results;
                }
            }

            return orig(self, cost, activator, purchasedObject, rng, avoidedItemIndex);
        }
    }
}
