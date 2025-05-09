﻿using BepInEx.Configuration;
using RiskOfChaos.ConfigHandling;
using RiskOfChaos.Content;
using RiskOfChaos.EffectHandling;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Data;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.EffectHandling.Formatting;
using RiskOfChaos.ModificationController;
using RiskOfChaos.ModificationController.HoldoutZone;
using RiskOfOptions.OptionConfigs;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectDefinitions.World.HoldoutZone
{
    [ChaosTimedEffect("decrease_holdout_zone_charge_rate", TimedEffectType.UntilStageEnd, ConfigName = "Decrease Teleporter Charge Rate")]
    public sealed class DecreaseHoldoutZoneChargeRate : NetworkBehaviour
    {
        [EffectConfig]
        static readonly ConfigHolder<float> _chargeRateDecrease =
            ConfigFactory<float>.CreateConfig("Rate Decrease", 0.25f)
                                .Description("Percentage decrease of teleporter charge rate")
                                .AcceptableValues(new AcceptableValueRange<float>(0f, 1f))
                                .OptionConfig(new StepSliderConfig
                                {
                                    FormatString = "-{0:0.##%}",
                                    increment = 0.01f,
                                    min = 0f,
                                    max = 1f
                                })
                                .Build();

        [EffectCanActivate]
        static bool CanActivate()
        {
            return RoCContent.NetworkedPrefabs.SimpleHoldoutZoneModificationProvider;
        }

        [GetEffectNameFormatter]
        static EffectNameFormatter GetNameFormatter()
        {
            return new EffectNameFormatter_GenericFloat(_chargeRateDecrease) { ValueFormat = "0.##%" };
        }

        ValueModificationController _holdoutZoneModificationController;

        void Start()
        {
            if (NetworkServer.active)
            {
                _holdoutZoneModificationController = Instantiate(RoCContent.NetworkedPrefabs.SimpleHoldoutZoneModificationProvider).GetComponent<ValueModificationController>();

                SimpleHoldoutZoneModificationProvider holdoutZoneModificationProvider = _holdoutZoneModificationController.GetComponent<SimpleHoldoutZoneModificationProvider>();
                holdoutZoneModificationProvider.ChargeRateMultiplierConfigBinding.BindToConfig(_chargeRateDecrease, v => 1f - v);

                NetworkServer.Spawn(_holdoutZoneModificationController.gameObject);
            }
        }

        void OnDestroy()
        {
            if (_holdoutZoneModificationController)
            {
                _holdoutZoneModificationController.Retire();
                _holdoutZoneModificationController = null;
            }
        }
    }
}
