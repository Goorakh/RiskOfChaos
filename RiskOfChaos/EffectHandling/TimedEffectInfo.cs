using BepInEx.Configuration;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfOptions.OptionConfigs;
using RiskOfOptions.Options;
using RoR2;
using System;
using System.Reflection;

namespace RiskOfChaos.EffectHandling
{
    public class TimedEffectInfo
    {
        public readonly int EffectIndex;

        public readonly int TimedEffectIndex;

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

        public TimedEffectInfo(in ChaosEffectInfo effectInfo, int timedEffectIndex)
        {
            EffectIndex = effectInfo.EffectIndex;
            TimedEffectIndex = timedEffectIndex;

            ChaosTimedEffectAttribute timedEffectAttribute = effectInfo.EffectType.GetCustomAttribute<ChaosTimedEffectAttribute>();
            if (timedEffectAttribute != null)
            {
                _defaultTimedType = timedEffectAttribute.TimedType;

                // Effect Duration
                _defaultDuration = timedEffectAttribute.DurationSeconds;
            }
            else
            {
                Log.Error($"Timed effect {effectInfo} is missing {nameof(ChaosTimedEffectAttribute)}");
            }
        }

        public string ApplyTimedTypeSuffix(string effectName)
        {
            return Language.GetStringFormatted("TIMED_EFFECT_NAME_FORMAT", effectName, TimedType switch
            {
                TimedEffectType.UntilNextEffect => Language.GetString("TIMED_TYPE_UNTIL_NEXT_EFFECT_NAME"),
                TimedEffectType.UntilStageEnd => Language.GetString("TIMED_TYPE_UNTIL_STAGE_END_NAME"),
                TimedEffectType.FixedDuration => Language.GetStringFormatted("TIMED_TYPE_FIXED_DURATION_NAME", DurationSeconds),
                TimedEffectType.Permanent => Language.GetString("TIMED_TYPE_PERMANENT_NAME"),
                _ => throw new NotImplementedException($"{TimedType} is not implemented"),
            });
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
