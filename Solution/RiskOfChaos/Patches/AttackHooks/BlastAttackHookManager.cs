using RiskOfChaos.Utilities;
using RoR2;

namespace RiskOfChaos.Patches.AttackHooks
{
    class BlastAttackHookManager : AttackHookManager
    {
        readonly BlastAttack _blastAttack;

        protected override AttackInfo AttackInfo { get; }

        public BlastAttackHookManager(BlastAttack blastAttack)
        {
            _blastAttack = AttackUtils.Clone(blastAttack);

            AttackInfo = new AttackInfo(_blastAttack);
        }

        protected override void fireAttackCopy()
        {
            AttackUtils.Clone(_blastAttack).Fire();
        }
    }
}
