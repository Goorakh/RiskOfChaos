using R2API;
using RiskOfChaos.Content;
using RiskOfChaos.Utilities.Extensions;
using RiskOfChaos_PatcherInterop;
using RoR2;
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

        protected override bool tryReplace(AttackHookMask activeAttackHooks)
        {
            int projectileIndex = ProjectileCatalog.GetProjectileIndex(_fireProjectileInfo.projectilePrefab);
            if (projectileIndex != -1)
            {
                if (projectileIndex == ProjectileCatalog.FindProjectileIndex("MageIcewallWalkerProjectile"))
                    return false;
            }

            return base.tryReplace(activeAttackHooks);
        }

        protected override bool setupProjectileFireInfo(ref FireProjectileInfo fireProjectileInfo)
        {
            fireProjectileInfo = _fireProjectileInfo;

            if (!fireProjectileInfo.GetProcCoefficientOverride().HasValue)
            {
                if (fireProjectileInfo.projectilePrefab && fireProjectileInfo.projectilePrefab.TryGetComponent(out ProjectileController projectileController))
                {
                    fireProjectileInfo.SetProcCoefficientOverride(projectileController.procCoefficient);
                }
            }

            return true;
        }

        protected override bool tryFireRepeating(AttackHookMask activeAttackHooks)
        {
            return (_fireProjectileInfo.procChainMask.HasModdedProc(CustomProcTypes.Replaced) || !_fireProjectileInfo.procChainMask.HasAnyProc()) && base.tryFireRepeating(activeAttackHooks);
        }
    }
}
