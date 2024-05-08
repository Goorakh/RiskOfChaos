using BepInEx.Configuration;
using RiskOfChaos.ConfigHandling;
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
    [ChaosTimedEffect("decrease_interactable_costs", TimedEffectType.UntilStageEnd, DefaultSelectionWeight = 0.8f, ConfigName = "Decrease Chest Prices")]
    public sealed class DecreaseInteractableCosts : TimedEffect, ICostModificationProvider
    {
        [EffectConfig]
        static readonly ConfigHolder<float> _decreaseAmount =
            ConfigFactory<float>.CreateConfig("Decrease Amount", 0.25f)
                                .Description("The amount to decrease costs by")
                                .AcceptableValues(new AcceptableValueRange<float>(0f, 1f))
                                .OptionConfig(new StepSliderConfig
                                {
                                    formatString = "-{0:P0}",
                                    min = 0f,
                                    max = 1f,
                                    increment = 0.05f
                                })
                                .OnValueChanged(() =>
                                {
                                    if (!NetworkServer.active || !TimedChaosEffectHandler.Instance)
                                        return;

                                    TimedChaosEffectHandler.Instance.InvokeEventOnAllInstancesOfEffect<DecreaseInteractableCosts>(e => e.OnValueDirty);
                                })
                                .FormatsEffectName()
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
            return new EffectNameFormatter_GenericFloat(_decreaseAmount.Value) { ValueFormat = "P0" };
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
            value.CostMultiplier *= 1f - _decreaseAmount.Value;
        }
    }
}
