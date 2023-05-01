using BepInEx.Configuration;
using RiskOfChaos.EffectHandling;
using RiskOfChaos.EffectHandling.Controllers;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Data;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfOptions.OptionConfigs;
using RiskOfOptions.Options;
using RoR2;
using System;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectDefinitions.World.ProjectileSpeed
{
    [ChaosEffect("increase_projectile_speed", ConfigName = "Increase Projectile Speed")]
    [ChaosTimedEffect(TimedEffectType.UntilStageEnd)]
    public sealed class IncreaseProjectileSpeed : GenericProjectileSpeedEffect
    {
        [InitEffectInfo]
        static readonly ChaosEffectInfo _effectInfo;

        static ConfigEntry<float> _projectileSpeedIncreaseConfig;
        const float PROJECTILE_SPEED_INCREASE_DEFAULT_VALUE = 0.5f;

        static float projectileSpeedIncrease
        {
            get
            {
                if (_projectileSpeedIncreaseConfig == null)
                {
                    return PROJECTILE_SPEED_INCREASE_DEFAULT_VALUE;
                }
                else
                {
                    return Mathf.Max(_projectileSpeedIncreaseConfig.Value, 0f);
                }
            }
        }

        [SystemInitializer(typeof(ChaosEffectCatalog))]
        static void InitConfigs()
        {
            _projectileSpeedIncreaseConfig = _effectInfo.BindConfig("Projectile Speed Increase", PROJECTILE_SPEED_INCREASE_DEFAULT_VALUE, null);

            _projectileSpeedIncreaseConfig.SettingChanged += (o, e) =>
            {
                if (!NetworkServer.active || !TimedChaosEffectHandler.Instance)
                    return;

                foreach (IncreaseProjectileSpeed effectInstance in TimedChaosEffectHandler.Instance.GetActiveEffectInstancesOfType<IncreaseProjectileSpeed>())
                {
                    effectInstance.OnValueDirty?.Invoke();
                }
            };

            addConfigOption(new StepSliderOption(_projectileSpeedIncreaseConfig, new StepSliderConfig
            {
                formatString = "+{0:P0}",
                min = 0f,
                max = 2f,
                increment = 0.01f
            }));
        }

        [EffectNameFormatArgs]
        static object[] GetDisplayNameFormatArgs()
        {
            return new object[]
            {
                projectileSpeedIncrease
            };
        }

        public override event Action OnValueDirty;

        protected override float speedMultiplier => 1f + projectileSpeedIncrease;
    }
}
