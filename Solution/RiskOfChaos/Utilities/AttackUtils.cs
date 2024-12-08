using RiskOfChaos.Utilities.Extensions;
using RoR2;
using System.Reflection;

namespace RiskOfChaos.Utilities
{
    public static class AttackUtils
    {
        public static BulletAttack Clone(BulletAttack src)
        {
            return src.ShallowCopy(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        }

        public static BlastAttack Clone(BlastAttack src)
        {
            return src.ShallowCopy();
        }

        public static OverlapAttack Clone(OverlapAttack src)
        {
            return src.ShallowCopy(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        }
    }
}
