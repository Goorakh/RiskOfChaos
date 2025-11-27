using RoR2;
using System.Linq;
using UnityEngine;

namespace RiskOfChaos.Patches
{
    static class CostHooks
    {
        public delegate void OverrideIsAffordableDelegate(CostTypeDef costTypeDef, int cost, Interactor activator, ref bool isAffordable);
        public static event OverrideIsAffordableDelegate OverrideIsAffordable;

        public delegate void OverridePayCostDelegate(CostTypeDef costTypeDef, CostTypeDef.PayCostContext context, CostTypeDef.PayCostResults result);
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

        static void CostTypeDef_PayCost(On.RoR2.CostTypeDef.orig_PayCost orig, CostTypeDef self, CostTypeDef.PayCostContext context, CostTypeDef.PayCostResults result)
        {
            if (OverridePayCost != null)
            {
                bool hasAnyOverride = false;
                foreach (OverridePayCostDelegate overridePayCost in OverridePayCost.GetInvocationList().OfType<OverridePayCostDelegate>())
                {
                    overridePayCost(self, context, result);
                    hasAnyOverride = true;
                }

                if (hasAnyOverride)
                {
                    return;
                }
            }

            orig(self, context, result);
        }
    }
}
