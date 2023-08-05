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

        public readonly bool AllowDuplicates;

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

                _timedTypeConfig = effectInfo.BindConfig("Duration Type", _defaultTimedType, new ConfigDescription($"What should determine how long this effect lasts.\n\n{nameof(TimedEffectType.UntilStageEnd)}: Lasts until you exit the stage.\n{nameof(TimedEffectType.FixedDuration)}: Lasts for a set number of seconds.\n{nameof(TimedEffectType.Permanent)}: Lasts until the end of the run."));

                _defaultDuration = timedEffectAttribute.DurationSeconds;
                if (_defaultDuration < 0f)
                    _defaultDuration = 60f;

                _durationConfig = effectInfo.BindConfig("Effect Duration", _defaultDuration, new ConfigDescription($"How long the effect should last, in seconds.\nOnly takes effect if the Duration Type is set to {nameof(TimedEffectType.FixedDuration)}"));

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
        
        internal void AddRiskOfOptionsEntries()
        {
            ChaosEffectInfo effectInfo = ChaosEffectCatalog.GetEffectInfo(EffectIndex);

            ConfigEntry<bool> isEffectEnabledConfig = effectInfo.IsEnabledConfig;
            bool isEffectDisabled()
            {
                return isEffectEnabledConfig != null && !isEffectEnabledConfig.Value;
            }

            if (_timedTypeConfig != null)
            {
                ChaosEffectCatalog.AddEffectConfigOption(new ChoiceOption(_timedTypeConfig, new ChoiceConfig
                {
                    checkIfDisabled = isEffectDisabled,
                }));
            }

            if (_durationConfig != null)
            {
                ChaosEffectCatalog.AddEffectConfigOption(new StepSliderOption(_durationConfig, new StepSliderConfig
                {
                    formatString = "{0:F0}s",
                    min = 0f,
                    max = 120f,
                    increment = 5f,
                    checkIfDisabled = () => isEffectDisabled() || TimedType != TimedEffectType.FixedDuration
                }));
            }
        }
    }
}
