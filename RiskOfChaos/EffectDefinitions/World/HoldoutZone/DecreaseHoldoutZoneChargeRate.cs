using BepInEx.Configuration;
using RiskOfChaos.ConfigHandling;
using RiskOfChaos.EffectHandling;
using RiskOfChaos.EffectHandling.Controllers;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Data;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.EffectHandling.Formatting;
using RiskOfChaos.ModifierController.HoldoutZone;
using RiskOfOptions.OptionConfigs;
using System;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectDefinitions.World.HoldoutZone
{
    [ChaosTimedEffect("decrease_holdout_zone_charge_rate", TimedEffectType.UntilStageEnd, ConfigName = "Decrease Teleporter Charge Rate")]
    public sealed class DecreaseHoldoutZoneChargeRate : TimedEffect, IHoldoutZoneModificationProvider
    {
        [EffectConfig]
        static readonly ConfigHolder<float> _chargeRateDecrease =
            ConfigFactory<float>.CreateConfig("Rate Decrease", 0.25f)
                                .Description("Percentage decrease of teleporter charge rate")
                                .AcceptableValues(new AcceptableValueRange<float>(0f, 1f))
                                .OptionConfig(new StepSliderConfig
                                {
                                    formatString = "-{0:P0}",
                                    increment = 0.01f,
                                    min = 0f,
                                    max = 1f
                                })
                                .OnValueChanged(() =>
                                {
                                    if (!NetworkServer.active || !TimedChaosEffectHandler.Instance)
                                        return;

                                    TimedChaosEffectHandler.Instance.InvokeEventOnAllInstancesOfEffect<DecreaseHoldoutZoneChargeRate>(e => e.OnValueDirty);
                                })
                                .Build();

        public event Action OnValueDirty;

        [EffectCanActivate]
        static bool CanActivate()
        {
            return HoldoutZoneModificationManager.Instance;
        }

        [GetEffectNameFormatter]
        static EffectNameFormatter GetNameFormatter()
        {
            return new EffectNameFormatter_GenericFloat(_chargeRateDecrease.Value) { ValueFormat = "P0" };
        }

        public void ModifyValue(ref HoldoutZoneModificationInfo value)
        {
            value.ChargeRateMultiplier *= 1f - _chargeRateDecrease.Value;
        }

        public override void OnStart()
        {
            HoldoutZoneModificationManager.Instance.RegisterModificationProvider(this);
        }

        public override void OnEnd()
        {
            if (HoldoutZoneModificationManager.Instance)
            {
                HoldoutZoneModificationManager.Instance.UnregisterModificationProvider(this);
            }
        }
    }
}
