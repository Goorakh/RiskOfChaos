using BepInEx.Configuration;
using RiskOfChaos.ConfigHandling;
using RiskOfChaos.Content;
using RiskOfChaos.EffectHandling;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Data;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.EffectHandling.Formatting;
using RiskOfOptions.OptionConfigs;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectDefinitions.Meta
{
    [ChaosTimedEffect("decrease_effect_duration", TimedEffectType.UntilStageEnd, ConfigName = "Decrease Effect Duration", IgnoreDurationModifiers = true)]
    [RequiredComponents(typeof(EffectDurationMultiplierEffect))]
    public sealed class DecreaseEffectDuration : NetworkBehaviour
    {
        [EffectConfig]
        static readonly ConfigHolder<float> _durationMultiplier =
            ConfigFactory<float>.CreateConfig("Duration Multiplier", 0.5f)
                                .AcceptableValues(new AcceptableValueRange<float>(0f, 1f))
                                .OptionConfig(new StepSliderConfig
                                {
                                    FormatString = "{0}x",
                                    min = 0f,
                                    max = 1f,
                                    increment = 0.1f
                                })
                                .FormatsEffectName()
                                .Build();

        [GetEffectNameFormatter]
        static EffectNameFormatter GetNameFormatter()
        {
            return new EffectNameFormatter_GenericFloat(_durationMultiplier.Value);
        }

        EffectDurationMultiplierEffect _effectDurationMultiplierEffect;

        void Awake()
        {
            _effectDurationMultiplierEffect = GetComponent<EffectDurationMultiplierEffect>();
        }

        public override void OnStartServer()
        {
            base.OnStartServer();

            _effectDurationMultiplierEffect.DurationMultiplier = _durationMultiplier.Value;
            _durationMultiplier.SettingChanged += onDurationMultiplierChanged;
        }

        void OnDestroy()
        {
            _durationMultiplier.SettingChanged -= onDurationMultiplierChanged;
        }

        [Server]
        void onDurationMultiplierChanged(object sender, ConfigChangedArgs<float> e)
        {
            _effectDurationMultiplierEffect.DurationMultiplier = _durationMultiplier.Value;
        }
    }
}
