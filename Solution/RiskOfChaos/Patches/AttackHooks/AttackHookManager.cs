using R2API;
using RiskOfChaos.Content;
using RiskOfChaos.EffectDefinitions.Character;
using RiskOfChaos.ModificationController.Projectile;
using RoR2;
using RoR2.Projectile;
using UnityEngine;

namespace RiskOfChaos.Patches.AttackHooks
{
    abstract class AttackHookManager
    {
        public delegate void FireAttackDelegate(AttackInfo attackInfo);

        protected abstract AttackInfo AttackInfo { get; }

        public AttackHookMask RunHooks()
        {
            return runHooksInternal();   
        }

        protected virtual AttackHookMask runHooksInternal()
        {
            AttackHookMask activatedAttackHooks = AttackHookMask.None;

            ProcChainMask procChainMask = AttackInfo.ProcChainMask;

            if (!procChainMask.HasModdedProc(CustomProcTypes.Replaced))
            {
                if (tryReplace())
                {
                    activatedAttackHooks |= AttackHookMask.Replaced;
                    return activatedAttackHooks;
                }
            }

            if (!procChainMask.HasModdedProc(CustomProcTypes.Delayed))
            {
                if (tryFireDelayed())
                {
                    activatedAttackHooks |= AttackHookMask.Delayed;
                    return activatedAttackHooks;
                }
            }

            if (!procChainMask.HasModdedProc(CustomProcTypes.Bouncing))
            {
                if (!procChainMask.HasModdedProc(CustomProcTypes.Repeated))
                {
                    if (tryFireRepeating())
                    {
                        activatedAttackHooks |= AttackHookMask.Repeat;
                    }
                }

                if (tryFireBounce())
                {
                    activatedAttackHooks |= AttackHookMask.Bounced;
                }
            }

            if (tryKnockback())
            {
                activatedAttackHooks |= AttackHookMask.Knockback;
            }

            return activatedAttackHooks;
        }

        protected abstract void fireAttackCopy(AttackInfo attackInfo);

        protected virtual bool tryReplace()
        {
            int overrideProjectileIndex = -1;
            if (ProjectileModificationManager.Instance)
            {
                overrideProjectileIndex = ProjectileModificationManager.Instance.OverrideProjectileIndex;
            }

            if (overrideProjectileIndex == -1)
                return false;

            GameObject overrideProjectilePrefab = ProjectileCatalog.GetProjectilePrefab(overrideProjectileIndex);
            if (!overrideProjectilePrefab)
                return false;

            FireProjectileInfo fireProjectileInfo = new FireProjectileInfo();
            AttackInfo.PopulateFireProjectileInfo(ref fireProjectileInfo);

            if (fireProjectileInfo.damage <= 0f && fireProjectileInfo.force <= 0f)
                return false;

            fireProjectileInfo.projectilePrefab = overrideProjectilePrefab;
            fireProjectileInfo.procChainMask.AddModdedProc(CustomProcTypes.Replaced);

            ProjectileManager.instance.FireProjectile(fireProjectileInfo);

            return true;
        }

        protected virtual bool tryFireDelayed()
        {
            return AttackDelayHooks.TryDelayAttack(fireAttackCopy, AttackInfo);
        }

        protected virtual bool tryFireRepeating()
        {
            return AttackMultiSpawnHook.TryMultiSpawn(fireAttackCopy, AttackInfo);
        }

        protected virtual bool tryFireBounce()
        {
            return false;
        }

        protected virtual bool tryKnockback()
        {
            return AttackKnockback.TryKnockbackBody(AttackInfo);
        }
    }
}
