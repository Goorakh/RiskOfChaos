using RiskOfChaos.EffectHandling;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.Patches;

namespace RiskOfChaos.EffectDefinitions.World
{
    [ChaosEffect("everything_slippery", EffectActivationCountHardCap = 1, IsNetworked = true)]
    public sealed class EverythingSlippery : TimedEffect
    {
        public override TimedEffectType TimedType => TimedEffectType.UntilStageEnd;

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
