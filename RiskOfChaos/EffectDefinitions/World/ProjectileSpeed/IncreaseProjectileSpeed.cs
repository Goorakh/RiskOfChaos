using BepInEx.Configuration;
using RiskOfChaos.EffectHandling;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Data;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfOptions.OptionConfigs;
using RiskOfOptions.Options;
using RoR2;
using UnityEngine;

namespace RiskOfChaos.EffectDefinitions.World.ProjectileSpeed
{
    [ChaosEffect("increase_projectile_speed", ConfigName = "Increase Projectile Speed")]
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
            _projectileSpeedIncreaseConfig = Main.Instance.Config.Bind(new ConfigDefinition(_effectInfo.ConfigSectionName, "Projectile Speed Increase"), PROJECTILE_SPEED_INCREASE_DEFAULT_VALUE);

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

        public override TimedEffectType TimedType => TimedEffectType.UntilStageEnd;

        protected override float speedMultiplier => 1f + projectileSpeedIncrease;
    }
}
