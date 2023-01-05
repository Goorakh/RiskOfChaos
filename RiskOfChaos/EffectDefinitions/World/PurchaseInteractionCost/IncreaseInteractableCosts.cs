using BepInEx.Configuration;
using RiskOfChaos.EffectHandling;
using RiskOfOptions.OptionConfigs;
using RiskOfOptions.Options;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace RiskOfChaos.EffectDefinitions.World.PurchaseInteractionCost
{
    [ChaosEffect(EFFECT_ID, DefaultSelectionWeight = 0.8f, ConfigName = "Increase Chest Prices")]
    public sealed class IncreaseInteractableCosts : GenericMultiplyPurchaseInteractionCostsEffect
    {
        const string EFFECT_ID = "increase_interactable_costs";

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
            string sectionName = ChaosEffectCatalog.GetConfigSectionName(EFFECT_ID);

            _increaseAmount = Main.Instance.Config.Bind(new ConfigDefinition(sectionName, "Increase Amount"), INCREASE_AMOUNT_DEFAULT_VALUE, new ConfigDescription("The amount to increase costs by"));
            ChaosEffectCatalog.AddEffectConfigOption(new StepSliderOption(_increaseAmount, new StepSliderConfig
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
