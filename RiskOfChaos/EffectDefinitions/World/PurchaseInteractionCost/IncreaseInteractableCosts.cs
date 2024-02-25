using RiskOfChaos.ConfigHandling;
using RiskOfChaos.ConfigHandling.AcceptableValues;
using RiskOfChaos.EffectHandling;
using RiskOfChaos.EffectHandling.Controllers;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Data;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.EffectHandling.Formatting;
using RiskOfChaos.ModifierController.Cost;
using RiskOfOptions.OptionConfigs;
using System;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectDefinitions.World.PurchaseInteractionCost
{
    [ChaosTimedEffect("increase_interactable_costs", TimedEffectType.UntilStageEnd, DefaultSelectionWeight = 0.8f, ConfigName = "Increase Chest Prices")]
    public sealed class IncreaseInteractableCosts : TimedEffect, ICostModificationProvider
    {
        const float INCREASE_AMOUNT_MIN_VALUE = 0.05f;

        [EffectConfig]
        static readonly ConfigHolder<float> _increaseAmount =
            ConfigFactory<float>.CreateConfig("Increase Amount", 0.25f)
                                .Description("The amount to increase costs by")
                                .AcceptableValues(new AcceptableValueMin<float>(INCREASE_AMOUNT_MIN_VALUE))
                                .OptionConfig(new StepSliderConfig
                                {
                                    formatString = "+{0:P0}",
                                    min = INCREASE_AMOUNT_MIN_VALUE,
                                    max = 2f,
                                    increment = 0.05f
                                })
                                .OnValueChanged(() =>
                                {
                                    if (!NetworkServer.active || !TimedChaosEffectHandler.Instance)
                                        return;

                                    TimedChaosEffectHandler.Instance.InvokeEventOnAllInstancesOfEffect<IncreaseInteractableCosts>(e => e.OnValueDirty);
                                })
                                .Build();

        [EffectCanActivate]
        static bool CanActivate()
        {
            return CostModificationManager.Instance;
        }

        public event Action OnValueDirty;

        [GetEffectNameFormatter]
        static EffectNameFormatter GetNameFormatter()
        {
            return new EffectNameFormatter_GenericFloat(_increaseAmount.Value) { ValueFormat = "P0" };
        }

        public override void OnStart()
        {
            CostModificationManager.Instance.RegisterModificationProvider(this);
        }

        public override void OnEnd()
        {
            if (CostModificationManager.Instance)
            {
                CostModificationManager.Instance.UnregisterModificationProvider(this);
            }
        }

        public void ModifyValue(ref CostModificationInfo value)
        {
            value.CostMultiplier *= 1f + _increaseAmount.Value;
        }
    }
}
