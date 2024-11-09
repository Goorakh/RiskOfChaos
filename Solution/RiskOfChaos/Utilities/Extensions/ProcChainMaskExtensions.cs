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
    }
}
