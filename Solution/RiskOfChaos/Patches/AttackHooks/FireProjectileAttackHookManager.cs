using RoR2;
using RoR2.Projectile;

namespace RiskOfChaos.Patches.AttackHooks
{
    class FireProjectileAttackHookManager : AttackHookManager
    {
        readonly ProjectileManager _projectileManager;
        readonly FireProjectileInfo _fireProjectileInfo;

        protected override AttackInfo AttackInfo { get; }

        public FireProjectileAttackHookManager(ProjectileManager projectileManager, FireProjectileInfo fireProjectileInfo)
        {
            _projectileManager = projectileManager;
            _fireProjectileInfo = fireProjectileInfo;

            AttackInfo = new AttackInfo(_fireProjectileInfo);
        }

        ProjectileManager getProjectileManager()
        {
            if (_projectileManager)
                return _projectileManager;

            return ProjectileManager.instance;
        }

        protected override void fireAttackCopy(AttackInfo attackInfo)
        {
            ProjectileManager projectileManager = getProjectileManager();
            if (!projectileManager)
                return;

            FireProjectileInfo fireProjectileInfo = _fireProjectileInfo;
            attackInfo.PopulateFireProjectileInfo(ref fireProjectileInfo);
            projectileManager.FireProjectile(fireProjectileInfo);
        }
        
        protected override bool tryReplace()
        {
            int projectileIndex = ProjectileCatalog.GetProjectileIndex(_fireProjectileInfo.projectilePrefab);
            if (projectileIndex != -1)
            {
                if (projectileIndex == ProjectileCatalog.FindProjectileIndex("MageIcewallWalkerProjectile"))
                    return false;
            }

            return base.tryReplace();
        }
    }
}
