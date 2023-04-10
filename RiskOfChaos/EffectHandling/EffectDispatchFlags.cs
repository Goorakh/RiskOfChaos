using System;

namespace RiskOfChaos.EffectHandling
{
    [Flags]
    public enum EffectDispatchFlags : uint
    {
        None,
        DontPlaySound = 1 << 0,
        DontStopTimedEffects = 1 << 1,
        DontStart = 1 << 2,
        CheckCanActivate = 1 << 3,
    }
}
