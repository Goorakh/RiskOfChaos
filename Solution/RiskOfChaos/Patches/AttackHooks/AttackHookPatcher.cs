using RoR2;
using RoR2.Orbs;
using RoR2.Projectile;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

namespace RiskOfChaos.Patches.AttackHooks
{
    static class AttackHookPatcher
    {
        [SystemInitializer]
        static void Init()
        {
            // This makes all bullet attacks share the same collection instances which A) is dumb, and B) messes up a lot of hooks
            // This is not even for effect pooling, this saves like, a couple allocations per bullet, really not worth the trouble it causes
            BulletAttack._UsePools = false;

            On.RoR2.BulletAttack.FireMulti += BulletAttack_FireMulti;
            On.RoR2.BulletAttack.FireSingle += BulletAttack_FireSingle;
            On.RoR2.BulletAttack.FireSingle_ReturnHit += BulletAttack_FireSingle_ReturnHit;

            On.RoR2.BlastAttack.Fire += BlastAttack_Fire;
            On.RoR2.OverlapAttack.Fire += OverlapAttack_Fire;
            On.RoR2.Projectile.ProjectileManager.FireProjectile_FireProjectileInfo += ProjectileManager_FireProjectile_FireProjectileInfo;
            On.RoR2.Orbs.OrbManager.AddOrb += OrbManager_AddOrb;
        }

        static bool shouldSkipOrig(AttackHookMask activatedHooks)
        {
            return (activatedHooks & AttackHookMask.Delayed) != 0;
        }

        static void BulletAttack_FireMulti(On.RoR2.BulletAttack.orig_FireMulti orig, BulletAttack self, Vector3 normal, int muzzleIndex)
        {
            AttackHookManager attackHookManager = new BulletAttackHookManager(self, normal, muzzleIndex, BulletAttackHookManager.FireType.Multi);
            AttackHookMask activatedHooks = attackHookManager.RunHooks();
            if (shouldSkipOrig(activatedHooks))
                return;

            orig(self, normal, muzzleIndex);
        }

        static void BulletAttack_FireSingle(On.RoR2.BulletAttack.orig_FireSingle orig, BulletAttack self, Vector3 normal, int muzzleIndex)
        {
            AttackHookManager attackHookManager = new BulletAttackHookManager(self, normal, muzzleIndex, BulletAttackHookManager.FireType.Single);
            AttackHookMask activatedHooks = attackHookManager.RunHooks();
            if (shouldSkipOrig(activatedHooks))
                return;

            orig(self, normal, muzzleIndex);
        }

        static Vector3 BulletAttack_FireSingle_ReturnHit(On.RoR2.BulletAttack.orig_FireSingle_ReturnHit orig, BulletAttack self, Vector3 normal, int muzzleIndex)
        {
            AttackHookManager attackHookManager = new BulletAttackHookManager(self, normal, muzzleIndex, BulletAttackHookManager.FireType.Single_ReturnHit);
            AttackHookMask activatedHooks = attackHookManager.RunHooks();
            if (shouldSkipOrig(activatedHooks))
                return self.origin + (self.aimVector * self.maxDistance);

            return orig(self, normal, muzzleIndex);
        }

        static BlastAttack.Result BlastAttack_Fire(On.RoR2.BlastAttack.orig_Fire orig, BlastAttack self)
        {
            AttackHookManager attackHookManager = new BlastAttackHookManager(self);
            AttackHookMask activatedHooks = attackHookManager.RunHooks();
            if (shouldSkipOrig(activatedHooks))
                return new BlastAttack.Result { hitPoints = [] };

            return orig(self);
        }

        static bool OverlapAttack_Fire(On.RoR2.OverlapAttack.orig_Fire orig, OverlapAttack self, List<HurtBox> hitResults)
        {
            AttackHookManager attackHookManager = new OverlapAttackHookManager(self);
            AttackHookMask activatedHooks = attackHookManager.RunHooks();
            if (shouldSkipOrig(activatedHooks))
                return false;

            return orig(self, hitResults);
        }

        static void ProjectileManager_FireProjectile_FireProjectileInfo(On.RoR2.Projectile.ProjectileManager.orig_FireProjectile_FireProjectileInfo orig, ProjectileManager self, FireProjectileInfo fireProjectileInfo)
        {
            AttackHookManager attackHookManager = new FireProjectileAttackHookManager(self, fireProjectileInfo);
            AttackHookMask activatedHooks = attackHookManager.RunHooks();
            if (shouldSkipOrig(activatedHooks))
                return;

            orig(self, fireProjectileInfo);
        }

        static void OrbManager_AddOrb(On.RoR2.Orbs.OrbManager.orig_AddOrb orig, OrbManager self, Orb orb)
        {
            AttackHookManager attackHookManager = new FireOrbAttackHookManager(self, orb);
            AttackHookMask activatedHooks = attackHookManager.RunHooks();
            if (shouldSkipOrig(activatedHooks))
                return;

            orig(self, orb);
        }
    }
}
