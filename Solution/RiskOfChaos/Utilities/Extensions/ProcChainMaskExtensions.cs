using R2API;
using RiskOfChaos.Content;
using RiskOfChaos.ModCompatibility;
using RoR2;

namespace RiskOfChaos.Utilities.Extensions
{
    public static class ProcChainMaskExtensions
    {
        public static bool HasAnyProc(this in ProcChainMask procChain)
        {
            if (procChain.mask != 0)
                return true;

            for (ModdedProcType moddedProcType = ProcTypeAPICompat.MinProcType; moddedProcType <= ProcTypeAPICompat.MaxProcType; moddedProcType++)
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

            for (ModdedProcType moddedProcType = ProcTypeAPICompat.MinProcType; moddedProcType <= ProcTypeAPICompat.MaxProcType; moddedProcType++)
            {
                if (src.HasModdedProc(moddedProcType))
                {
                    dst.AddModdedProc(moddedProcType);
                }
            }
        }
    }
}
