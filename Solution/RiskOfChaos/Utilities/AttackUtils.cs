using RiskOfChaos.Utilities.Extensions;
using RoR2;
using System.Reflection;

namespace RiskOfChaos.Utilities
{
    public static class AttackUtils
    {
        public static BulletAttack Clone(BulletAttack src)
        {
            if (src == null)
                return null;

            return src.ShallowCopy(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        }

        public static BlastAttack Clone(BlastAttack src)
        {
            if (src == null)
                return null;

            return src.ShallowCopy();
        }

        public static OverlapAttack Clone(OverlapAttack src)
        {
            if (src == null)
                return null;

            return src.ShallowCopy(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        }
    }
}
