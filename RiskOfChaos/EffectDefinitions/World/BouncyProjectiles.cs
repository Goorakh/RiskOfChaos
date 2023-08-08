using RiskOfChaos.ConfigHandling;
using RiskOfChaos.EffectHandling;
using RiskOfChaos.EffectHandling.Controllers;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Data;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.ModifierController.Projectile;
using RiskOfOptions.OptionConfigs;
using System;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectDefinitions.World
{
    [ChaosEffect("bouncy_projectiles")]
    [ChaosTimedEffect(TimedEffectType.UntilStageEnd)]
    public sealed class BouncyProjectiles : TimedEffect, IProjectileModificationProvider
    {
        [EffectConfig]
        static readonly ConfigHolder<int> _maxBulletBounceCount =
            ConfigFactory<int>.CreateConfig("Max Bullet Bounce Count", 20)
                              .Description("The maximum amount of times bullets can be bounced")
                              .OptionConfig(new IntSliderConfig
                              {
                                  min = 1,
                                  max = 40
                              })
                              .OnValueChanged(bounceCountConfigChanged)
                              .ValueConstrictor(CommonValueConstrictors.GreaterThanOrEqualTo(1))
                              .Build();

        [EffectConfig]
        static readonly ConfigHolder<int> _maxProjectileBounceCount =
            ConfigFactory<int>.CreateConfig("Max Projectile Bounce Count", 2)
                              .Description("The maximum amount of times projectiles can be bounced")
                              .OptionConfig(new IntSliderConfig
                              {
                                  min = 1,
                                  max = 10
                              })
                              .OnValueChanged(bounceCountConfigChanged)
                              .ValueConstrictor(CommonValueConstrictors.GreaterThanOrEqualTo(1))
                              .Build();

        static void bounceCountConfigChanged()
        {
            if (!NetworkServer.active || !TimedChaosEffectHandler.Instance)
                return;

            foreach (BouncyProjectiles effectInstance in TimedChaosEffectHandler.Instance.GetActiveEffectInstancesOfType<BouncyProjectiles>())
            {
                effectInstance.OnValueDirty?.Invoke();
            }
        }

        [EffectCanActivate]
        static bool CanActivate()
        {
            return ProjectileModificationManager.Instance;
        }

        public override void OnStart()
        {
            ProjectileModificationManager.Instance.RegisterModificationProvider(this);
        }

        public event Action OnValueDirty;

        public void ModifyValue(ref ProjectileModificationData value)
        {
            value.BulletBounceCount += (uint)_maxBulletBounceCount.Value;
            value.ProjectileBounceCount += (uint)_maxProjectileBounceCount.Value;
            value.OrbBounceCount += (uint)_maxProjectileBounceCount.Value;
        }

        public override void OnEnd()
        {
            if (ProjectileModificationManager.Instance)
            {
                ProjectileModificationManager.Instance.UnregisterModificationProvider(this);
            }
        }
    }
}
