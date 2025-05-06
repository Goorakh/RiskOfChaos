using HarmonyLib;
using MonoMod.RuntimeDetour;
using Rewired.ComponentControls.Effects;
using RiskOfChaos.ModificationController.Projectile;
using RoR2;
using RoR2.Orbs;
using RoR2.Projectile;
using System.Reflection;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.Patches
{
    static class ProjectileSpeedHook
    {
        static float currentSpeedMultiplier
        {
            get
            {
                if (!ProjectileModificationManager.Instance)
                    return 1f;

                return ProjectileModificationManager.Instance.SpeedMultiplier;
            }
        }

        [SystemInitializer]
        static void Init()
        {
            On.RoR2.Projectile.ProjectileManager.InitializeProjectile += ProjectileManager_InitializeProjectile;

            On.RoR2.Projectile.ProjectileSimple.SetLifetime += ProjectileSimple_SetLifetime;

            MethodInfo orbDurationSetter = AccessTools.DeclaredPropertySetter(typeof(Orb), nameof(Orb.duration));
            if (orbDurationSetter != null)
            {
                new Hook(orbDurationSetter, Orb_set_duration);
            }
            else
            {
                Log.Warning("Failed to find Orb.set_duration MethodInfo");
            }

#pragma warning disable CS0618 // Type or member is obsolete
            MethodInfo projectileSimpleVelocitySetter = AccessTools.DeclaredPropertySetter(typeof(ProjectileSimple), nameof(ProjectileSimple.velocity));
#pragma warning restore CS0618 // Type or member is obsolete
            if (projectileSimpleVelocitySetter != null)
            {
                // Hook the deprecated velocity setter, just in case some old mod is still using it
                new Hook(projectileSimpleVelocitySetter, ProjectileSimple_set_velocity);
            }
            else
            {
                Log.Warning("Failed to find ProjectileSimple.set_velocity MethodInfo");
            }
        }

        static void ProjectileManager_InitializeProjectile(On.RoR2.Projectile.ProjectileManager.orig_InitializeProjectile orig, ProjectileController projectileController, FireProjectileInfo fireProjectileInfo)
        {
            orig(projectileController, fireProjectileInfo);

            float speedMultiplier = currentSpeedMultiplier;
            if (speedMultiplier == 1f)
                return;

            float durationMultiplier = speedMultiplier != 0f ? 1f / speedMultiplier : 1f;
            float durationMultiplierIncreaseOnly = Mathf.Max(durationMultiplier, 1f);

            ProjectileSimple projectileSimple = projectileController.GetComponent<ProjectileSimple>();
            BoomerangProjectile boomerangProjectile = projectileController.GetComponent<BoomerangProjectile>();
            CleaverProjectile cleaverProjectile = projectileController.GetComponent<CleaverProjectile>();
            DaggerController daggerController = projectileController.GetComponent<DaggerController>();
            MissileController missileController = projectileController.GetComponent<MissileController>();
            ProjectileCharacterController projectileCharacterController = projectileController.GetComponent<ProjectileCharacterController>();
            SoulSearchController soulSearchController = projectileController.GetComponent<SoulSearchController>();
            PalmBlastProjectileController palmBlastProjectileController = projectileController.GetComponent<PalmBlastProjectileController>();

            bool isMovingProjectile = (projectileSimple && projectileSimple.desiredForwardSpeed > 0f) ||
                                      (boomerangProjectile && boomerangProjectile.travelSpeed > 0f) ||
                                      (cleaverProjectile && cleaverProjectile.travelSpeed > 0f) ||
                                      (daggerController && daggerController.acceleration > 0f) ||
                                      (missileController && missileController.maxVelocity > 0f && missileController.acceleration > 0f) ||
                                      (projectileCharacterController && projectileCharacterController.velocity > 0f) ||
                                      (soulSearchController && soulSearchController.maxVelocity > 0f && soulSearchController.acceleration > 0f) ||
                                      (palmBlastProjectileController && palmBlastProjectileController.projectileSpeed > 0f);

            if (boomerangProjectile)
            {
                boomerangProjectile.travelSpeed *= speedMultiplier;

                boomerangProjectile.transitionDuration *= durationMultiplier;
                boomerangProjectile.maxFlyStopwatch *= durationMultiplier;
            }

            if (cleaverProjectile)
            {
                cleaverProjectile.travelSpeed *= speedMultiplier;

                cleaverProjectile.transitionDuration *= durationMultiplier;
            }

            if (daggerController)
            {
                daggerController.acceleration *= speedMultiplier;

                daggerController.delayTimer *= durationMultiplier;
                daggerController.giveupTimer *= durationMultiplierIncreaseOnly;
                daggerController.deathTimer *= durationMultiplierIncreaseOnly;
            }

            if (projectileController.TryGetComponent(out GummyCloneProjectile gummyCloneProjectile))
            {
                if (isMovingProjectile)
                {
                    gummyCloneProjectile.maxLifetime *= durationMultiplierIncreaseOnly;
                }
            }

            if (projectileController.TryGetComponent(out HookProjectileImpact hookProjectileImpact))
            {
                hookProjectileImpact.reelSpeed *= speedMultiplier;

                hookProjectileImpact.liveTimer *= durationMultiplier;
                hookProjectileImpact.reelDelayTime *= durationMultiplier;
            }

            if (missileController)
            {
                missileController.maxVelocity *= speedMultiplier;
                missileController.acceleration *= speedMultiplier;

                missileController.delayTimer *= durationMultiplier;
                missileController.giveupTimer *= durationMultiplierIncreaseOnly;

                if (isMovingProjectile)
                {
                    missileController.deathTimer *= durationMultiplierIncreaseOnly;
                }
            }

            if (projectileCharacterController)
            {
                projectileCharacterController.velocity *= speedMultiplier;

                if (isMovingProjectile)
                {
                    projectileCharacterController.lifetime *= durationMultiplierIncreaseOnly;
                }
            }

            if (projectileController.TryGetComponent(out ProjectileGrappleController projectileGrappleController))
            {
                projectileGrappleController.maxTravelDistance *= durationMultiplier;
            }

            if (projectileController.TryGetComponent(out ProjectileImpactExplosion projectileImpactExplosion))
            {
                if (isMovingProjectile)
                {
                    projectileImpactExplosion.lifetime *= durationMultiplierIncreaseOnly;
                    projectileImpactExplosion.lifetimeAfterImpact *= durationMultiplierIncreaseOnly;
                }
            }

            if (projectileController.TryGetComponent(out ProjectileOwnerOrbiter projectileOwnerOrbiter))
            {
                if (NetworkServer.active)
                {
                    projectileOwnerOrbiter.NetworkdegreesPerSecond *= speedMultiplier;
                }
            }

            if (projectileSimple)
            {
                projectileSimple.desiredForwardSpeed *= speedMultiplier;

                if (isMovingProjectile)
                {
                    projectileSimple.lifetime *= durationMultiplierIncreaseOnly;
                }
            }

            if (projectileController.TryGetComponent(out ProjectileSpawnMaster projectileSpawnMaster))
            {
                if (isMovingProjectile)
                {
                    projectileSpawnMaster.maxLifetime *= durationMultiplierIncreaseOnly;
                }
            }

            if (projectileController.TryGetComponent(out ProjectileSteerTowardTarget projectileSteerTowardTarget))
            {
                projectileSteerTowardTarget.rotationSpeed *= speedMultiplier;
            }

            if (soulSearchController)
            {
                soulSearchController.maxVelocity *= speedMultiplier;
                soulSearchController.acceleration *= speedMultiplier;

                soulSearchController.delayTimer *= durationMultiplier;

                if (isMovingProjectile)
                {
                    soulSearchController.deathTimer *= durationMultiplierIncreaseOnly;
                }
            }

            if (projectileController.TryGetComponent(out SoulSpiralProjectile soulSpiralProjectile))
            {
                soulSpiralProjectile.degreesPerSecond *= speedMultiplier;
            }

            if (projectileController.TryGetComponent(out VelocityRandomOnStart velocityRandomOnStart))
            {
                velocityRandomOnStart.minSpeed *= speedMultiplier;
                velocityRandomOnStart.maxSpeed *= speedMultiplier;

                velocityRandomOnStart.minAngularSpeed *= speedMultiplier;
                velocityRandomOnStart.maxAngularSpeed *= speedMultiplier;
            }

            if (projectileController.TryGetComponent(out RotateAroundAxis rotateAroundAxis))
            {
                rotateAroundAxis.slowRotationSpeed *= speedMultiplier;
                rotateAroundAxis.fastRotationSpeed *= speedMultiplier;
            }

            if (palmBlastProjectileController)
            {
                palmBlastProjectileController.projectileSpeed *= speedMultiplier;
                palmBlastProjectileController.finalProjectileSpeed *= speedMultiplier;
            }
        }

        static void ProjectileSimple_SetLifetime(On.RoR2.Projectile.ProjectileSimple.orig_SetLifetime orig, ProjectileSimple self, float newLifetime)
        {
            float speedMultiplier = currentSpeedMultiplier;

            float durationMultiplier = speedMultiplier != 0f ? 1f / speedMultiplier : 1f;

            newLifetime *= durationMultiplier;

            orig(self, newLifetime);
        }

        delegate void orig_Orb_set_duration(Orb self, float value);
        static void Orb_set_duration(orig_Orb_set_duration orig, Orb self, float value)
        {
            if (self.distanceToTarget > 0f)
            {
                float speedMultiplier = currentSpeedMultiplier;
                float durationMultiplier = speedMultiplier != 0f ? 1f / speedMultiplier : 1f;

                value *= durationMultiplier;
            }

            orig(self, value);
        }

        delegate void orig_ProjectileSimple_set_velocity(ProjectileSimple self, float value);
        static void ProjectileSimple_set_velocity(orig_ProjectileSimple_set_velocity orig, ProjectileSimple self, float value)
        {
            orig(self, value * currentSpeedMultiplier);
        }
    }
}
