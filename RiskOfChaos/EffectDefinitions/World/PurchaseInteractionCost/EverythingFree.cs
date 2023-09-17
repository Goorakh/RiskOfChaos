using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.Patches;
using RoR2;

namespace RiskOfChaos.EffectDefinitions.World.PurchaseInteractionCost
{
    [ChaosTimedEffect("everything_free", 30f, AllowDuplicates = false, IsNetworked = true)]
    public sealed class EverythingFree : TimedEffect
    {
        public override void OnStart()
        {
            OverrideCostTypeCostHook.OverrideCost += OverrideCost;
        }

        public override void OnEnd()
        {
            OverrideCostTypeCostHook.OverrideCost -= OverrideCost;
        }

        static void OverrideCost(CostTypeDef costType, ref int cost)
        {
            cost = 0;
        }
    }
}
