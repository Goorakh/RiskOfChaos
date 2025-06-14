using EntityStates;
using HarmonyLib;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RoR2;
using RoR2.Orbs;
using RoR2.Projectile;
using System.Collections.Generic;
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
            On.RoR2.OverlapAttack.ProcessHits += OverlapAttack_ProcessHits;
            On.RoR2.Projectile.ProjectileManager.FireProjectile_FireProjectileInfo += ProjectileManager_FireProjectile_FireProjectileInfo;
            On.RoR2.Orbs.OrbManager.AddOrb += OrbManager_AddOrb;

            IL.EntityStates.GolemMonster.FireLaser.OnEnter += FireLaser_OnEnter;

            IL.EntityStates.Merc.Evis.FixedUpdate += Evis_FixedUpdate;
        }

        static bool shouldSkipOrig(AttackHookMask activatedHooks)
        {
            return (activatedHooks & (AttackHookMask.Delayed | AttackHookMask.Replaced)) != 0;
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

        static void OverlapAttack_ProcessHits(On.RoR2.OverlapAttack.orig_ProcessHits orig, OverlapAttack self, List<OverlapAttack.OverlapInfo> hitList)
        {
            AttackHookManager attackHookManager = new OverlapAttackHookManager(self, hitList);
            AttackHookMask activatedHooks = attackHookManager.RunHooks();
            if (shouldSkipOrig(activatedHooks))
                return;

            orig(self, hitList);
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

        static void FireLaser_OnEnter(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            ILLabel skipAttackLabel = null;
            if (!c.TryFindNext(out ILCursor[] cursors,
                               x => x.MatchCallOrCallvirt(AccessTools.DeclaredPropertyGetter(typeof(EntityState), nameof(EntityState.isAuthority))),
                               x => x.MatchBrfalse(out skipAttackLabel)))
            {
                Log.Error("Failed to find patch location");
                return;
            }

            ILCursor cursor = cursors[1];
            cursor.Index++;
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.EmitDelegate(runAttackHooks);
            cursor.Emit(OpCodes.Brfalse, skipAttackLabel);

            static bool runAttackHooks(EntityStates.GolemMonster.FireLaser fireLaserState)
            {
                AttackHookManager attackHookManager = new FireGolemLaserAttackHookManager(fireLaserState);
                AttackHookMask activatedHooks = attackHookManager.RunHooks();
                if (shouldSkipOrig(activatedHooks))
                    return false;

                return true;
            }
        }

        static void Evis_FixedUpdate(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            int targetHurtBoxLocalIndex = -1;
            ILLabel invalidTargetLabel = null;
            if (!c.TryFindNext(out ILCursor[] foundCursors,
                               x => x.MatchCallOrCallvirt<EntityStates.Merc.Evis>(nameof(EntityStates.Merc.Evis.SearchForTarget)),
                               x => x.MatchStloc(out targetHurtBoxLocalIndex),
                               x => x.MatchLdloc(targetHurtBoxLocalIndex),
                               x => x.MatchBrfalse(out invalidTargetLabel)))
            {
                Log.Error("Failed to find patch location");
                return;
            }

            c = foundCursors[3];
            c.Index++;

            ILLabel skipAttackLabel = c.DefineLabel();

            c.Emit(OpCodes.Ldarg_0);
            c.Emit(OpCodes.Ldloc, targetHurtBoxLocalIndex);
            c.EmitDelegate(runAttackHooks);
            c.Emit(OpCodes.Brfalse, skipAttackLabel);

            c.Goto(invalidTargetLabel.Target);
            c.Index--;
            c.MarkLabel(skipAttackLabel);

            static bool runAttackHooks(EntityStates.Merc.Evis evis, HurtBox target)
            {
                AttackHookManager attackHookManager = new EvisAttackHookManager(evis, target);
                AttackHookMask activatedHooks = attackHookManager.RunHooks();
                if (shouldSkipOrig(activatedHooks))
                    return false;

                return true;
            }
        }
    }
}
