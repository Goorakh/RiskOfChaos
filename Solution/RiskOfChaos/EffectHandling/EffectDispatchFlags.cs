using System;

namespace RiskOfChaos.EffectHandling
{
    [Flags]
    public enum EffectDispatchFlags : uint
    {
        None,
        DontPlaySound = 1 << 0,
        CheckCanActivate = 1 << 1,
        DontSendChatMessage = 1 << 2
    }
}
