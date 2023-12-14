using RiskOfChaos.EffectDefinitions.World;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.Utilities.Extensions;
using RoR2;
using RoR2.Orbs;
using RoR2.Projectile;
using System.Reflection;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectDefinitions.Character
{
    [ChaosTimedEffect("attack_knockback", 90f, IsNetworked = true)]
    [IncompatibleEffects(typeof(DisableKnockback))]
    public sealed class AttackKnockback : TimedEffect
    {
        public override void OnStart()
        {
            On.RoR2.BulletAttack.Fire += BulletAttack_Fire;

            On.RoR2.Orbs.OrbManager.AddOrb += OrbManager_AddOrb;

            On.RoR2.Projectile.ProjectileManager.FireProjectile_FireProjectileInfo += ProjectileManager_FireProjectile_FireProjectileInfo;
        }

        public override void OnEnd()
        {
            On.RoR2.BulletAttack.Fire -= BulletAttack_Fire;

            On.RoR2.Orbs.OrbManager.AddOrb -= OrbManager_AddOrb;

            On.RoR2.Projectile.ProjectileManager.FireProjectile_FireProjectileInfo -= ProjectileManager_FireProjectile_FireProjectileInfo;
        }

        static void BulletAttack_Fire(On.RoR2.BulletAttack.orig_Fire orig, BulletAttack self)
        {
            orig(self);

            if (!self.owner)
                return;

            CharacterBody ownerBody = self.owner.GetComponent<CharacterBody>();
            if (!ownerBody)
                return;

            tryKnockbackBody(ownerBody, -self.aimVector, self.damage);
        }

        static void OrbManager_AddOrb(On.RoR2.Orbs.OrbManager.orig_AddOrb orig, OrbManager self, Orb orb)
        {
            orig(self, orb);

            if (orb is null || !orb.target)
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

            tryKnockbackBody(attacker, -(orb.target.transform.position - orb.origin).normalized, damage);
        }

        static void ProjectileManager_FireProjectile_FireProjectileInfo(On.RoR2.Projectile.ProjectileManager.orig_FireProjectile_FireProjectileInfo orig, ProjectileManager self, FireProjectileInfo fireProjectileInfo)
        {
            orig(self, fireProjectileInfo);

            if (!fireProjectileInfo.owner)
                return;

            CharacterBody ownerBody = fireProjectileInfo.owner.GetComponent<CharacterBody>();
            if (!ownerBody)
                return;

            Vector3 fireDirection = (fireProjectileInfo.rotation * Vector3.forward).normalized;

            tryKnockbackBody(ownerBody, -fireDirection, fireProjectileInfo.damage);
        }

        static void tryKnockbackBody(CharacterBody body, Vector3 knockbackDirection, float damage)
        {
            if (!NetworkServer.active && !body.hasEffectiveAuthority)
                return;

            float baseForce = body.isPlayerControlled ? 8.5f : 30f;

            float damageCoefficient = damage / body.damage;
            float damageForceMultiplier = Mathf.Pow(damageCoefficient, damageCoefficient > 1f ? 1f / 1.5f : 2f);

            Vector3 force = knockbackDirection * (baseForce * damageForceMultiplier);

            if (body.TryGetComponent(out IPhysMotor motor))
            {
                PhysForceInfo physForceInfo = PhysForceInfo.Create();
                physForceInfo.force = force;
                physForceInfo.disableAirControlUntilCollision = false;
                physForceInfo.ignoreGroundStick = true;
                physForceInfo.massIsOne = true;

                motor.ApplyForceImpulse(physForceInfo);
            }
            else if (body.TryGetComponent(out Rigidbody rigidbody))
            {
                rigidbody.AddForce(force, ForceMode.VelocityChange);
            }
        }
    }
}
