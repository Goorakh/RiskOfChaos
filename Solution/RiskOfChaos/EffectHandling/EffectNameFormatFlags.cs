using System;

namespace RiskOfChaos.EffectHandling
{
    [Flags]
    public enum EffectNameFormatFlags : byte
    {
        None = 0,
        RuntimeFormatArgs = 1 << 0,
        TimedType = 1 << 1,
        All = byte.MaxValue
    }
}
