using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.EffectUtils.World.AllChanceShrines;
using RiskOfChaos.Utilities.Extensions;
using RoR2;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectDefinitions.World
{
    [ChaosEffect("all_chance_shrines", DefaultSelectionWeight = 0.7f)]
    public sealed class AllChanceShrines : MonoBehaviour
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

        void Start()
        {
            if (!NetworkServer.active)
                return;

            // ToList is required here since PerformReplacement will modify the enumerable returned by getAllReplacementsData
            getAllReplacementsData().ToList().TryDo(replacementData =>
            {
                replacementData.PerformReplacement();
            });
        }
    }
}
