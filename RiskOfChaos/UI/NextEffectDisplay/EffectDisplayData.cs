using RiskOfChaos.EffectHandling;
using RiskOfChaos.EffectHandling.Formatting;

namespace RiskOfChaos.UI.NextEffectDisplay
{
    public readonly record struct EffectDisplayData(ChaosEffectIndex EffectIndex, float TimeRemaining, EffectNameFormatter NameFormatter)
    {
        public static readonly EffectDisplayData None = new EffectDisplayData(ChaosEffectIndex.Invalid, -1f, null);
    }
}
