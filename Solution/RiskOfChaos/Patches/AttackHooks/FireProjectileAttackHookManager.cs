using RiskOfChaos.Utilities.Extensions;
using RoR2.Projectile;

namespace RiskOfChaos.Patches.AttackHooks
{
    class FireProjectileAttackHookManager : AttackHookManager
    {
        readonly ProjectileManager _projectileManager;
        readonly FireProjectileInfo _fireProjectileInfo;

        public FireProjectileAttackHookManager(ProjectileManager projectileManager, FireProjectileInfo fireProjectileInfo)
        {
            _projectileManager = projectileManager;
            _fireProjectileInfo = fireProjectileInfo;
        }

        ProjectileManager getProjectileManager()
        {
            if (_projectileManager)
                return _projectileManager;

            return ProjectileManager.instance;
        }

        protected override void fireAttackCopy()
        {
            ProjectileManager projectileManager = getProjectileManager();
            if (!projectileManager)
                return;

            projectileManager.FireProjectile(_fireProjectileInfo);
        }

        protected override bool tryFireRepeating(AttackHookMask activeAttackHooks)
        {
            return !_fireProjectileInfo.procChainMask.HasAnyProc() && base.tryFireRepeating(activeAttackHooks);
        }
    }
}
