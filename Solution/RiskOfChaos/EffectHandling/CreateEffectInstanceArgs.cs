namespace RiskOfChaos.EffectHandling
{
    public record struct CreateEffectInstanceArgs(ulong DispatchID, ulong RNGSeed, TimedEffectType? OverrideDurationType)
    {
        public static readonly CreateEffectInstanceArgs None = new CreateEffectInstanceArgs(0, 0, null);
    }
}
