using RoR2;
using System.Text;
using UnityEngine;

namespace RiskOfChaos.Patches
{
    static class OverrideCostTypeCostHook
    {
        public delegate void OverrideCostDelegate(CostTypeDef costType, ref int cost);

        static event OverrideCostDelegate _overrideCost;
        public static event OverrideCostDelegate OverrideCost
        {
            add
            {
                _overrideCost += value;
                tryApplyPatches();
            }
            remove
            {
                _overrideCost -= value;
            }
        }

        static bool _hasAppliedPatches = false;
        static void tryApplyPatches()
        {
            if (_hasAppliedPatches)
                return;

            On.RoR2.CostTypeDef.BuildCostString += CostTypeDef_BuildCostString;
            On.RoR2.CostTypeDef.BuildCostStringStyled += CostTypeDef_BuildCostStringStyled;
            On.RoR2.CostTypeDef.IsAffordable += CostTypeDef_IsAffordable;
            On.RoR2.CostTypeDef.PayCost += CostTypeDef_PayCost;

            _hasAppliedPatches = true;
        }

        static int getOverrideCost(CostTypeDef costType, int cost)
        {
            _overrideCost?.Invoke(costType, ref cost);
            return cost;
        }

        static void CostTypeDef_BuildCostString(On.RoR2.CostTypeDef.orig_BuildCostString orig, CostTypeDef self, int cost, StringBuilder stringBuilder)
        {
            orig(self, getOverrideCost(self, cost), stringBuilder);
        }

        static void CostTypeDef_BuildCostStringStyled(On.RoR2.CostTypeDef.orig_BuildCostStringStyled orig, CostTypeDef self, int cost, StringBuilder stringBuilder, bool forWorldDisplay, bool includeColor)
        {
            orig(self, getOverrideCost(self, cost), stringBuilder, forWorldDisplay, includeColor);
        }

        static bool CostTypeDef_IsAffordable(On.RoR2.CostTypeDef.orig_IsAffordable orig, CostTypeDef self, int cost, Interactor activator)
        {
            return orig(self, getOverrideCost(self, cost), activator);
        }

        static CostTypeDef.PayCostResults CostTypeDef_PayCost(On.RoR2.CostTypeDef.orig_PayCost orig, CostTypeDef self, int cost, Interactor activator, GameObject purchasedObject, Xoroshiro128Plus rng, ItemIndex avoidedItemIndex)
        {
            return orig(self, getOverrideCost(self, cost), activator, purchasedObject, rng, avoidedItemIndex);
        }
    }
}
