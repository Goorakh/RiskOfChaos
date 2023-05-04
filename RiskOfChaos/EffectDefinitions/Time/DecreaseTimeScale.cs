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

namespace RiskOfChaos.EffectDefinitions.Time
{
    [ChaosEffect("decrease_time_scale", ConfigName = "Decrease World Speed", EffectWeightReductionPercentagePerActivation = 20f)]
    [ChaosTimedEffect(TimedEffectType.UntilStageEnd)]
    public sealed class DecreaseTimeScale : GenericMultiplyTimeScaleEffect
    {
        [InitEffectInfo]
        static readonly ChaosEffectInfo _effectInfo;

        static ConfigEntry<float> _timeScaleDecreaseConfig;
        const float TIME_SCALE_DECREASE_DEFAULT_VALUE = 0.25f;

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

            _timeScaleDecreaseConfig.SettingChanged += (o, e) =>
            {
                if (!NetworkServer.active || !TimedChaosEffectHandler.Instance)
                    return;

                foreach (DecreaseTimeScale effectInstance in TimedChaosEffectHandler.Instance.GetActiveEffectInstancesOfType<DecreaseTimeScale>())
                {
                    effectInstance.OnValueDirty?.Invoke();
                }
            };

            addConfigOption(new StepSliderOption(_timeScaleDecreaseConfig, new StepSliderConfig
            {
                formatString = "-{0:P0}",
                min = 0f,
                max = 1f,
                increment = 0.01f
            }));
        }

        public override event Action OnValueDirty;

        protected override float multiplier => 1f - timeScaleDecrease;

        [EffectNameFormatArgs]
        static object[] GetDisplayNameFormatArgs()
        {
            return new object[] { timeScaleDecrease };
        }
    }
}
