using BepInEx.Configuration;
using HarmonyLib;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RiskOfChaos.EffectHandling;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Data;
using RiskOfChaos.GravityModifier;
using RiskOfOptions.OptionConfigs;
using RiskOfOptions.Options;
using RoR2;
using UnityEngine;

namespace RiskOfChaos.EffectDefinitions.Gravity
{
    [ChaosEffect("rotate_gravity", DefaultSelectionWeight = 0.8f, EffectWeightReductionPercentagePerActivation = 20f)]
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
            _maxDeviationConfig = Main.Instance.Config.Bind(new ConfigDefinition(_effectInfo.ConfigSectionName, "Max Rotation Angle"), MAX_DEVITATION_DEFAULT_VALUE, new ConfigDescription("The maximum amount of deviation (in degrees) that can be applied to the gravity direction"));

            addConfigOption(new StepSliderOption(_maxDeviationConfig, new StepSliderConfig
            {
                formatString = "{0:F1}",
                min = MAX_DEVITATION_MIN_VALUE,
                max = MAX_DEVITATION_MAX_VALUE,
                increment = 0.5f
            }));
        }

        public override void ModifyGravity(ref Vector3 gravity)
        {
            float maxDeviation = RotateGravity.maxDeviation;

            Quaternion gravityRotation = Quaternion.Euler(RNG.RangeFloat(-maxDeviation, maxDeviation),
                                                          RNG.RangeFloat(-maxDeviation, maxDeviation),
                                                          RNG.RangeFloat(-maxDeviation, maxDeviation));

            gravity = gravityRotation * gravity;
        }
    }
}
