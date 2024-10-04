namespace RiskOfChaos.EffectHandling.Controllers
{
    public struct ChaosEffectDispatchArgs
    {
        public EffectDispatchFlags DispatchFlags = EffectDispatchFlags.None;

        public ulong RNGSeed;

        public TimedEffectType? OverrideDurationType;
        public float? OverrideDuration;

        public ChaosEffectDispatchArgs()
        {
        }

        public readonly bool HasFlag(EffectDispatchFlags flag)
        {
            return (DispatchFlags & flag) != 0;
        }
    }
}
