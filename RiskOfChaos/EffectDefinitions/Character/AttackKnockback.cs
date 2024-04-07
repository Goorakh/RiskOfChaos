using MonoMod.Cil;
using RiskOfChaos.EffectDefinitions.World;
using RiskOfChaos.EffectHandling;
using RiskOfChaos.EffectHandling.Controllers;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Data;
using RiskOfChaos.Utilities.Extensions;
using RoR2;
using RoR2.Orbs;
using System.Reflection;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectDefinitions.Character
{
    [ChaosTimedEffect("attack_knockback", 90f, IsNetworked = true)]
    [IncompatibleEffects(typeof(DisableKnockback))]
    [EffectConfigBackwardsCompatibility("Effect: Extreme Recoil")]
    public sealed class AttackKnockback : TimedEffect
    {
        [InitEffectInfo]
        public static new readonly TimedEffectInfo EffectInfo;

        static bool _appliedPatches;

        static void tryApplyPatches()
        {
            if (_appliedPatches)
                return;

            On.RoR2.BulletAttack.Fire += (orig, self) =>
            {
                orig(self);

                if (!TimedChaosEffectHandler.Instance || !TimedChaosEffectHandler.Instance.IsTimedEffectActive(EffectInfo))
                    return;

                if (!self.owner)
                    return;

                CharacterBody ownerBody = self.owner.GetComponent<CharacterBody>();
                if (!ownerBody)
                    return;

                tryKnockbackBody(ownerBody, -self.aimVector.normalized, self.damage);
            };

            On.RoR2.Orbs.OrbManager.AddOrb += (orig, self, orb) =>
            {
                orig(self, orb);

                if (!TimedChaosEffectHandler.Instance || !TimedChaosEffectHandler.Instance.IsTimedEffectActive(EffectInfo))
                    return;

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
            };

            On.RoR2.Projectile.ProjectileManager.FireProjectile_FireProjectileInfo += (orig, self, fireProjectileInfo) =>
            {
                orig(self, fireProjectileInfo);

                if (!TimedChaosEffectHandler.Instance || !TimedChaosEffectHandler.Instance.IsTimedEffectActive(EffectInfo))
                    return;

                if (!fireProjectileInfo.owner)
                    return;

                CharacterBody ownerBody = fireProjectileInfo.owner.GetComponent<CharacterBody>();
                if (!ownerBody)
                    return;

                Vector3 fireDirection = (fireProjectileInfo.rotation * Vector3.forward).normalized;

                tryKnockbackBody(ownerBody, -fireDirection, fireProjectileInfo.damage);
            };

            On.EntityStates.GolemMonster.FireLaser.OnEnter += (orig, self) =>
            {
                orig(self);

#pragma warning disable Publicizer001 // Accessing a member that was not originally public
                if (!self.isAuthority)
                    return;
#pragma warning restore Publicizer001 // Accessing a member that was not originally public

                if (!TimedChaosEffectHandler.Instance || !TimedChaosEffectHandler.Instance.IsTimedEffectActive(EffectInfo))
                    return;

#pragma warning disable Publicizer001 // Accessing a member that was not originally public
                CharacterBody body = self.characterBody;
#pragma warning restore Publicizer001 // Accessing a member that was not originally public

                tryKnockbackBody(body, -self.laserDirection.normalized, EntityStates.GolemMonster.FireLaser.damageCoefficient * body.damage);
            };

            _appliedPatches = true;
        }

        static void tryKnockbackBody(CharacterBody body, Vector3 knockbackDirection, float damage)
        {
            if (!NetworkServer.active && !body.hasEffectiveAuthority)
                return;

            float baseForce = body.isPlayerControlled ? 8.5f : 30f;

            float damageCoefficient = damage / body.damage;
            float damageForceMultiplier = Mathf.Pow(damageCoefficient, damageCoefficient > 1f ? 1f / 1.5f : 2f);
            float effectStackMultiplier = TimedChaosEffectHandler.Instance.GetEffectStackCount(EffectInfo);

            Vector3 force = knockbackDirection * (baseForce * damageForceMultiplier * effectStackMultiplier);

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

        public override void OnStart()
        {
            tryApplyPatches();
        }

        public override void OnEnd()
        {
        }
    }
}
