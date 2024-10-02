using System;

namespace RiskOfChaos.EffectHandling
{
    [Flags]
    public enum EffectDispatchFlags : uint
    {
        None,
        DontPlaySound = 1 << 0,
        DontStart = 1 << 1,
        CheckCanActivate = 1 << 2,
        DontSendChatMessage = 1 << 3,
        DontCount = 1 << 4,
        SkipServerInit = 1 << 5,

        LoadedFromSave = DontPlaySound | DontSendChatMessage | DontCount | SkipServerInit
    }
}
