using BepInEx.Configuration;
using RiskOfChaos.EffectHandling;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Data;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfOptions.OptionConfigs;
using RiskOfOptions.Options;
using RoR2;
using UnityEngine;

namespace RiskOfChaos.EffectDefinitions.Time
{
    [ChaosEffect("decrease_time_scale", ConfigName = "Decrease World Speed", EffectWeightReductionPercentagePerActivation = 20f)]
    [ChaosTimedEffect(TimedEffectType.UntilStageEnd)]
    public sealed class DecreaseTimeScale : GenericMultiplyTimeScaleEffect
    {
        [InitEffectInfo]
        static readonly ChaosEffectInfo _effectInfo;

        static ConfigEntry<float> _timeScaleDecreaseConfig;
        const float TIME_SCALE_DECREASE_DEFAULT_VALUE = 0.5f;

        static float timeScaleDecrease
        {
            get
            {
                if (_timeScaleDecreaseConfig == null)
                {
                    return TIME_SCALE_DECREASE_DEFAULT_VALUE;
                }
                else
                {
                    return Mathf.Clamp01(_timeScaleDecreaseConfig.Value);
                }
            }
        }

        [SystemInitializer(typeof(ChaosEffectCatalog))]
        static void Init()
        {
            _timeScaleDecreaseConfig = _effectInfo.BindConfig("World Speed Decrease", TIME_SCALE_DECREASE_DEFAULT_VALUE, null);

            addConfigOption(new StepSliderOption(_timeScaleDecreaseConfig, new StepSliderConfig
            {
                formatString = "-{0:P0}",
                min = 0f,
                max = 1f,
                increment = 0.01f
            }));
        }

        protected override float multiplier => 1f - timeScaleDecrease;

        [EffectNameFormatArgs]
        static object[] GetDisplayNameFormatArgs()
        {
            return new object[] { timeScaleDecrease };
        }
    }
}
