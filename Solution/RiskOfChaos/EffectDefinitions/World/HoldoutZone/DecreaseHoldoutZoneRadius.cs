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
            return new EffectNameFormatter_GenericFloat(_radiusDecrease.Value) { ValueFormat = "P0" };
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

                _radiusDecrease.SettingChanged += onRadiusDecreaseChanged;
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

            _radiusDecrease.SettingChanged -= onRadiusDecreaseChanged;
        }

        void onRadiusDecreaseChanged(object sender, ConfigChangedArgs<float> e)
        {
            refreshRadiusModification();
        }

        [Server]
        void refreshRadiusModification()
        {
            if (_holdoutZoneModificationProvider)
            {
                _holdoutZoneModificationProvider.RadiusMultiplier = 1f - _radiusDecrease.Value;
            }
        }
    }
}
