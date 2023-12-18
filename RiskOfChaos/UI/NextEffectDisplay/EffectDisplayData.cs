using RiskOfChaos.EffectHandling;
using RiskOfChaos.EffectHandling.Formatting;

namespace RiskOfChaos.UI.NextEffectDisplay
{
    public readonly record struct EffectDisplayData(ChaosEffectIndex EffectIndex, float TimeRemaining, EffectNameFormatter NameFormatter);
}
