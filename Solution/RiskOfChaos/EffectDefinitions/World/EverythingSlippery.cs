using RiskOfChaos.EffectHandling;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.Patches;
using UnityEngine;

namespace RiskOfChaos.EffectDefinitions.World
{
    [ChaosTimedEffect("everything_slippery", TimedEffectType.UntilStageEnd, AllowDuplicates = false)]
    public sealed class EverythingSlippery : MonoBehaviour
    {
        void Start()
        {
            OverrideAllSurfacesSlippery.IsActive = true;
        }

        void OnDestroy()
        {
            OverrideAllSurfacesSlippery.IsActive = false;
        }
    }
}
