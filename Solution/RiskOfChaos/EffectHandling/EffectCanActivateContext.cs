using RiskOfChaos.Utilities;
using RoR2;

namespace RiskOfChaos.EffectHandling
{
    public record struct EffectCanActivateContext(RunTimeStamp ActivationTime, bool IsShortcut)
    {
        public static EffectCanActivateContext Now => new EffectCanActivateContext(Run.FixedTimeStamp.now);

        public static EffectCanActivateContext Now_Shortcut => new EffectCanActivateContext(Run.FixedTimeStamp.now, true);

        public EffectCanActivateContext(RunTimeStamp ActivationTime) : this(ActivationTime, false)
        {
        }

        public readonly bool IsNow => ActivationTime.HasPassed;
    }
}
