using System;

namespace RiskOfChaos.Patches.AttackHooks
{
    [Flags]
    public enum AttackHookMask
    {
        None = 0,
        Delayed = 1 << 0,
        Repeat = 1 << 1,
        Bounced = 1 << 2,
        Replaced = 1 << 3,
    }
}
