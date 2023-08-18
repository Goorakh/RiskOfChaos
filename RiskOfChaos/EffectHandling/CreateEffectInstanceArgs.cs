namespace RiskOfChaos.EffectHandling
{
    public record struct CreateEffectInstanceArgs(ulong DispatchID, ulong RNGSeed)
    {
        public static readonly CreateEffectInstanceArgs None = new CreateEffectInstanceArgs(0, 0);
    }
}
