using HarmonyLib;
using MonoMod.RuntimeDetour;
using RiskOfChaos.ModifierController.Projectile;
using RoR2;
using RoR2.Orbs;
using RoR2.Projectile;
using System;
using System.Reflection;
using UnityEngine;

namespace RiskOfChaos.Patches
{
    static class ProjectileSpeedHook
    {
        [SystemInitializer]
        static void Init()
        {
            On.RoR2.Projectile.ProjectileManager.InitializeProjectile += (orig, projectileController, fireProjectileInfo) =>
            {
                orig(projectileController, fireProjectileInfo);

                if (projectileController.TryGetComponent(out ProjectileSimple projectileSimple))
                {
                    tryMultiplyProjectileValues(ref projectileSimple.desiredForwardSpeed, ref projectileSimple.lifetime, projectileSimple);
                }

                if (projectileController.TryGetComponent(out BoomerangProjectile boomerangProjectile))
                {
                    tryMultiplyProjectileSpeed(ref boomerangProjectile.travelSpeed, boomerangProjectile);
                }

                if (projectileController.TryGetComponent(out DaggerController daggerController))
                {
                    tryMultiplyProjectileSpeed(ref daggerController.acceleration, daggerController);
                }

                if (projectileController.TryGetComponent(out MissileController missileController))
                {
                    tryMultiplyProjectileValues(ref missileController.maxVelocity, ref missileController.deathTimer, missileController);
                    tryMultiplyProjectileTravelTime(ref missileController.giveupTimer, missileController);
                }

                if (projectileController.TryGetComponent(out ProjectileCharacterController projectileCharacterController))
                {
                    tryMultiplyProjectileValues(ref projectileCharacterController.velocity, ref projectileCharacterController.lifetime, projectileCharacterController);
                }

                if (projectileController.TryGetComponent(out ProjectileSteerTowardTarget projectileSteerTowardTarget))
                {
                    tryMultiplyProjectileSpeed(ref projectileSteerTowardTarget.rotationSpeed, projectileSteerTowardTarget);
                }
            };

            On.RoR2.VelocityRandomOnStart.Start += (orig, self) =>
            {
                if (self.GetComponent<ProjectileController>())
                {
                    tryMultiplyProjectileSpeed(ref self.minSpeed, self);
                    tryMultiplyProjectileSpeed(ref self.maxSpeed, self);

                    tryMultiplyProjectileSpeed(ref self.minAngularSpeed, self);
                    tryMultiplyProjectileSpeed(ref self.maxAngularSpeed, self);
                }

                orig(self);
            };

            On.RoR2.Projectile.ProjectileSimple.SetLifetime += (orig, self, newLifetime) =>
            {
                tryMultiplyProjectileTravelTime(ref newLifetime, self);

                orig(self, newLifetime);
            };

#pragma warning disable CS0618 // Type or member is obsolete
            MethodInfo projectileSimpleVelocitySetter = AccessTools.DeclaredPropertySetter(typeof(ProjectileSimple), nameof(ProjectileSimple.velocity));
#pragma warning restore CS0618 // Type or member is obsolete
            if (projectileSimpleVelocitySetter != null)
            {
                // Hook the deprecated velocity setter, just in case some old mod is still using it
                new Hook(projectileSimpleVelocitySetter, (Action<ProjectileSimple, float> orig, ProjectileSimple self, float value) =>
                {
                    tryMultiplyProjectileSpeed(ref value, self);
                    orig(self, value);
                });
            }

            MethodInfo orbDurationSetter = AccessTools.DeclaredPropertySetter(typeof(Orb), nameof(Orb.duration));
            if (orbDurationSetter != null)
            {
                new Hook(orbDurationSetter, (Action<Orb, float> orig, Orb self, float value) =>
                {
#pragma warning disable Publicizer001 // Accessing a member that was not originally public
                    float distanceToTarget = self.distanceToTarget;
#pragma warning restore Publicizer001 // Accessing a member that was not originally public

                    if (distanceToTarget > 0f)
                    {
                        tryMultiplyProjectileTravelTime(ref value, self);
                    }

                    orig(self, value);
                });
            }
            else
            {
                Log.Warning("Failed to find Orb.set_duration MethodInfo");
            }
        }

        static bool tryGetTotalMultiplier(out float totalMultiplier)
        {
            if (!ProjectileModificationManager.Instance || !ProjectileModificationManager.Instance.AnyModificationActive)
            {
                totalMultiplier = 1f;
                return false;
            }

            totalMultiplier = ProjectileModificationManager.Instance.NetworkedTotalProjectileSpeedMultiplier;

            const float MULTIPLIER_ACTIVE_MIN_DIFF = 0.01f;
            return Mathf.Abs(totalMultiplier - 1f) >= MULTIPLIER_ACTIVE_MIN_DIFF;
        }

        static void tryMultiplyProjectileValues(ref float speed, ref float travelTime, object debugIdentifier)
        {
            if (Mathf.Abs(speed) > float.Epsilon || Mathf.Abs(travelTime) > float.Epsilon)
            {
                if (tryGetTotalMultiplier(out float totalSpeedMultiplier))
                {
                    speed *= totalSpeedMultiplier;
                    travelTime /= totalSpeedMultiplier;

#if DEBUG
                    Log.Debug($"Modified projectile speed for {debugIdentifier ?? "null"}: {nameof(totalSpeedMultiplier)}={totalSpeedMultiplier}");
#endif
                }
            }
        }

        static void tryMultiplyProjectileSpeed(ref float speed, object debugIdentifier)
        {
            float travelTime = 0f;
            tryMultiplyProjectileValues(ref speed, ref travelTime, debugIdentifier);
        }

        static void tryMultiplyProjectileTravelTime(ref float travelTime, object debugIdentifier)
        {
            float speed = 0f;
            tryMultiplyProjectileValues(ref speed, ref travelTime, debugIdentifier);
        }
    }
}
