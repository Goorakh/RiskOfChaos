using RiskOfChaos.EffectHandling;
using RiskOfChaos.EffectHandling.Formatting;
using RiskOfChaos.Utilities;

namespace RiskOfChaos.UI.NextEffectDisplay
{
    public readonly record struct EffectDisplayData(ChaosEffectIndex EffectIndex, RunTimeStamp ActivationTime, EffectNameFormatter NameFormatter);
}
