using RiskOfChaos.EffectHandling;

namespace RiskOfChaos.UI.NextEffectDisplay
{
    public readonly record struct EffectDisplayData(ChaosEffectIndex EffectIndex, float TimeRemaining)
    {
        public static readonly EffectDisplayData None = new EffectDisplayData(ChaosEffectIndex.Invalid, -1f);
    }
}
