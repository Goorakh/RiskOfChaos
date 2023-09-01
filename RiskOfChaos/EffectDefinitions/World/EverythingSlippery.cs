using RiskOfChaos.EffectHandling;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.Patches;

namespace RiskOfChaos.EffectDefinitions.World
{
    [ChaosTimedEffect("everything_slippery", TimedEffectType.UntilStageEnd, AllowDuplicates = false, IsNetworked = true)]
    public sealed class EverythingSlippery : TimedEffect
    {
        public override void OnStart()
        {
            OverrideAllSurfacesSlippery.IsActive = true;
        }

        public override void OnEnd()
        {
            OverrideAllSurfacesSlippery.IsActive = false;
        }
    }
}
