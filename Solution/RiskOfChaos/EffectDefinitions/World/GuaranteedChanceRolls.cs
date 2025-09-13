using RiskOfChaos.EffectHandling;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Data;
using UnityEngine;

namespace RiskOfChaos.EffectDefinitions.World
{
    [ChaosTimedEffect("guaranteed_chance_rolls", 30f, AllowDuplicates = false, DefaultSelectionWeight = 0.9f)]
    [EffectConfigBackwardsCompatibility("Effect: Guaranteed Chance Effects (Lasts 1 stage)")]
    public sealed class GuaranteedChanceRolls : MonoBehaviour
    {
        [InitEffectInfo]
        public static readonly TimedEffectInfo EffectInfo;
    }
}
