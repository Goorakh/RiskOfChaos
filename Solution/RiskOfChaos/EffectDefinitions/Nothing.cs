using RiskOfChaos.EffectHandling;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Data;
using UnityEngine;

namespace RiskOfChaos.EffectDefinitions
{
    [ChaosEffect("nothing", DefaultSelectionWeight = 0.7f)]
    public sealed class Nothing : MonoBehaviour
    {
        [InitEffectInfo]
        public static readonly ChaosEffectInfo EffectInfo;
    }
}
