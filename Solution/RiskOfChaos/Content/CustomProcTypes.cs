using R2API;
using RoR2;

namespace RiskOfChaos.Content
{
    public static class CustomProcTypes
    {
        public static ModdedProcType Repeated { get; private set; }

        public static ModdedProcType Bounced { get; private set; }

        [SystemInitializer]
        static void Init()
        {
            Repeated = ProcTypeAPI.ReserveProcType();
            Bounced = ProcTypeAPI.ReserveProcType();
        }
    }
}
