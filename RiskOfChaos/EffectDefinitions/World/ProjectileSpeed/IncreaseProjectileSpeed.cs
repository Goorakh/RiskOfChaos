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
    [ChaosTimedEffect("increase_projectile_speed", TimedEffectType.UntilStageEnd, ConfigName = "Increase Projectile Speed")]
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

                                    TimedChaosEffectHandler.Instance.InvokeEventOnAllInstancesOfEffect<IncreaseProjectileSpeed>(e => e.OnValueDirty);
                                })
                                .Build();

        [EffectNameFormatArgs]
        static string[] GetDisplayNameFormatArgs()
        {
            return new string[] { _projectileSpeedIncrease.Value.ToString("P0") };
        }

        public override event Action OnValueDirty;

        protected override float speedMultiplier => 1f + _projectileSpeedIncrease.Value;
    }
}
