using R2API;
using RoR2;
using System.Runtime.CompilerServices;

namespace RiskOfChaos.Utilities.Extensions
{
    public static class ProcChainMaskExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool HasAnyProc(this ProcChainMask mask)
        {
            // ProcTypeAPI compatibility:
            // Compare to zero struct instead of checking the mask field
            return !mask.Equals(default);
        }

        public static void AddProcsFrom(this ref ProcChainMask dst, in ProcChainMask src)
        {
            for (ProcType procType = 0; procType < ProcType.Count; procType++)
            {
                if (src.HasProc(procType))
                {
                    dst.AddProc(procType);
                }
            }

            for (ModdedProcType moddedProcType = 0; moddedProcType < (ModdedProcType)ProcTypeAPI.ModdedProcTypeCount; moddedProcType++)
            {
                if (src.HasModdedProc(moddedProcType))
                {
                    dst.AddModdedProc(moddedProcType);
                }
            }
        }
    }
}
