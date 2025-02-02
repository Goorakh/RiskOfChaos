using RiskOfChaos.ConfigHandling;
using RiskOfChaos.ConfigHandling.AcceptableValues;
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
    [ChaosTimedEffect("increase_holdout_zone_charge_rate", TimedEffectType.UntilStageEnd, ConfigName = "Increase Teleporter Charge Rate")]
    public sealed class IncreaseHoldoutZoneChargeRate : NetworkBehaviour
    {
        [EffectConfig]
        static readonly ConfigHolder<float> _chargeRateIncrease =
            ConfigFactory<float>.CreateConfig("Rate Increase", 0.5f)
                                .Description("Percentage increase of teleporter charge rate")
                                .AcceptableValues(new AcceptableValueMin<float>(0f))
                                .OptionConfig(new FloatFieldConfig
                                {
                                    FormatString = "+{0:0.##%}",
                                    Min = 0f
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
            return new EffectNameFormatter_GenericFloat(_chargeRateIncrease) { ValueFormat = "0.##%" };
        }

        ValueModificationController _holdoutZoneModificationController;

        void Start()
        {
            if (NetworkServer.active)
            {
                _holdoutZoneModificationController = Instantiate(RoCContent.NetworkedPrefabs.SimpleHoldoutZoneModificationProvider).GetComponent<ValueModificationController>();

                SimpleHoldoutZoneModificationProvider holdoutZoneModificationProvider = _holdoutZoneModificationController.GetComponent<SimpleHoldoutZoneModificationProvider>();
                holdoutZoneModificationProvider.ChargeRateMultiplierConfigBinding.BindToConfig(_chargeRateIncrease, v => 1f + v);

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
