using R2API;
using RiskOfChaos.Content;
using RoR2;

namespace RiskOfChaos.Utilities.Extensions
{
    public static class ProcChainMaskExtensions
    {
        public static bool HasAnyProc(this in ProcChainMask procChain)
        {
            if (procChain.mask != 0)
                return true;

            for (ModdedProcType moddedProcType = ModdedProcType.Invalid + 1; moddedProcType <= (ModdedProcType)ProcTypeAPI.ModdedProcTypeCount; moddedProcType++)
            {
                if (CustomProcTypes.IsMarkerProc(moddedProcType))
                    continue;

                if (procChain.HasModdedProc(moddedProcType))
                {
                    return true;
                }
            }

            return false;
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

            for (ModdedProcType moddedProcType = ModdedProcType.Invalid + 1; moddedProcType <= (ModdedProcType)ProcTypeAPI.ModdedProcTypeCount; moddedProcType++)
            {
                if (src.HasModdedProc(moddedProcType))
                {
                    dst.AddModdedProc(moddedProcType);
                }
            }
        }
    }
}
