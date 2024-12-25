using R2API;
using RoR2;

namespace RiskOfChaos.Content
{
    public static class CustomProcTypes
    {
        public static ModdedProcType Repeated { get; private set; } = ModdedProcType.Invalid;

        public static ModdedProcType Bouncing { get; private set; } = ModdedProcType.Invalid;

        public static ModdedProcType BounceFinished { get; private set; } = ModdedProcType.Invalid;

        public static ModdedProcType Delayed { get; private set; } = ModdedProcType.Invalid;

        public static ModdedProcType Replaced { get; private set; } = ModdedProcType.Invalid;

        public static bool IsMarkerProc(ModdedProcType moddedProcType)
        {
            return moddedProcType == Repeated ||
                   moddedProcType == Bouncing ||
                   moddedProcType == BounceFinished ||
                   moddedProcType == Delayed ||
                   moddedProcType == Replaced;
        }

        [SystemInitializer]
        static void Init()
        {
            Repeated = ProcTypeAPI.ReserveProcType();
            Bouncing = ProcTypeAPI.ReserveProcType();
            BounceFinished = ProcTypeAPI.ReserveProcType();
            Delayed = ProcTypeAPI.ReserveProcType();
            Replaced = ProcTypeAPI.ReserveProcType();
        }
    }
}
