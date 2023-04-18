using System;

namespace RiskOfChaos.EffectHandling
{
    public enum TimedEffectType : byte
    {
        UntilNextEffect,
        UntilStageEnd,
        FixedDuration,
    }

    [Flags]
    public enum TimedEffectFlags
    {
        None = 0,
        UntilNextEffect = 1 << TimedEffectType.UntilNextEffect,
        UntilStageEnd = 1 << TimedEffectType.UntilStageEnd,
        FixedDuration = 1 << TimedEffectType.FixedDuration,
        All = ~0b0
    }
}
