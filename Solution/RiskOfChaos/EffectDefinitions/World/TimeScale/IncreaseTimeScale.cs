using RiskOfChaos.ConfigHandling;
using RiskOfChaos.ConfigHandling.AcceptableValues;
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
    [ChaosTimedEffect("increase_time_scale", 120f, ConfigName = "Increase World Speed")]
    public sealed class IncreaseTimeScale : MonoBehaviour
    {
        [EffectConfig]
        static readonly ConfigHolder<float> _timeScaleIncrease =
            ConfigFactory<float>.CreateConfig("World Speed Increase", 0.35f)
                                .AcceptableValues(new AcceptableValueMin<float>(0f))
                                .OptionConfig(new StepSliderConfig
                                {
                                    FormatString = "+{0:P0}",
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
            return new EffectNameFormatter_GenericFloat(_timeScaleIncrease.Value) { ValueFormat = "P0" };
        }

        ValueModificationController _timeScaleModificationController;

        void Start()
        {
            if (NetworkServer.active)
            {
                _timeScaleModificationController = Instantiate(RoCContent.NetworkedPrefabs.GenericTimeScaleModificationProvider).GetComponent<ValueModificationController>();

                _timeScaleModificationController.SetInterpolationParameters(new InterpolationParameters(1f));

                GenericTimeScaleModificationProvider timeScaleModificationProvider = _timeScaleModificationController.GetComponent<GenericTimeScaleModificationProvider>();
                timeScaleModificationProvider.TimeScaleMultiplierConfigBinding.BindToConfig(_timeScaleIncrease, v => 1f + v);
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
