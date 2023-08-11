using RiskOfChaos.ModifierController.AttackDelay;
using RiskOfChaos.Utilities.Extensions;
using RoR2;
using RoR2.Orbs;
using RoR2.Projectile;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace RiskOfChaos.Patches
{
    static class AttackDelayHook
    {
        static readonly TimerQueue _delayedAttackTimers = new TimerQueue();

        static float totalDelay
        {
            get
            {
                AttackDelayModificationManager instance = AttackDelayModificationManager.Instance;
                if (!instance)
                    return 0f;

                return instance.NetworkedTotalAttackDelay;
            }
        }

        [SystemInitializer]
        static void Init()
        {
            On.RoR2.BulletAttack.Fire += BulletAttack_Fire;
            On.RoR2.BlastAttack.Fire += BlastAttack_Fire;
            On.RoR2.OverlapAttack.Fire += OverlapAttack_Fire;
            On.RoR2.Projectile.ProjectileManager.FireProjectile_FireProjectileInfo += ProjectileManager_FireProjectile_FireProjectileInfo;
            On.RoR2.Orbs.OrbManager.AddOrb += OrbManager_AddOrb;

            Run.onRunStartGlobal += _ =>
            {
                _delayedAttackTimers.Clear();
                RoR2Application.onUpdate += update;
            };

            Run.onRunDestroyGlobal += _ =>
            {
                RoR2Application.onUpdate -= update;
                _delayedAttackTimers.Clear();
            };

            Stage.onServerStageComplete += _ =>
            {
                _delayedAttackTimers.Clear();
            };
        }

        static void update()
        {
            _delayedAttackTimers.Update(Time.deltaTime);
        }

        static void tryDelayAttack(Action orig)
        {
            if (_delayedAttackTimers == null || totalDelay <= 0f)
            {
                orig();
            }
            else
            {
                _delayedAttackTimers.CreateTimer(totalDelay, orig);
            }
        }

        static bool tryDelayAttack<T>(Func<T> orig, out T result)
        {
            if (_delayedAttackTimers == null || totalDelay <= 0f)
            {
                result = orig();
                return true;
            }
            else
            {
                _delayedAttackTimers.CreateTimer(totalDelay, () => orig());
                result = default;
                return false;
            }
        }

        static void BulletAttack_Fire(On.RoR2.BulletAttack.orig_Fire orig, BulletAttack self)
        {
            tryDelayAttack(() => orig(self));
        }

        static BlastAttack.Result BlastAttack_Fire(On.RoR2.BlastAttack.orig_Fire orig, BlastAttack self)
        {
            if (tryDelayAttack(() => orig(self), out BlastAttack.Result result))
            {
                return result;
            }
            else
            {
                return new BlastAttack.Result()
                {
                    hitCount = 0,
                    hitPoints = Array.Empty<BlastAttack.HitPoint>()
                };
            }
        }

        static bool OverlapAttack_Fire(On.RoR2.OverlapAttack.orig_Fire orig, OverlapAttack self, List<HurtBox> hitResults)
        {
            if (tryDelayAttack(() => orig(self, hitResults), out bool result))
            {
                return result;
            }
            else
            {
                return false;
            }
        }

        static void ProjectileManager_FireProjectile_FireProjectileInfo(On.RoR2.Projectile.ProjectileManager.orig_FireProjectile_FireProjectileInfo orig, ProjectileManager self, FireProjectileInfo fireProjectileInfo)
        {
            tryDelayAttack(() => orig(self, fireProjectileInfo));
        }

        static void OrbManager_AddOrb(On.RoR2.Orbs.OrbManager.orig_AddOrb orig, OrbManager self, Orb orb)
        {
            tryDelayAttack(() => orig(self, orb));
        }
    }
}
