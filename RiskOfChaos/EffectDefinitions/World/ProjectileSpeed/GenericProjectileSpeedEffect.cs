using HarmonyLib;
using MonoMod.RuntimeDetour;
using Newtonsoft.Json.Linq;
using RiskOfChaos.EffectHandling.Controllers;
using RoR2.Orbs;
using RoR2.Projectile;
using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectDefinitions.World.ProjectileSpeed
{
    public abstract class GenericProjectileSpeedEffect : TimedEffect
    {
        static bool _hasAppliedPatches;
        static void tryApplyPatches()
        {
            if (_hasAppliedPatches)
                return;

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

            On.RoR2.Projectile.MissileController.Awake += (orig, self) =>
            {
                orig(self);

                tryMultiplyProjectileSpeed(ref self.maxVelocity, self);
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

            _hasAppliedPatches = true;
        }

        static bool tryGetTotalMultiplier(out float totalMultiplier)
        {
            totalMultiplier = 1f;

            if (!TimedChaosEffectHandler.Instance)
                return false;

            foreach (GenericProjectileSpeedEffect projectileSpeedEffect in TimedChaosEffectHandler.Instance.GetActiveEffectInstancesOfType<GenericProjectileSpeedEffect>())
            {
                totalMultiplier *= projectileSpeedEffect._serverSpeedMultiplier;
            }

            return Mathf.Abs(totalMultiplier - 1f) > float.Epsilon;
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

        protected abstract float speedMultiplier { get; }

        protected float _serverSpeedMultiplier;

        public override void OnPreStartServer()
        {
            base.OnPreStartServer();

            _serverSpeedMultiplier = speedMultiplier;
        }

        public override void Serialize(NetworkWriter writer)
        {
            base.Serialize(writer);

            writer.Write(_serverSpeedMultiplier);
        }

        public override void Deserialize(NetworkReader reader)
        {
            base.Deserialize(reader);

            _serverSpeedMultiplier = reader.ReadSingle();
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
