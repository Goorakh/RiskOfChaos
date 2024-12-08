using RiskOfChaos.Utilities;
using RoR2;

namespace RiskOfChaos.Patches.AttackHooks
{
    class BlastAttackHookManager : AttackHookManager
    {
        readonly BlastAttack _blastAttack;

        public BlastAttackHookManager(BlastAttack blastAttack)
        {
            _blastAttack = AttackUtils.Clone(blastAttack);
        }

        protected override void fireAttackCopy()
        {
            AttackUtils.Clone(_blastAttack).Fire();
        }
    }
}
