using BepInEx.Configuration;
using RiskOfChaos.ConfigHandling;
using RiskOfChaos.ConfigHandling.AcceptableValues;
using RiskOfChaos.EffectDefinitions;
using RiskOfChaos.EffectHandling.Controllers;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.Formatting;
using RiskOfChaos.ModifierController.Effect;
using RiskOfOptions.OptionConfigs;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectHandling
{
    public class TimedEffectInfo : ChaosEffectInfo
    {
        readonly bool _allowDuplicates;
        readonly ConfigHolder<bool> _allowDuplicatesOverrideConfig;
        public bool AllowDuplicates => _allowDuplicatesOverrideConfig?.Value ?? _allowDuplicates;

        readonly ConfigHolder<TimedEffectType> _timedType;
        public TimedEffectType TimedType => _timedType.Value;

        readonly ConfigHolder<float> _fixedTimeDuration;
        public float DurationSeconds => _fixedTimeDuration.Value;

        public readonly bool HideFromEffectsListWhenPermanent;
        public bool ShouldDisplayOnHUD => !HideFromEffectsListWhenPermanent || TimedType != TimedEffectType.Permanent;

        public bool CanStack => TimedType switch
        {
            TimedEffectType.UntilStageEnd or TimedEffectType.FixedDuration => true,
            _ => false
        };

        public readonly bool IgnoreDurationModifiers;

        readonly ConfigHolder<int> _stageCountDuration;
        public float MaxStocks
        {
            get
            {
                float maxStocks;
                if (TimedType == TimedEffectType.UntilStageEnd)
                {
                    maxStocks = _stageCountDuration.Value;
                }
                else
                {
                    maxStocks = 1f;
                }

                if (!IgnoreDurationModifiers && EffectModificationManager.Instance)
                {
                    maxStocks *= EffectModificationManager.Instance.DurationMultiplier;
                }

                return maxStocks;
            }
        }

        readonly ConfigHolder<bool> _alwaysActiveEnabled;
        readonly ConfigHolder<int> _alwaysActiveStackCount;

        public int AlwaysActiveCount
        {
            get
            {
                if (!_alwaysActiveEnabled.Value)
                    return 0;

                if (AllowDuplicates && _alwaysActiveStackCount != null)
                {
                    return _alwaysActiveStackCount.Value;
                }
                else
                {
                    return 1;
                }
            }
        }

        public TimedEffectInfo(ChaosEffectIndex effectIndex, ChaosTimedEffectAttribute attribute, ConfigFile configFile) : base(effectIndex, attribute, configFile)
        {
            _timedType = ConfigFactory<TimedEffectType>.CreateConfig("Duration Type", attribute.TimedType)
                                                       .Description($"What should determine how long this effect lasts.\n\n{nameof(TimedEffectType.UntilStageEnd)}: Lasts until you exit the stage.\n{nameof(TimedEffectType.FixedDuration)}: Lasts for a set number of seconds.\n{nameof(TimedEffectType.Permanent)}: Lasts until the end of the run.")
                                                       .OptionConfig(new ChoiceConfig())
                                                       .ValueValidator(CommonValueValidators.DefinedEnumValue<TimedEffectType>())
                                                       .Build();

            float defaultDuration = attribute.DurationSeconds;
            if (defaultDuration < 0f)
                defaultDuration = 60f;

            _fixedTimeDuration = ConfigFactory<float>.CreateConfig("Effect Time Duration", defaultDuration)
                                                     .Description($"How long the effect should last, in seconds.\nOnly takes effect if the Duration Type is set to {nameof(TimedEffectType.FixedDuration)}")
                                                     .AcceptableValues(new AcceptableValueMin<float>(0f))
                                                     .OptionConfig(new StepSliderConfig
                                                     {
                                                         formatString = "{0:F0}s",
                                                         min = 0f,
                                                         max = 120f,
                                                         increment = 5f,
                                                         checkIfDisabled = () => TimedType != TimedEffectType.FixedDuration
                                                     })
                                                     .RenamedFrom("Effect Duration")
                                                     .Build();

            _stageCountDuration =
                ConfigFactory<int>.CreateConfig("Effect Stage Duration", attribute.DefaultStageCountDuration)
                                  .Description($"How many stages this effect should last.\nOnly applies if Duration Type is set to {nameof(TimedEffectType.UntilStageEnd)}")
                                  .AcceptableValues(new AcceptableValueMin<int>(1))
                                  .OptionConfig(new IntSliderConfig
                                  {
                                      min = 1,
                                      max = 10,
                                      checkIfDisabled = () => TimedType != TimedEffectType.UntilStageEnd
                                  })
                                  .Build();

            _allowDuplicates = attribute.AllowDuplicates;
            if (_allowDuplicates)
            {
                _allowDuplicatesOverrideConfig =
                    ConfigFactory<bool>.CreateConfig("Allow Duplicates", _allowDuplicates)
                                       .Description("If more than one instance of this effect is allowed to be active at the same time")
                                       .OptionConfig(new CheckBoxConfig())
                                       .Build();
            }

            _alwaysActiveEnabled =
                ConfigFactory<bool>.CreateConfig("Permanently Active", false)
                                   .Description(_allowDuplicates ? "If one or more instances of this effect should always be active during a run" : "If this effect should always be active during a run")
                                   .OptionConfig(new CheckBoxConfig())
                                   .Build();

            if (_allowDuplicates)
            {
                _alwaysActiveStackCount =
                    ConfigFactory<int>.CreateConfig("Permanently Active Duplicate Count", 1)
                                      .Description("How many instances of this effect should always be active, only takes effect if 'Permanently Active' is set to true and 'Allow Duplicates' is set to true")
                                      .AcceptableValues(new AcceptableValueMin<int>(1))
                                      .OptionConfig(new IntSliderConfig
                                      {
                                          min = 1,
                                          max = 20,
                                          checkIfDisabled = () => !_alwaysActiveEnabled.Value || !AllowDuplicates
                                      })
                                      .Build();
            }

            HideFromEffectsListWhenPermanent = attribute.HideFromEffectsListWhenPermanent;

            IgnoreDurationModifiers = attribute.IgnoreDurationModifiers;
        }

        public override void BindConfigs()
        {
            base.BindConfigs();

            _timedType?.Bind(this);

            _fixedTimeDuration?.Bind(this);

            _stageCountDuration?.Bind(this);

            _allowDuplicatesOverrideConfig?.Bind(this);

            _alwaysActiveEnabled?.Bind(this);

            _alwaysActiveStackCount?.Bind(this);
        }

        public override bool CanActivate(in EffectCanActivateContext context)
        {
            if (!base.CanActivate(context))
                return false;

            if (!CanStack && !AllowDuplicates)
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

        public override string GetDisplayName(EffectNameFormatter formatter, EffectNameFormatFlags formatFlags = EffectNameFormatFlags.All)
        {
            string displayName = base.GetDisplayName(formatter, formatFlags);

            if ((formatFlags & EffectNameFormatFlags.TimedType) != 0)
            {
                float durationMultiplier = MaxStocks;

                switch (TimedType)
                {
                    case TimedEffectType.UntilStageEnd:
                        int stageCount = Mathf.CeilToInt(durationMultiplier);
                        string token = stageCount == 1 ? "TIMED_TYPE_UNTIL_STAGE_END_SINGLE_FORMAT" : "TIMED_TYPE_UNTIL_STAGE_END_MULTI_FORMAT";
                        return Language.GetStringFormatted(token, displayName, stageCount);
                    case TimedEffectType.FixedDuration:
                        return Language.GetStringFormatted("TIMED_TYPE_FIXED_DURATION_FORMAT", displayName, DurationSeconds * durationMultiplier);
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
                    timedEffect.TimedType = args.OverrideDurationType ?? TimedType;

                    if (timedEffect.TimedType == TimedEffectType.FixedDuration)
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
