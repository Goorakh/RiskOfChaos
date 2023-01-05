using BepInEx.Configuration;
using RiskOfChaos.EffectHandling;
using RiskOfOptions.OptionConfigs;
using RiskOfOptions.Options;
using RoR2;
using UnityEngine;

namespace RiskOfChaos.EffectDefinitions.World.PurchaseInteractionCost
{
    [ChaosEffect(EFFECT_ID, DefaultSelectionWeight = 0.8f, ConfigName = "Decrease Chest Prices")]
    public sealed class DecreaseInteractableCosts : GenericMultiplyPurchaseInteractionCostsEffect
    {
        const string EFFECT_ID = "decrease_interactable_costs";

        static ConfigEntry<float> _decreaseAmount;
        const float DECREASE_AMOUNT_DEFAULT_VALUE = 0.25f;

        static float decreaseAmount
        {
            get
            {
                if (_decreaseAmount == null)
                    return DECREASE_AMOUNT_DEFAULT_VALUE;

                return Mathf.Clamp01(_decreaseAmount.Value);
            }
        }

        protected override float multiplier => 1f - decreaseAmount;

        [SystemInitializer(typeof(ChaosEffectCatalog))]
        static void Init()
        {
            string sectionName = ChaosEffectCatalog.GetConfigSectionName(EFFECT_ID);

            _decreaseAmount = Main.Instance.Config.Bind(new ConfigDefinition(sectionName, "Decrease Amount"), DECREASE_AMOUNT_DEFAULT_VALUE, new ConfigDescription("The amount to decrease costs by"));
            ChaosEffectCatalog.AddEffectConfigOption(new StepSliderOption(_decreaseAmount, new StepSliderConfig
            {
                formatString = "-{0:P0}",
                min = 0f,
                max = 1f,
                increment = 0.05f
            }));
        }

        [EffectNameFormatArgs]
        static object[] GetDisplayNameFormatArgs()
        {
            return new object[] { decreaseAmount };
        }
    }
}
