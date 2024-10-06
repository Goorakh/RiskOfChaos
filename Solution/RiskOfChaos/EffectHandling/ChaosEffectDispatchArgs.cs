using RiskOfChaos.Utilities;

namespace RiskOfChaos.EffectHandling
{
    public struct ChaosEffectDispatchArgs
    {
        public EffectDispatchFlags DispatchFlags;

        public ulong RNGSeed;

        public RunTimeStamp? OverrideStartTime;

        public TimedEffectType? OverrideDurationType;
        public float? OverrideDuration;
    }
}
