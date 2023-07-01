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
            On.RoR2.Projectile.ProjectileSimple.Awake += (orig, self) =>
            {
                tryMultiplyProjectileValues(ref self.desiredForwardSpeed, ref self.lifetime, self);

                if (self.oscillate)
                {
                    tryMultiplyProjectileSpeed(ref self.oscillateSpeed, self);
                }

                orig(self);
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

            On.RoR2.Projectile.BoomerangProjectile.Awake += (orig, self) =>
            {
                tryMultiplyProjectileSpeed(ref self.travelSpeed, self);
                orig(self);
            };

            On.RoR2.Projectile.DaggerController.Awake += (orig, self) =>
            {
                tryMultiplyProjectileSpeed(ref self.acceleration, self);
                orig(self);
            };

            On.RoR2.Projectile.MissileController.Awake += (orig, self) =>
            {
                tryMultiplyProjectileValues(ref self.maxVelocity, ref self.deathTimer, self);
                tryMultiplyProjectileTravelTime(ref self.giveupTimer, self);
                orig(self);
            };

            On.RoR2.Projectile.ProjectileCharacterController.Awake += (orig, self) =>
            {
                tryMultiplyProjectileValues(ref self.velocity, ref self.lifetime, self);
                orig(self);
            };

            MethodInfo orbDurationSetter = AccessTools.DeclaredPropertySetter(typeof(Orb), nameof(Orb.duration));
            if (orbDurationSetter != null)
            {
                new Hook(orbDurationSetter, (Action<Orb, float> orig, Orb self, float value) =>
                {
                    tryMultiplyProjectileTravelTime(ref value, self);

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
