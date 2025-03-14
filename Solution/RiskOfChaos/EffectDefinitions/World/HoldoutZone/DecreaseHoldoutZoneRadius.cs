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
    [ChaosTimedEffect("decrease_holdout_zone_radius", TimedEffectType.UntilStageEnd, ConfigName = "Decrease Teleporter Zone Radius")]
    public sealed class DecreaseHoldoutZoneRadius : NetworkBehaviour
    {
        [EffectConfig]
        static readonly ConfigHolder<float> _radiusDecrease =
            ConfigFactory<float>.CreateConfig("Radius Decrease", 0.5f)
                                .Description("Percentage decrease of teleporter radius")
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
            return new EffectNameFormatter_GenericFloat(_radiusDecrease) { ValueFormat = "0.##%" };
        }

        ValueModificationController _holdoutZoneModificationController;

        void Start()
        {
            if (NetworkServer.active)
            {
                _holdoutZoneModificationController = Instantiate(RoCContent.NetworkedPrefabs.SimpleHoldoutZoneModificationProvider).GetComponent<ValueModificationController>();

                SimpleHoldoutZoneModificationProvider holdoutZoneModificationProvider = _holdoutZoneModificationController.GetComponent<SimpleHoldoutZoneModificationProvider>();
                holdoutZoneModificationProvider.RadiusMultiplierConfigBinding.BindToConfig(_radiusDecrease, v => 1f - v);

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
