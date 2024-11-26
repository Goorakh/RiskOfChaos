using R2API;
using RoR2;

namespace RiskOfChaos.Content
{
    public static class CustomProcTypes
    {
        public static ModdedProcType Repeated { get; private set; }

        public static ModdedProcType Bouncing { get; private set; }

        public static ModdedProcType BounceChainEnd { get; private set; }

        [SystemInitializer]
        static void Init()
        {
            Repeated = ProcTypeAPI.ReserveProcType();
            Bouncing = ProcTypeAPI.ReserveProcType();
            BounceChainEnd = ProcTypeAPI.ReserveProcType();
        }
    }
}
