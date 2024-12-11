using R2API;
using RiskOfChaos.Content;
using RiskOfChaos.EffectDefinitions.Character;
using RiskOfChaos.ModificationController.Projectile;
using RiskOfChaos.Utilities;
using RiskOfChaos.Utilities.Extensions;
using RoR2;
using RoR2.Projectile;
using UnityEngine;

namespace RiskOfChaos.Patches.AttackHooks
{
    abstract class AttackHookManager
    {
        public static AttackContext Context;

        protected abstract AttackInfo AttackInfo { get; }

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

            tryKnockback(activeAttackHooks);

            return activatedAttackHooks;
        }

        protected abstract void fireAttackCopy();

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
            AttackInfo.PopulateFireProjectileInfo(ref fireProjectileInfo);

            if (fireProjectileInfo.damage <= 0f && fireProjectileInfo.force <= 0f)
                return false;

            if (fireProjectileInfo.procChainMask.HasModdedProc(CustomProcTypes.Replaced))
                return false;

            if (fireProjectileInfo.rotation == Quaternion.identity)
            {
                fireProjectileInfo.rotation = QuaternionUtils.Spread(WorldUtils.GetWorldUpByGravity(), 20f, RoR2Application.rng);
            }

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
            if ((activeAttackHooks & AttackHookMask.Replaced) == 0)
            {
                if (AttackInfo.ProcChainMask.HasAnyProc())
                {
                    return false;
                }
            }

            return AttackMultiSpawnHook.TryMultiSpawn(fireAttackCopy, activeAttackHooks);
        }

        protected virtual bool tryFireBounce(AttackHookMask activeAttackHooks)
        {
            return false;
        }

        protected virtual bool tryKnockback(AttackHookMask activeAttackHooks)
        {
            if ((activeAttackHooks & AttackHookMask.Replaced) == 0)
            {
                if (AttackInfo.ProcChainMask.HasAnyProc())
                {
                    return false;
                }
            }

            return AttackKnockback.TryKnockbackBody(AttackInfo);
        }
    }
}
