using BepInEx.Configuration;
using RiskOfChaos.ConfigHandling;
using RiskOfChaos.Content;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Data;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.EffectHandling.Formatting;
using RiskOfChaos.ModificationController;
using RiskOfChaos.ModificationController.TimeScale;
using RiskOfChaos.Utilities.Interpolation;
using RiskOfOptions.OptionConfigs;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectDefinitions.World.TimeScale
{
    [ChaosTimedEffect("decrease_time_scale", 120f, ConfigName = "Decrease World Speed")]
    public sealed class DecreaseTimeScale : MonoBehaviour
    {
        [EffectConfig]
        static readonly ConfigHolder<float> _timeScaleDecrease =
            ConfigFactory<float>.CreateConfig("World Speed Decrease", 0.35f)
                                .AcceptableValues(new AcceptableValueRange<float>(0f, 1f))
                                .OptionConfig(new StepSliderConfig
                                {
                                    FormatString = "-{0:P0}",
                                    min = 0f,
                                    max = 1f,
                                    increment = 0.01f
                                })
                                .FormatsEffectName()
                                .Build();

        [EffectCanActivate]
        static bool CanActivate()
        {
            return RoCContent.NetworkedPrefabs.GenericTimeScaleModificationProvider;
        }

        [GetEffectNameFormatter]
        static EffectNameFormatter GetNameFormatter()
        {
            return new EffectNameFormatter_GenericFloat(_timeScaleDecrease.Value) { ValueFormat = "P0" };
        }

        ValueModificationController _timeScaleModificationController;

        void Start()
        {
            if (NetworkServer.active)
            {
                _timeScaleModificationController = Instantiate(RoCContent.NetworkedPrefabs.GenericTimeScaleModificationProvider).GetComponent<ValueModificationController>();

                _timeScaleModificationController.SetInterpolationParameters(new InterpolationParameters(1f));

                GenericTimeScaleModificationProvider timeScaleModificationProvider = _timeScaleModificationController.GetComponent<GenericTimeScaleModificationProvider>();
                timeScaleModificationProvider.TimeScaleMultiplierConfigBinding.BindToConfig(_timeScaleDecrease, v => 1f - v);
                timeScaleModificationProvider.CompensatePlayerSpeed = true;

                NetworkServer.Spawn(_timeScaleModificationController.gameObject);
            }
        }

        void OnDestroy()
        {
            if (_timeScaleModificationController)
            {
                _timeScaleModificationController.Retire();
                _timeScaleModificationController = null;
            }
        }
    }
}
