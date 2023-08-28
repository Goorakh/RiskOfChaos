using BepInEx.Configuration;
using RiskOfChaos.ConfigHandling;
using RiskOfChaos.EffectDefinitions;
using RiskOfChaos.EffectHandling.Controllers;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfOptions.OptionConfigs;
using RoR2;
using System.Reflection;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectHandling
{
    public class TimedEffectInfo : ChaosEffectInfo
    {
        public readonly bool AllowDuplicates;

        readonly ConfigHolder<TimedEffectType> _timedType;
        public TimedEffectType TimedType => _timedType.Value;

        readonly ConfigHolder<float> _duration;
        public float DurationSeconds => _duration.Value;

        public TimedEffectInfo(ChaosEffectIndex effectIndex, ChaosEffectAttribute attribute, ConfigFile configFile) : base(effectIndex, attribute, configFile)
        {
            ChaosTimedEffectAttribute timedEffectAttribute = EffectType.GetCustomAttribute<ChaosTimedEffectAttribute>();
            if (timedEffectAttribute != null)
            {
                _timedType = ConfigFactory<TimedEffectType>.CreateConfig("Duration Type", timedEffectAttribute.TimedType)
                                                           .Description($"What should determine how long this effect lasts.\n\n{nameof(TimedEffectType.UntilStageEnd)}: Lasts until you exit the stage.\n{nameof(TimedEffectType.FixedDuration)}: Lasts for a set number of seconds.\n{nameof(TimedEffectType.Permanent)}: Lasts until the end of the run.")
                                                           .OptionConfig(new ChoiceConfig())
                                                           .ValueValidator(CommonValueValidators.DefinedEnumValue<TimedEffectType>())
                                                           .Build();

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

                AllowDuplicates = timedEffectAttribute.AllowDuplicates;
            }
            else
            {
                Log.Error($"Timed effect {this} is missing {nameof(ChaosTimedEffectAttribute)}");
            }
        }

        public override void BindConfigs()
        {
            base.BindConfigs();

            _timedType?.Bind(this);

            _duration?.Bind(this);
        }

        public override bool CanActivate(in EffectCanActivateContext context)
        {
            if (!base.CanActivate(context))
                return false;

            if (!AllowDuplicates)
            {
                if (TimedChaosEffectHandler.Instance && TimedChaosEffectHandler.Instance.AnyInstanceOfEffectActive(this, context))
                {
#if DEBUG
                    Log.Debug($"Duplicate effect {this} cannot activate");
#endif

                    return false;
                }
            }

            return true;
        }

        public override string GetDisplayName(EffectNameFormatFlags formatFlags = EffectNameFormatFlags.All)
        {
            string displayName = base.GetDisplayName(formatFlags);

            if ((formatFlags & EffectNameFormatFlags.TimedType) != 0)
            {
                switch (TimedType)
                {
                    case TimedEffectType.UntilStageEnd:
                        return Language.GetStringFormatted("TIMED_TYPE_UNTIL_STAGE_END_FORMAT", displayName);
                    case TimedEffectType.FixedDuration:
                        return Language.GetStringFormatted("TIMED_TYPE_FIXED_DURATION_FORMAT", displayName, DurationSeconds);
                    case TimedEffectType.Permanent:
                        return Language.GetStringFormatted("TIMED_TYPE_PERMANENT_FORMAT", displayName);
                    default:
                        Log.Warning($"Timed type {TimedType} is not implemented");
                        return displayName;
                }
            }
            else
            {
                return displayName;
            }
        }

        public override BaseEffect CreateInstance(in CreateEffectInstanceArgs args)
        {
            BaseEffect effectInstance = base.CreateInstance(args);
            if (effectInstance is TimedEffect timedEffect)
            {
                if (NetworkServer.active)
                {
                    timedEffect.TimedType = TimedType;

                    if (TimedType == TimedEffectType.FixedDuration)
                    {
                        timedEffect.DurationSeconds = DurationSeconds;
                    }
                }
            }
            else
            {
                Log.Error($"Effect info {this} is marked as timed, but instance is not of type {nameof(TimedEffect)} ({effectInstance})");
            }

            return effectInstance;
        }
    }
}
