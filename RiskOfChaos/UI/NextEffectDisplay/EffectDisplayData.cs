using RiskOfChaos.EffectHandling;
using System;

namespace RiskOfChaos.UI.NextEffectDisplay
{
    public readonly record struct EffectDisplayData(ChaosEffectIndex EffectIndex, float TimeRemaining, string[] DisplayNameFormatArgs)
    {
        public static readonly EffectDisplayData None = new EffectDisplayData(ChaosEffectIndex.Invalid, -1f, Array.Empty<string>());
    }
}
