using BepInEx.Configuration;
using RiskOfChaos.EffectHandling;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Data;
using RiskOfOptions.OptionConfigs;
using RiskOfOptions.Options;
using RoR2;
using UnityEngine;

namespace RiskOfChaos.EffectDefinitions.Gravity
{
    [ChaosEffect("rotate_gravity", DefaultSelectionWeight = 0.8f, EffectWeightReductionPercentagePerActivation = 20f)]
    [ChaosTimedEffect(TimedEffectType.UntilStageEnd)]
    [EffectConfigBackwardsCompatibility("Effect: Random Gravity Direction (Lasts 1 stage)")]
    public sealed class RotateGravity : GenericGravityEffect
    {
        [InitEffectInfo]
        static readonly ChaosEffectInfo _effectInfo;

        static ConfigEntry<float> _maxDeviationConfig;
        const float MAX_DEVITATION_DEFAULT_VALUE = 30f;

        const float MAX_DEVITATION_MIN_VALUE = 0f;
        const float MAX_DEVITATION_MAX_VALUE = 90f;

        static float maxDeviation
        {
            get
            {
                if (_maxDeviationConfig == null)
                {
                    return MAX_DEVITATION_DEFAULT_VALUE;
                }
                else
                {
                    return Mathf.Clamp(_maxDeviationConfig.Value, MAX_DEVITATION_MIN_VALUE, MAX_DEVITATION_MAX_VALUE);
                }
            }
        }

        [SystemInitializer(typeof(ChaosEffectCatalog))]
        static void InitConfig()
        {
            _maxDeviationConfig = _effectInfo.BindConfig("Max Rotation Angle", MAX_DEVITATION_DEFAULT_VALUE, new ConfigDescription("The maximum amount of deviation (in degrees) that can be applied to the gravity direction"));

            addConfigOption(new StepSliderOption(_maxDeviationConfig, new StepSliderConfig
            {
                formatString = "{0:F1}",
                min = MAX_DEVITATION_MIN_VALUE,
                max = MAX_DEVITATION_MAX_VALUE,
                increment = 0.5f
            }));
        }

        Quaternion _gravityRotation;

        public override void OnPreStartServer()
        {
            base.OnPreStartServer();

            float maxDeviation = RotateGravity.maxDeviation;

            _gravityRotation = Quaternion.Euler(RNG.RangeFloat(-maxDeviation, maxDeviation),
                                                RNG.RangeFloat(-maxDeviation, maxDeviation),
                                                RNG.RangeFloat(-maxDeviation, maxDeviation));
        }

        public override void ModifyValue(ref Vector3 gravity)
        {
            gravity = _gravityRotation * gravity;
        }
    }
}
