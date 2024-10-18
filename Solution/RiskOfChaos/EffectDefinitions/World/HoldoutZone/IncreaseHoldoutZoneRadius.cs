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
                                .OptionConfig(new StepSliderConfig
                                {
                                    FormatString = "+{0:P0}",
                                    increment = 0.01f,
                                    min = 0f,
                                    max = 2f
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
            return new EffectNameFormatter_GenericFloat(_radiusIncrease.Value) { ValueFormat = "P0" };
        }

        ValueModificationController _holdoutZoneModificationController;
        SimpleHoldoutZoneModificationProvider _holdoutZoneModificationProvider;

        void Start()
        {
            if (NetworkServer.active)
            {
                _holdoutZoneModificationController = Instantiate(RoCContent.NetworkedPrefabs.SimpleHoldoutZoneModificationProvider).GetComponent<ValueModificationController>();

                _holdoutZoneModificationProvider = _holdoutZoneModificationController.GetComponent<SimpleHoldoutZoneModificationProvider>();
                refreshRadiusModification();

                NetworkServer.Spawn(_holdoutZoneModificationController.gameObject);

                _radiusIncrease.SettingChanged += onRadiusIncreaseChanged;
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

            _radiusIncrease.SettingChanged -= onRadiusIncreaseChanged;
        }

        void onRadiusIncreaseChanged(object sender, ConfigChangedArgs<float> e)
        {
            refreshRadiusModification();
        }

        [Server]
        void refreshRadiusModification()
        {
            if (_holdoutZoneModificationProvider)
            {
                _holdoutZoneModificationProvider.RadiusMultiplier = 1f + _radiusIncrease.Value;
            }
        }
    }
}
