namespace RiskOfChaos.EffectHandling
{
    public record struct OverrideEffect(ChaosEffectInfo Effect, float? OverrideWeight)
    {
        public readonly float GetWeight()
        {
            return OverrideWeight ?? Effect.TotalSelectionWeight;
        }
    }
}
