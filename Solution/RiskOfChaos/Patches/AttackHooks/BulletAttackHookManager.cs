using RiskOfChaos.Utilities;
using RoR2;

namespace RiskOfChaos.Patches.AttackHooks
{
    sealed class BulletAttackHookManager : AttackHookManager
    {
        readonly BulletAttack _bulletAttack;
        readonly BulletAttack _bulletAttackTemplate;
        readonly BulletAttack.FireSingleArgs _fireArgs;

        protected override AttackInfo AttackInfo { get; }

        public BulletAttackHookManager(BulletAttack bulletAttack, BulletAttack.FireSingleArgs fireArgs)
        {
            _bulletAttack = bulletAttack;
            _bulletAttackTemplate = AttackUtils.Clone(bulletAttack);
            _fireArgs = fireArgs;

            AttackInfo = new AttackInfo(_bulletAttack, _fireArgs);
        }

        protected override void fireAttackCopy(AttackInfo attackInfo)
        {
            BulletAttack bulletAttackCopy = AttackUtils.Clone(_bulletAttackTemplate);
            attackInfo.PopulateBulletAttack(bulletAttackCopy);
            bulletAttackCopy.FireSingle(_fireArgs);
        }

        protected override bool tryFireBounce()
        {
            return BulletBounceHook.TryStartBounce(_bulletAttack, AttackInfo);
        }
    }
}
