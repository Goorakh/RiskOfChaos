using RiskOfChaos.ConfigHandling;
using RiskOfChaos.EffectHandling;
using RiskOfChaos.EffectHandling.Controllers;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Data;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfOptions.OptionConfigs;
using System;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectDefinitions.World.ProjectileSpeed
{
    [ChaosEffect("increase_projectile_speed", ConfigName = "Increase Projectile Speed")]
    [ChaosTimedEffect(TimedEffectType.UntilStageEnd)]
    public sealed class IncreaseProjectileSpeed : GenericProjectileSpeedEffect
    {
        [EffectConfig]
        static readonly ConfigHolder<float> _projectileSpeedIncrease =
            ConfigFactory<float>.CreateConfig("Projectile Speed Increase", 0.5f)
                                .OptionConfig(new StepSliderConfig
                                {
                                    formatString = "+{0:P0}",
                                    min = 0f,
                                    max = 2f,
                                    increment = 0.01f
                                })
                                .ValueConstrictor(CommonValueConstrictors.GreaterThanOrEqualTo(0f))
                                .OnValueChanged(() =>
                                {
                                    if (!NetworkServer.active || !TimedChaosEffectHandler.Instance)
                                        return;

                                    foreach (IncreaseProjectileSpeed effectInstance in TimedChaosEffectHandler.Instance.GetActiveEffectInstancesOfType<IncreaseProjectileSpeed>())
                                    {
                                        effectInstance.OnValueDirty?.Invoke();
                                    }
                                })
                                .Build();

        [EffectNameFormatArgs]
        static object[] GetDisplayNameFormatArgs()
        {
            return new object[]
            {
                _projectileSpeedIncrease.Value
            };
        }

        public override event Action OnValueDirty;

        protected override float speedMultiplier => 1f + _projectileSpeedIncrease.Value;
    }
}
