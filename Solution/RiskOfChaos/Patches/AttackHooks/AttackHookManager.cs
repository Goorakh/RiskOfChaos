using R2API;
using RiskOfChaos.Content;
using RiskOfChaos.ModificationController.Projectile;
using RoR2;
using RoR2.Projectile;

namespace RiskOfChaos.Patches.AttackHooks
{
    abstract class AttackHookManager
    {
        public static AttackContext Context;

        public AttackHookMask RunHooks()
        {
            AttackHookMask activeAttackHooks = Context.Pop();
            return runHooksInternal(activeAttackHooks);   
        }

        protected virtual AttackHookMask runHooksInternal(AttackHookMask activeAttackHooks)
        {
            AttackHookMask activatedAttackHooks = AttackHookMask.None;

            if ((activeAttackHooks & AttackHookMask.Replaced) == 0)
            {
                if (tryReplace(activeAttackHooks))
                {
                    activatedAttackHooks |= AttackHookMask.Replaced;
                    return activatedAttackHooks;
                }
            }

            if ((activeAttackHooks & AttackHookMask.Delayed) == 0)
            {
                if (tryFireDelayed(activeAttackHooks))
                {
                    activatedAttackHooks |= AttackHookMask.Delayed;
                    return activatedAttackHooks;
                }
            }

            if ((activeAttackHooks & AttackHookMask.Bounced) == 0)
            {
                if ((activeAttackHooks & AttackHookMask.Repeat) == 0)
                {
                    if (tryFireRepeating(activeAttackHooks))
                    {
                        activatedAttackHooks |= AttackHookMask.Repeat;
                    }
                }

                if (tryFireBounce(activeAttackHooks))
                {
                    activatedAttackHooks |= AttackHookMask.Bounced;
                }
            }

            return activatedAttackHooks;
        }

        protected abstract void fireAttackCopy();

        protected abstract bool setupProjectileFireInfo(ref FireProjectileInfo fireProjectileInfo);

        protected virtual bool tryReplace(AttackHookMask activeAttackHooks)
        {
            int overrideProjectileIndex = -1;
            if (ProjectileModificationManager.Instance)
            {
                overrideProjectileIndex = ProjectileModificationManager.Instance.OverrideProjectileIndex;
            }

            if (overrideProjectileIndex == -1)
                return false;

            FireProjectileInfo fireProjectileInfo = new FireProjectileInfo();
            if (!setupProjectileFireInfo(ref fireProjectileInfo))
                return false;

            if (fireProjectileInfo.damage <= 0f && fireProjectileInfo.force <= 0f)
                return false;

            if (fireProjectileInfo.procChainMask.HasModdedProc(CustomProcTypes.Replaced))
                return false;

            fireProjectileInfo.projectilePrefab = ProjectileCatalog.GetProjectilePrefab(overrideProjectileIndex);
            fireProjectileInfo.procChainMask.AddModdedProc(CustomProcTypes.Replaced);

            Context.Activate(activeAttackHooks | AttackHookMask.Replaced);
            ProjectileManager.instance.FireProjectile(fireProjectileInfo);

            return true;
        }

        protected virtual bool tryFireDelayed(AttackHookMask activeAttackHooks)
        {
            return AttackDelayHooks.TryDelayAttack(fireAttackCopy, activeAttackHooks);
        }

        protected virtual bool tryFireRepeating(AttackHookMask activeAttackHooks)
        {
            return AttackMultiSpawnHook.TryMultiSpawn(fireAttackCopy, activeAttackHooks);
        }

        protected virtual bool tryFireBounce(AttackHookMask activeAttackHooks)
        {
            return false;
        }
    }
}
