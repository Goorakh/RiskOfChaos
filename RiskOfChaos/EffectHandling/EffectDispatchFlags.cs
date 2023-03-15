using System;

namespace RiskOfChaos.EffectHandling
{
    [Flags]
    public enum EffectDispatchFlags : byte
    {
        None,
        DontPlaySound = 1 << 0,
        DontStopTimedEffects = 1 << 1
    }
}
