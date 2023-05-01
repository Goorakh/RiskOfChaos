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

namespace RiskOfChaos.EffectDefinitions.Gravity
{
    [ChaosEffect("increase_gravity", ConfigName = "Increase Gravity", EffectWeightReductionPercentagePerActivation = 25f)]
    [ChaosTimedEffect(TimedEffectType.UntilStageEnd)]
    public sealed class IncreaseGravity : GenericMultiplyGravityEffect
    {
        [InitEffectInfo]
        static readonly ChaosEffectInfo _effectInfo;

        const float GRAVITY_INCREASE_DEFAULT_VALUE = 0.5f;

        static ConfigEntry<float> _gravityIncrease;
        static float gravityIncrease => Mathf.Max(_gravityIncrease?.Value ?? GRAVITY_INCREASE_DEFAULT_VALUE, 0f);

        [SystemInitializer(typeof(ChaosEffectCatalog))]
        static void InitConfigs()
        {
            _gravityIncrease = _effectInfo.BindConfig("Increase per Activation", GRAVITY_INCREASE_DEFAULT_VALUE, new ConfigDescription("How much gravity should increase per effect activation, 50% means the gravity is multiplied by 1.5, 100% means the gravity is multiplied by 2, etc."));

            _gravityIncrease.SettingChanged += (o, e) =>
            {
                if (!NetworkServer.active || !TimedChaosEffectHandler.Instance)
                    return;

                foreach (IncreaseGravity effectInstance in TimedChaosEffectHandler.Instance.GetActiveEffectInstancesOfType<IncreaseGravity>())
                {
                    effectInstance.OnValueDirty?.Invoke();
                }
            };

            addConfigOption(new StepSliderOption(_gravityIncrease, new StepSliderConfig
            {
                min = 0f,
                max = 1f,
                increment = 0.01f,
                formatString = "+{0:P0}"
            }));
        }

        public override event Action OnValueDirty;

        protected override float multiplier => 1f + gravityIncrease;

        [EffectNameFormatArgs]
        static object[] GetEffectNameFormatArgs()
        {
            return new object[] { gravityIncrease };
        }
    }
}
