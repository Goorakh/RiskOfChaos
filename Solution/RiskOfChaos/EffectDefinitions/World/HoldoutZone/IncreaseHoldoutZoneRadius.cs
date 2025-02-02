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
    [ChaosTimedEffect("increase_holdout_zone_radius", TimedEffectType.UntilStageEnd, ConfigName = "Increase Teleporter Zone Radius")]
    public sealed class IncreaseHoldoutZoneRadius : NetworkBehaviour
    {
        [EffectConfig]
        static readonly ConfigHolder<float> _radiusIncrease =
            ConfigFactory<float>.CreateConfig("Radius Increase", 0.5f)
                                .Description("Percentage increase of teleporter radius")
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
            return new EffectNameFormatter_GenericFloat(_radiusIncrease) { ValueFormat = "0.##%" };
        }

        ValueModificationController _holdoutZoneModificationController;

        void Start()
        {
            if (NetworkServer.active)
            {
                _holdoutZoneModificationController = Instantiate(RoCContent.NetworkedPrefabs.SimpleHoldoutZoneModificationProvider).GetComponent<ValueModificationController>();

                SimpleHoldoutZoneModificationProvider holdoutZoneModificationProvider = _holdoutZoneModificationController.GetComponent<SimpleHoldoutZoneModificationProvider>();
                holdoutZoneModificationProvider.RadiusMultiplierConfigBinding.BindToConfig(_radiusIncrease, v => 1f + v);

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
