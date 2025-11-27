using RoR2;
using RoR2.Projectile;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace RiskOfChaos.Patches.AttackHooks
{
    sealed class FireProjectileAttackHookManager : AttackHookManager
    {
        static int[] _projectileReplaceBlacklist = [];
        static int[] _projectileCopyBlacklist = [];

        [SystemInitializer(typeof(ProjectileCatalog))]
        static void Init()
        {
            static bool tryAddProjectileByName(HashSet<int> set, string projectileName)
            {
                int projectileIndex = ProjectileCatalog.FindProjectileIndex(projectileName);
                if (projectileIndex == -1)
                {
                    Log.Warning($"Failed to find projectile '{projectileName}'");
                    return false;
                }

                return set.Add(projectileIndex);
            }

            HashSet<int> copyBlacklist = [];
            HashSet<int> replaceBlacklist = [];

            for (int i = 0; i < ProjectileCatalog.projectilePrefabCount; i++)
            {
                GameObject projectilePrefab = ProjectileCatalog.GetProjectilePrefab(i);
                if (!projectilePrefab)
                    continue;

                if (projectilePrefab.TryGetComponent(out ThrownObjectProjectileController vehicleSeat))
                {
                    Log.Debug($"Adding {projectilePrefab.name} to blacklists");

                    copyBlacklist.Add(i);
                    replaceBlacklist.Add(i);
                }
            }

            tryAddProjectileByName(replaceBlacklist, "MageIcewallWalkerProjectile");

            _projectileCopyBlacklist = [.. copyBlacklist];
            Array.Sort(_projectileCopyBlacklist);

            _projectileReplaceBlacklist = [.. replaceBlacklist];
            Array.Sort(_projectileReplaceBlacklist);
        }

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
            if (projectileIndex == -1 || Array.BinarySearch(_projectileReplaceBlacklist, projectileIndex) >= 0)
                return false;

            return base.tryReplace();
        }

        protected override bool tryFireDelayed()
        {
            int projectileIndex = ProjectileCatalog.GetProjectileIndex(_fireProjectileInfo.projectilePrefab);
            if (projectileIndex == -1 || Array.BinarySearch(_projectileCopyBlacklist, projectileIndex) >= 0)
                return false;

            return base.tryFireDelayed();
        }

        protected override bool tryFireRepeating()
        {
            int projectileIndex = ProjectileCatalog.GetProjectileIndex(_fireProjectileInfo.projectilePrefab);
            if (projectileIndex == -1 || Array.BinarySearch(_projectileCopyBlacklist, projectileIndex) >= 0)
                return false;

            return base.tryFireRepeating();
        }
    }
}
