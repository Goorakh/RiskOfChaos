using BepInEx.Configuration;
using RiskOfChaos.EffectHandling;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Data;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfOptions.OptionConfigs;
using RiskOfOptions.Options;
using RoR2;
using UnityEngine;

namespace RiskOfChaos.EffectDefinitions.Gravity
{
    [ChaosEffect("decrease_gravity", ConfigName = "Decrease Gravity", EffectWeightReductionPercentagePerActivation = 25f, IsNetworked = true)]
    public sealed class DecreaseGravity : GenericMultiplyGravityEffect
    {
        [InitEffectInfo]
        static readonly ChaosEffectInfo _effectInfo;

        const float GRAVITY_DECREASE_DEFAULT_VALUE = 0.5f;

        static ConfigEntry<float> _gravityDecrease;
        static float gravityDecrease => Mathf.Clamp01(_gravityDecrease?.Value ?? GRAVITY_DECREASE_DEFAULT_VALUE);

        [SystemInitializer(typeof(ChaosEffectCatalog))]
        static void Init()
        {
            _gravityDecrease = Main.Instance.Config.Bind(new ConfigDefinition(_effectInfo.ConfigSectionName, "Decrease per Activation"), GRAVITY_DECREASE_DEFAULT_VALUE, new ConfigDescription("How much gravity should decrease per effect activation, 50% means the gravity is multiplied by 0.5, 100% means the gravity is reduced to 0, 0% means gravity doesn't change at all. etc."));
            addConfigOption(new StepSliderOption(_gravityDecrease, new StepSliderConfig
            {
                min = 0f,
                max = 1f,
                increment = 0.01f,
                formatString = "-{0:P0}"
            }));
        }

        protected override float multiplier => 1f - gravityDecrease;

        [EffectNameFormatArgs]
        static object[] GetEffectNameFormatArgs()
        {
            return new object[] { gravityDecrease };
        }
    }
}
