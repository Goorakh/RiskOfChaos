using RoR2;

namespace RiskOfChaos.Utilities
{
    public static class ConVarFlagUtil
    {
        public const ConVarFlags SERVER =
#if DEBUG
            ConVarFlags.ExecuteOnServer;
#else
            ConVarFlags.SenderMustBeServer;
#endif
    }
}
