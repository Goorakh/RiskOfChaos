using System;

namespace RiskOfChaos.EffectHandling
{
    public enum TimedEffectType : byte
    {
        UntilStageEnd,
        FixedDuration,
        Permanent,
        AlwaysActive
    }

    [Flags]
    public enum TimedEffectFlags
    {
        None = 0,
        UntilStageEnd = 1 << TimedEffectType.UntilStageEnd,
        FixedDuration = 1 << TimedEffectType.FixedDuration,
        Permanent = 1 << TimedEffectType.Permanent,
        AlwaysActive = 1 << TimedEffectType.AlwaysActive,
        All = ~0b0
    }
}
