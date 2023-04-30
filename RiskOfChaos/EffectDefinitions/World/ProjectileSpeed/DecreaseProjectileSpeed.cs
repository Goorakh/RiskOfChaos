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
    [ChaosEffect("decrease_projectile_speed", ConfigName = "Decrease Projectile Speed")]
    [ChaosTimedEffect(TimedEffectType.UntilStageEnd)]
    public sealed class DecreaseProjectileSpeed : GenericProjectileSpeedEffect
    {
        [InitEffectInfo]
        static readonly ChaosEffectInfo _effectInfo;

        static ConfigEntry<float> _projectileSpeedDecreaseConfig;
        const float PROJECTILE_SPEED_DECREASE_DEFAULT_VALUE = 0.5f;

        static float projectileSpeedDecrease
        {
            get
            {
                if (_projectileSpeedDecreaseConfig == null)
                {
                    return PROJECTILE_SPEED_DECREASE_DEFAULT_VALUE;
                }
                else
                {
                    return Mathf.Clamp01(_projectileSpeedDecreaseConfig.Value);
                }
            }
        }

        [SystemInitializer(typeof(ChaosEffectCatalog))]
        static void InitConfigs()
        {
            _projectileSpeedDecreaseConfig = _effectInfo.BindConfig("Projectile Speed Decrease", PROJECTILE_SPEED_DECREASE_DEFAULT_VALUE, null);

            addConfigOption(new StepSliderOption(_projectileSpeedDecreaseConfig, new StepSliderConfig
            {
                formatString = "-{0:P0}",
                min = 0f,
                max = 1f,
                increment = 0.01f
            }));
        }

        [EffectNameFormatArgs]
        static object[] GetDisplayNameFormatArgs()
        {
            return new object[]
            {
                projectileSpeedDecrease
            };
        }

        protected override float speedMultiplier => 1f - projectileSpeedDecrease;
    }
}
