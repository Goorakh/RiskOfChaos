using BepInEx.Configuration;
using RiskOfChaos.EffectHandling;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Data;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfOptions.OptionConfigs;
using RiskOfOptions.Options;
using RoR2;
using UnityEngine;

namespace RiskOfChaos.EffectDefinitions.World.PurchaseInteractionCost
{
    [ChaosEffect("increase_interactable_costs", DefaultSelectionWeight = 0.8f, ConfigName = "Increase Chest Prices")]
    public sealed class IncreaseInteractableCosts : GenericMultiplyPurchaseInteractionCostsEffect
    {
        [InitEffectInfo]
        static readonly ChaosEffectInfo _effectInfo;

        static ConfigEntry<float> _increaseAmount;
        const float INCREASE_AMOUNT_DEFAULT_VALUE = 0.25f;
        const float INCREASE_AMOUNT_MIN_VALUE = 0.05f;

        static float increaseAmount
        {
            get
            {
                if (_increaseAmount == null)
                    return INCREASE_AMOUNT_DEFAULT_VALUE;

                return Mathf.Max(_increaseAmount.Value, INCREASE_AMOUNT_MIN_VALUE);
            }
        }

        protected override float multiplier => 1f + increaseAmount;

        [SystemInitializer(typeof(ChaosEffectCatalog))]
        static void Init()
        {
            _increaseAmount = Main.Instance.Config.Bind(new ConfigDefinition(_effectInfo.ConfigSectionName, "Increase Amount"), INCREASE_AMOUNT_DEFAULT_VALUE, new ConfigDescription("The amount to increase costs by"));
            addConfigOption(new StepSliderOption(_increaseAmount, new StepSliderConfig
            {
                formatString = "+{0:P0}",
                min = INCREASE_AMOUNT_MIN_VALUE,
                max = 2f,
                increment = 0.05f
            }));
        }

        [EffectNameFormatArgs]
        static object[] GetDisplayNameFormatArgs()
        {
            return new object[] { increaseAmount };
        }
    }
}
