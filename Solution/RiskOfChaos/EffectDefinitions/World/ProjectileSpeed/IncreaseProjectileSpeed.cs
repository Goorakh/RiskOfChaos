using RiskOfChaos.ConfigHandling;
using RiskOfChaos.ConfigHandling.AcceptableValues;
using RiskOfChaos.EffectHandling;
using RiskOfChaos.EffectHandling.Controllers;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Data;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.EffectHandling.Formatting;
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
                                .AcceptableValues(new AcceptableValueMin<float>(0f))
                                .OptionConfig(new StepSliderConfig
                                {
                                    FormatString = "+{0:P0}",
                                    min = 0f,
                                    max = 2f,
                                    increment = 0.01f
                                })
                                .OnValueChanged(() =>
                                {
                                    if (!NetworkServer.active || !TimedChaosEffectHandler.Instance)
                                        return;

                                    TimedChaosEffectHandler.Instance.InvokeEventOnAllInstancesOfEffect<IncreaseProjectileSpeed>(e => e.OnValueDirty);
                                })
                                .FormatsEffectName()
                                .Build();

        [GetEffectNameFormatter]
        static EffectNameFormatter GetNameFormatter()
        {
            return new EffectNameFormatter_GenericFloat(_projectileSpeedIncrease.Value) { ValueFormat = "P0" };
        }

        public override event Action OnValueDirty;

        protected override float speedMultiplier => 1f + _projectileSpeedIncrease.Value;
    }
}
