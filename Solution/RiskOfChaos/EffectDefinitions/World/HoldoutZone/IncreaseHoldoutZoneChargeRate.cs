using RiskOfChaos.ConfigHandling;
using RiskOfChaos.ConfigHandling.AcceptableValues;
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
    [ChaosTimedEffect("increase_holdout_zone_charge_rate", TimedEffectType.UntilStageEnd, ConfigName = "Increase Teleporter Charge Rate")]
    public sealed class IncreaseHoldoutZoneChargeRate : TimedEffect, IHoldoutZoneModificationProvider
    {
        [EffectConfig]
        static readonly ConfigHolder<float> _chargeRateIncrease =
            ConfigFactory<float>.CreateConfig("Rate Increase", 0.5f)
                                .Description("Percentage increase of teleporter charge rate")
                                .AcceptableValues(new AcceptableValueMin<float>(0f))
                                .OptionConfig(new StepSliderConfig
                                {
                                    FormatString = "+{0:P0}",
                                    increment = 0.01f,
                                    min = 0f,
                                    max = 2f
                                })
                                .OnValueChanged(() =>
                                {
                                    if (!NetworkServer.active || !TimedChaosEffectHandler.Instance)
                                        return;

                                    TimedChaosEffectHandler.Instance.InvokeEventOnAllInstancesOfEffect<IncreaseHoldoutZoneChargeRate>(e => e.OnValueDirty);
                                })
                                .FormatsEffectName()
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
            return new EffectNameFormatter_GenericFloat(_chargeRateIncrease.Value) { ValueFormat = "P0" };
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

        public void ModifyValue(ref HoldoutZoneModificationInfo value)
        {
            value.ChargeRateMultiplier *= 1f + _chargeRateIncrease.Value;
        }
    }
}
