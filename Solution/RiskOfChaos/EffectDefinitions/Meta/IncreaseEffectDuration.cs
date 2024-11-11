using RiskOfChaos.ConfigHandling;
using RiskOfChaos.ConfigHandling.AcceptableValues;
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
    [ChaosTimedEffect("increase_effect_duration", TimedEffectType.UntilStageEnd, ConfigName = "Increase Effect Duration", DefaultSelectionWeight = 0.7f, IgnoreDurationModifiers = true)]
    [RequiredComponents(typeof(EffectDurationMultiplierEffect))]
    public sealed class IncreaseEffectDuration : NetworkBehaviour
    {
        [EffectConfig]
        static readonly ConfigHolder<float> _durationMultiplier =
            ConfigFactory<float>.CreateConfig("Effect Duration Multiplier", 2f)
                                .AcceptableValues(new AcceptableValueMin<float>(1f))
                                .OptionConfig(new FloatFieldConfig { Min = 1f, FormatString = "{0}x" })
                                .Build();

        [GetEffectNameFormatter]
        static EffectNameFormatter GetNameFormatter()
        {
            return new EffectNameFormatter_GenericFloat(_durationMultiplier);
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
