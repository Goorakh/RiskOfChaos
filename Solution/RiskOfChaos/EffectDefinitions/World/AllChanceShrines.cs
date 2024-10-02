using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.EffectUtils.World.AllChanceShrines;
using RiskOfChaos.Utilities.Extensions;
using RoR2;
using System.Collections.Generic;
using System.Linq;

namespace RiskOfChaos.EffectDefinitions.World
{
    [ChaosEffect("all_chance_shrines", DefaultSelectionWeight = 0.7f)]
    public sealed class AllChanceShrines : BaseEffect
    {
        static IEnumerable<ShrineReplacementData> getAllReplacementsData()
        {
            return InstanceTracker.GetInstancesList<PurchaseInteraction>().SelectMany(ShrineReplacementData.GetReplacementDatasFor);
        }

        [EffectCanActivate]
        static bool CanActivate()
        {
            return FixedPositionChanceShrine.SpawnCard && getAllReplacementsData().Any();
        }

        public override void OnStart()
        {
            // ToList is required here since PerformReplacement will modify the enumerable returned by getAllReplacementsData
            getAllReplacementsData().ToList().TryDo(replacementData =>
            {
                replacementData.PerformReplacement(RNG.Branch());
            });
        }
    }
}
