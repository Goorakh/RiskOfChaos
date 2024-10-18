using BepInEx.Configuration;
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
                                    FormatString = "-{0:P0}",
                                    increment = 0.01f,
                                    min = 0f,
                                    max = 1f
                                })
                                .FormatsEffectName()
                                .Build();

        [EffectCanActivate]
        static bool CanActivate()
        {
            return RoCContent.NetworkedPrefabs.SimpleHoldoutZoneModificationProvider;
        }

        [GetEffectNameFormatter]
        static EffectNameFormatter GetNameFormatter()
        {
            return new EffectNameFormatter_GenericFloat(_chargeRateDecrease.Value) { ValueFormat = "P0" };
        }

        ValueModificationController _holdoutZoneModificationController;
        SimpleHoldoutZoneModificationProvider _holdoutZoneModificationProvider;

        void Start()
        {
            if (NetworkServer.active)
            {
                _holdoutZoneModificationController = Instantiate(RoCContent.NetworkedPrefabs.SimpleHoldoutZoneModificationProvider).GetComponent<ValueModificationController>();

                _holdoutZoneModificationProvider = _holdoutZoneModificationController.GetComponent<SimpleHoldoutZoneModificationProvider>();
                refreshChargeRateModification();

                NetworkServer.Spawn(_holdoutZoneModificationController.gameObject);

                _chargeRateDecrease.SettingChanged += onChargeRateDecreaseChanged;
            }
        }

        void OnDestroy()
        {
            if (_holdoutZoneModificationController)
            {
                _holdoutZoneModificationController.Retire();
                _holdoutZoneModificationController = null;
                _holdoutZoneModificationProvider = null;
            }

            _chargeRateDecrease.SettingChanged -= onChargeRateDecreaseChanged;
        }

        void onChargeRateDecreaseChanged(object sender, ConfigChangedArgs<float> e)
        {
            refreshChargeRateModification();
        }

        [Server]
        void refreshChargeRateModification()
        {
            if (_holdoutZoneModificationProvider)
            {
                _holdoutZoneModificationProvider.ChargeRateMultiplier = 1f - _chargeRateDecrease.Value;
            }
        }
    }
}
