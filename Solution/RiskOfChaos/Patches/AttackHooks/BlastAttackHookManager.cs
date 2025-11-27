using RiskOfChaos.Utilities;
using RoR2;

namespace RiskOfChaos.Patches.AttackHooks
{
    sealed class BlastAttackHookManager : AttackHookManager
    {
        readonly BlastAttack _blastAttack;

        protected override AttackInfo AttackInfo { get; }

        public BlastAttackHookManager(BlastAttack blastAttack)
        {
            _blastAttack = AttackUtils.Clone(blastAttack);

            AttackInfo = new AttackInfo(_blastAttack);
        }

        protected override void fireAttackCopy(AttackInfo attackInfo)
        {
            BlastAttack blastAttack = AttackUtils.Clone(_blastAttack);
            attackInfo.PopulateBlastAttack(blastAttack);
            blastAttack.Fire();
        }
    }
}
