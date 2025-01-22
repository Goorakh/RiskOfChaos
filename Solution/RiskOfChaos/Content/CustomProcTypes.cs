using R2API;
using RoR2;
using System;

namespace RiskOfChaos.Content
{
    public static class CustomProcTypes
    {
        public static ModdedProcType Repeated { get; private set; } = ModdedProcType.Invalid;

        public static ModdedProcType Bouncing { get; private set; } = ModdedProcType.Invalid;

        public static ModdedProcType BounceFinished { get; private set; } = ModdedProcType.Invalid;

        public static ModdedProcType Delayed { get; private set; } = ModdedProcType.Invalid;

        public static ModdedProcType Replaced { get; private set; } = ModdedProcType.Invalid;

        public static ModdedProcType KnockbackApplied { get; private set; } = ModdedProcType.Invalid;

        static ModdedProcType[] _markerProcTypes = [];

        public static bool IsMarkerProc(ModdedProcType moddedProcType)
        {
            return Array.BinarySearch(_markerProcTypes, moddedProcType) >= 0;
        }

        [SystemInitializer]
        static void Init()
        {
            Repeated = ProcTypeAPI.ReserveProcType();
            Bouncing = ProcTypeAPI.ReserveProcType();
            BounceFinished = ProcTypeAPI.ReserveProcType();
            Delayed = ProcTypeAPI.ReserveProcType();
            Replaced = ProcTypeAPI.ReserveProcType();
            KnockbackApplied = ProcTypeAPI.ReserveProcType();

            _markerProcTypes = [
                Repeated,
                Bouncing,
                BounceFinished,
                Delayed,
                Replaced,
                KnockbackApplied,
            ];

            Array.Sort(_markerProcTypes);
        }
    }
}
