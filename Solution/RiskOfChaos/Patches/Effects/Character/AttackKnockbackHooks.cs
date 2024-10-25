using RiskOfChaos.EffectDefinitions.Character;
using RiskOfChaos.EffectHandling.Controllers;
using RiskOfChaos.Utilities.Extensions;
using RoR2;
using RoR2.Orbs;
using RoR2.Projectile;
using System.Reflection;
using UnityEngine;

namespace RiskOfChaos.Patches.Effects.Character
{
    static class AttackKnockbackHooks
    {
        [SystemInitializer]
        static void Init()
        {
            On.RoR2.BulletAttack.Fire += BulletAttack_Fire;

            On.RoR2.Orbs.OrbManager.AddOrb += OrbManager_AddOrb;

            On.RoR2.Projectile.ProjectileManager.FireProjectile_FireProjectileInfo += ProjectileManager_FireProjectile_FireProjectileInfo;

            On.EntityStates.GolemMonster.FireLaser.OnEnter += FireLaser_OnEnter;
        }

        static void BulletAttack_Fire(On.RoR2.BulletAttack.orig_Fire orig, BulletAttack self)
        {
            orig(self);

            if (!ChaosEffectTracker.Instance || !ChaosEffectTracker.Instance.IsTimedEffectActive(AttackKnockback.EffectInfo))
                return;

            if (!self.owner || self.procChainMask.mask != 0)
                return;

            CharacterBody ownerBody = self.owner.GetComponent<CharacterBody>();
            if (!ownerBody)
                return;

            AttackKnockback.TryKnockbackBody(ownerBody, -self.aimVector, self.damage);
        }

        static void OrbManager_AddOrb(On.RoR2.Orbs.OrbManager.orig_AddOrb orig, OrbManager self, Orb orb)
        {
            orig(self, orb);

            if (!ChaosEffectTracker.Instance || !ChaosEffectTracker.Instance.IsTimedEffectActive(AttackKnockback.EffectInfo))
                return;

            if (orb is null || !orb.target)
                return;

            Vector3 orbDirection = orb.target.transform.position - orb.origin;
            if (orbDirection.sqrMagnitude == 0f)
                return;

            if (orb.TryGetProcChainMask(out ProcChainMask orbProcChainMask) && orbProcChainMask.mask != 0)
                return;

            CharacterBody attacker = orb.GetAttacker();
            if (!attacker)
                return;

            float damage;
            switch (orb)
            {
                case LunarDetonatorOrb lunarDetonatorOrb:
                    damage = lunarDetonatorOrb.baseDamage;
                    break;
                default:
                    FieldInfo damageValueField = orb.GetType().GetField("damageValue", BindingFlags.Public | BindingFlags.Instance);
                    if (damageValueField is null || damageValueField.FieldType != typeof(float))
                        return;

                    damage = (float)damageValueField.GetValue(orb);
                    break;
            }

            AttackKnockback.TryKnockbackBody(attacker, -orbDirection, damage);
        }

        static void ProjectileManager_FireProjectile_FireProjectileInfo(On.RoR2.Projectile.ProjectileManager.orig_FireProjectile_FireProjectileInfo orig, ProjectileManager self, FireProjectileInfo fireProjectileInfo)
        {
            orig(self, fireProjectileInfo);

            if (!ChaosEffectTracker.Instance || !ChaosEffectTracker.Instance.IsTimedEffectActive(AttackKnockback.EffectInfo))
                return;

            if (!fireProjectileInfo.owner || fireProjectileInfo.procChainMask.mask != 0)
                return;

            CharacterBody ownerBody = fireProjectileInfo.owner.GetComponent<CharacterBody>();
            if (!ownerBody)
                return;

            AttackKnockback.TryKnockbackBody(ownerBody, -(fireProjectileInfo.rotation * Vector3.forward), fireProjectileInfo.damage);
        }

        static void FireLaser_OnEnter(On.EntityStates.GolemMonster.FireLaser.orig_OnEnter orig, EntityStates.GolemMonster.FireLaser self)
        {
            orig(self);

            if (!self.isAuthority)
                return;

            if (!ChaosEffectTracker.Instance || !ChaosEffectTracker.Instance.IsTimedEffectActive(AttackKnockback.EffectInfo))
                return;

            CharacterBody body = self.characterBody;

            AttackKnockback.TryKnockbackBody(body, -self.laserDirection, EntityStates.GolemMonster.FireLaser.damageCoefficient * body.damage);
        }
    }
}
