using R2API;
using System;

namespace RiskOfChaos.Content
{
    public static class CustomProcTypes
    {
        public static readonly ModdedProcType Repeated;

        public static readonly ModdedProcType Bouncing;

        public static readonly ModdedProcType BounceFinished;

        public static readonly ModdedProcType Delayed;

        public static readonly ModdedProcType Replaced;

        public static readonly ModdedProcType KnockbackApplied;

        static readonly ModdedProcType[] _markerProcTypes;

        static CustomProcTypes()
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

        public static bool IsMarkerProc(ModdedProcType moddedProcType)
        {
            return Array.BinarySearch(_markerProcTypes, moddedProcType) >= 0;
        }
    }
}
