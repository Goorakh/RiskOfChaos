using BepInEx.Configuration;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfOptions.OptionConfigs;
using RiskOfOptions.Options;
using RoR2;
using System.Reflection;

namespace RiskOfChaos.EffectHandling
{
    public class TimedEffectInfo
    {
        public readonly ChaosEffectIndex EffectIndex;
        public readonly TimedChaosEffectIndex TimedEffectIndex;

        readonly TimedEffectType _defaultTimedType;
        readonly ConfigEntry<TimedEffectType> _timedTypeConfig;
        public TimedEffectType TimedType
        {
            get
            {
                if (_timedTypeConfig == null)
                {
                    return _defaultTimedType;
                }
                else
                {
                    return _timedTypeConfig.Value;
                }
            }
        }

        readonly float _defaultDuration;
        readonly ConfigEntry<float> _durationConfig;
        public float DurationSeconds
        {
            get
            {
                if (_durationConfig == null)
                {
                    return _defaultDuration;
                }
                else
                {
                    return _durationConfig.Value;
                }
            }
        }

        public TimedEffectInfo(in ChaosEffectInfo effectInfo, TimedChaosEffectIndex timedEffectIndex)
        {
            EffectIndex = effectInfo.EffectIndex;
            TimedEffectIndex = timedEffectIndex;

            ChaosTimedEffectAttribute timedEffectAttribute = effectInfo.EffectType.GetCustomAttribute<ChaosTimedEffectAttribute>();
            if (timedEffectAttribute != null)
            {
                _defaultTimedType = timedEffectAttribute.TimedType;

                _defaultDuration = timedEffectAttribute.DurationSeconds;
                if (TimedType == TimedEffectType.FixedDuration)
                {
                    _durationConfig = effectInfo.BindConfig("Effect Duration", _defaultDuration, new ConfigDescription("How long the effect should last, in seconds"));
                }
            }
            else
            {
                Log.Error($"Timed effect {effectInfo} is missing {nameof(ChaosTimedEffectAttribute)}");
            }
        }

        public string ApplyTimedTypeSuffix(string effectName)
        {
            switch (TimedType)
            {
                case TimedEffectType.UntilNextEffect:
                    return Language.GetStringFormatted("TIMED_TYPE_UNTIL_NEXT_EFFECT_FORMAT", effectName);
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

        internal void AddRiskOfOptionsEntries()
        {
            if (_timedTypeConfig != null)
            {
                ChaosEffectCatalog.AddEffectConfigOption(new ChoiceOption(_timedTypeConfig));
            }

            if (_durationConfig != null)
            {
                ChaosEffectCatalog.AddEffectConfigOption(new StepSliderOption(_durationConfig, new StepSliderConfig
                {
                    formatString = "{0:F0}s",
                    min = 0f,
                    max = 120f,
                    increment = 5f,
                    checkIfDisabled = () => TimedType != TimedEffectType.FixedDuration
                }));
            }
        }
    }
}
