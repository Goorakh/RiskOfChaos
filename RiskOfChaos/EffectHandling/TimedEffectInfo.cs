using RiskOfChaos.ConfigHandling;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfOptions.OptionConfigs;
using RoR2;
using System.Reflection;

namespace RiskOfChaos.EffectHandling
{
    public class TimedEffectInfo
    {
        public readonly ChaosEffectIndex EffectIndex;
        public readonly TimedChaosEffectIndex TimedEffectIndex;

        public readonly bool AllowDuplicates;

        readonly ConfigHolder<TimedEffectType> _timedType;
        public TimedEffectType TimedType => _timedType.Value;

        readonly ConfigHolder<float> _duration;
        public float DurationSeconds => _duration.Value;

        public TimedEffectInfo(in ChaosEffectInfo effectInfo, TimedChaosEffectIndex timedEffectIndex)
        {
            EffectIndex = effectInfo.EffectIndex;
            TimedEffectIndex = timedEffectIndex;

            ChaosTimedEffectAttribute timedEffectAttribute = effectInfo.EffectType.GetCustomAttribute<ChaosTimedEffectAttribute>();
            if (timedEffectAttribute != null)
            {
                _timedType = ConfigFactory<TimedEffectType>.CreateConfig("Duration Type", timedEffectAttribute.TimedType)
                                                           .Description($"What should determine how long this effect lasts.\n\n{nameof(TimedEffectType.UntilStageEnd)}: Lasts until you exit the stage.\n{nameof(TimedEffectType.FixedDuration)}: Lasts for a set number of seconds.\n{nameof(TimedEffectType.Permanent)}: Lasts until the end of the run.")
                                                           .OptionConfig(new ChoiceConfig())
                                                           .Build();
                _timedType.Bind(effectInfo);

                float defaultDuration = timedEffectAttribute.DurationSeconds;
                if (defaultDuration < 0f)
                    defaultDuration = 60f;

                _duration = ConfigFactory<float>.CreateConfig("Effect Duration", defaultDuration)
                                                .Description($"How long the effect should last, in seconds.\nOnly takes effect if the Duration Type is set to {nameof(TimedEffectType.FixedDuration)}")
                                                .OptionConfig(new StepSliderConfig
                                                {
                                                    formatString = "{0:F0}s",
                                                    min = 0f,
                                                    max = 120f,
                                                    increment = 5f,
                                                    checkIfDisabled = () => TimedType != TimedEffectType.FixedDuration
                                                })
                                                .ValueConstrictor(CommonValueConstrictors.GreaterThanOrEqualTo(0f))
                                                .Build();
                _duration.Bind(effectInfo);

                AllowDuplicates = timedEffectAttribute.AllowDuplicates;
            }
            else
            {
                Log.Error($"Timed effect {effectInfo} is missing {nameof(ChaosTimedEffectAttribute)}");
            }
        }

        public override string ToString()
        {
            return ChaosEffectCatalog.GetEffectInfo(EffectIndex).ToString();
        }

        public string ApplyTimedTypeSuffix(string effectName)
        {
            switch (TimedType)
            {
                case TimedEffectType.UntilStageEnd:
                    return Language.GetStringFormatted("TIMED_TYPE_UNTIL_STAGE_END_FORMAT", effectName);
                case TimedEffectType.FixedDuration:
                    return Language.GetStringFormatted("TIMED_TYPE_FIXED_DURATION_FORMAT", effectName, DurationSeconds);
                case TimedEffectType.Permanent:
                    return Language.GetStringFormatted("TIMED_TYPE_PERMANENT_FORMAT", effectName);
                default:
                    Log.Warning($"Timed type {TimedType} is not implemented");
                    return effectName;
            }
        }
    }
}
