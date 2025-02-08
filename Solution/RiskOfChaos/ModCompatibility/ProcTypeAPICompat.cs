using BepInEx;
using BepInEx.Bootstrap;
using R2API;
using System;
using System.Runtime.CompilerServices;

namespace RiskOfChaos.ModCompatibility
{
    static class ProcTypeAPICompat
    {
        static bool _isInitialized;

        public static ModdedProcType MinProcType { get; private set; }

        static int _maxValueOffset = 0;

        public static ModdedProcType MaxProcType
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return (ModdedProcType)(ProcTypeAPI.ModdedProcTypeCount + _maxValueOffset);
            }
        }

        public static void Init()
        {
            if (_isInitialized)
                return;

            ModdedProcType minValue = ModdedProcType.Invalid + 1;
            int maxValueOffset = 0;

            if (Chainloader.PluginInfos.TryGetValue(ProcTypeAPI.PluginGUID, out PluginInfo procTypeApiPlugin))
            {
                Log.Debug($"Running ProcTypeAPI version {procTypeApiPlugin.Metadata.Version}");

                if (procTypeApiPlugin.Metadata.Version < new Version(1, 0, 2))
                {
                    minValue = 0;
                    maxValueOffset = -1;
                }
            }
            else
            {
                Log.Error("Failed to find ProcTypeAPI plugin");
            }

            MinProcType = minValue;
            _maxValueOffset = maxValueOffset;

            Log.Debug($"MinProcType={MinProcType}, maxValueOffset={_maxValueOffset}");

            _isInitialized = true;
        }
    }
}
