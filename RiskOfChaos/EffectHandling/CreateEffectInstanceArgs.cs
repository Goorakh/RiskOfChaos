namespace RiskOfChaos.EffectHandling
{
    public readonly struct CreateEffectInstanceArgs
    {
        public readonly ulong RNGSeed;

        public CreateEffectInstanceArgs(ulong rngSeed)
        {
            RNGSeed = rngSeed;
        }
    }
}
