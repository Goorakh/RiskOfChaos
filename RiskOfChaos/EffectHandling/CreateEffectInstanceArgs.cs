namespace RiskOfChaos.EffectHandling
{
    public readonly struct CreateEffectInstanceArgs
    {
        public readonly ulong DispatchID;
        public readonly ulong RNGSeed;

        public CreateEffectInstanceArgs(ulong dispatchID, ulong rngSeed)
        {
            DispatchID = dispatchID;
            RNGSeed = rngSeed;
        }
    }
}
