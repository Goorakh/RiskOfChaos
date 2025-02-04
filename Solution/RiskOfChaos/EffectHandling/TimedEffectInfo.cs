using BepInEx.Configuration;
using RiskOfChaos.Components;
using RiskOfChaos.ConfigHandling;
using RiskOfChaos.ConfigHandling.AcceptableValues;
using RiskOfChaos.EffectHandling.Controllers;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectComponents;
using RiskOfChaos.ModificationController.Effect;
using RiskOfChaos.SaveHandling;
using RiskOfOptions.OptionConfigs;
using RoR2;
using System;
using System.Collections.Generic;

namespace RiskOfChaos.EffectHandling
{
    public class TimedEffectInfo : ChaosEffectInfo
    {
        readonly bool _allowDuplicates;
        readonly ConfigHolder<bool> _allowDuplicatesOverrideConfig;
        public bool AllowDuplicates => _allowDuplicatesOverrideConfig?.Value ?? _allowDuplicates;

        readonly ConfigHolder<ConfigTimedEffectType> _timedType;
        public TimedEffectType TimedType => (TimedEffectType)_timedType.Value;

        readonly ConfigHolder<float> _fixedTimeDuration;
        public float DurationSeconds => _fixedTimeDuration.Value;

        public readonly bool HideFromEffectsListWhenPermanent;
        public bool ShouldDisplayOnHUD => GetShouldDisplayOnHUD(TimedType);

        public bool CanStack => GetCanStack(TimedType);

        public readonly bool IgnoreDurationModifiers;

        readonly ConfigHolder<int> _stageCountDuration;
        public int StageDuration => _stageCountDuration.Value;

        public float BaseDuration => GetBaseDuration(TimedType);

        public float Duration => GetDuration(TimedType);

        public readonly ConfigHolder<bool> AlwaysActiveEnabledConfig;
        public readonly ConfigHolder<int> AlwaysActiveStackCountConfig;

        public int AlwaysActiveCount
        {
            get
            {
                if (!AlwaysActiveEnabledConfig.Value)
                    return 0;

                if (AllowDuplicates && AlwaysActiveStackCountConfig != null)
                {
                    return AlwaysActiveStackCountConfig.Value;
                }
                else
                {
                    return 1;
                }
            }
        }

        public TimedEffectInfo(ChaosEffectIndex effectIndex, ChaosTimedEffectAttribute attribute, ConfigFile configFile) : base(effectIndex, attribute, configFile)
        {
            ConfigTimedEffectType configTimedType;
            if (attribute.TimedType == TimedEffectType.AlwaysActive)
            {
                Log.Warning($"Effect {Identifier} is defined with a duration type of {nameof(TimedEffectType.AlwaysActive)}, this is not supported, assuming {nameof(TimedEffectType.Permanent)}");
                configTimedType = ConfigTimedEffectType.Permanent;
            }
            else
            {
                configTimedType = (ConfigTimedEffectType)attribute.TimedType;
            }

            _timedType = ConfigFactory<ConfigTimedEffectType>.CreateConfig("Duration Type", configTimedType)
                                                              .Description($"""
                                                               What should determine how long this effect lasts.

                                                               {nameof(ConfigTimedEffectType.UntilStageEnd)}: Lasts until you exit the stage.
                                                               {nameof(ConfigTimedEffectType.FixedDuration)}: Lasts for a set number of seconds.
                                                               {nameof(ConfigTimedEffectType.Permanent)}: Lasts until the end of the run.
                                                               """)
                                                              .OptionConfig(new ChoiceConfig())
                                                              .Build();

            float defaultDuration = attribute.DurationSeconds;
            if (defaultDuration < 0f)
                defaultDuration = 60f;

            _fixedTimeDuration = ConfigFactory<float>.CreateConfig("Effect Time Duration", defaultDuration)
                                                     .Description($"""
                                                      How long the effect should last, in seconds.
                                                      Only takes effect if the Duration Type is set to {nameof(TimedEffectType.FixedDuration)}
                                                      """)
                                                     .AcceptableValues(new AcceptableValueMin<float>(0f))
                                                     .OptionConfig(new FloatFieldConfig
                                                     {
                                                         FormatString = "{0}s",
                                                         Min = 0f,
                                                         checkIfDisabled = () => TimedType != TimedEffectType.FixedDuration
                                                     })
                                                     .RenamedFrom("Effect Duration")
                                                     .Build();

            _stageCountDuration =
                ConfigFactory<int>.CreateConfig("Effect Stage Duration", attribute.DefaultStageCountDuration)
                                  .Description($"""
                                   How many stages this effect should last.
                                   Only applies if Duration Type is set to {nameof(TimedEffectType.UntilStageEnd)}
                                   """)
                                  .AcceptableValues(new AcceptableValueMin<int>(1))
                                  .OptionConfig(new IntFieldConfig
                                  {
                                      Min = 1,
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

            AlwaysActiveEnabledConfig =
                ConfigFactory<bool>.CreateConfig("Permanently Active", false)
                                   .Description(_allowDuplicates ? "If one or more instances of this effect should always be active during a run" : "If this effect should always be active during a run")
                                   .OptionConfig(new CheckBoxConfig())
                                   .Build();

            if (_allowDuplicates)
            {
                AlwaysActiveStackCountConfig =
                    ConfigFactory<int>.CreateConfig("Permanently Active Duplicate Count", 1)
                                      .Description("How many instances of this effect should always be active, only takes effect if 'Permanently Active' is set to true and 'Allow Duplicates' is set to true")
                                      .AcceptableValues(new AcceptableValueMin<int>(1))
                                      .OptionConfig(new IntFieldConfig
                                      {
                                          Min = 1,
                                          checkIfDisabled = () => !AlwaysActiveEnabledConfig.Value || !AllowDuplicates
                                      })
                                      .Build();
            }

            HideFromEffectsListWhenPermanent = attribute.HideFromEffectsListWhenPermanent;

            IgnoreDurationModifiers = attribute.IgnoreDurationModifiers;
        }

        public bool GetCanStack(TimedEffectType timedType)
        {
            switch (timedType)
            {
                case TimedEffectType.UntilStageEnd:
                case TimedEffectType.FixedDuration:
                    return true;
                default:
                    return false;
            }
        }

        public float GetBaseDuration(TimedEffectType timedType)
        {
            switch (timedType)
            {
                case TimedEffectType.UntilStageEnd:
                    return StageDuration;
                case TimedEffectType.FixedDuration:
                    return DurationSeconds;
                default:
                    return 1f;
            }
        }

        public float GetDuration(TimedEffectType timedType)
        {
            float duration = GetBaseDuration(timedType);

            if (EffectModificationManager.Instance)
            {
                EffectModificationManager.Instance.TryModifyDuration(this, ref duration);
            }

            return duration;
        }

        public bool GetShouldDisplayOnHUD(TimedEffectType timedType)
        {
            switch (timedType)
            {
                case TimedEffectType.Permanent:
                    return !HideFromEffectsListWhenPermanent;
                case TimedEffectType.AlwaysActive:
                    return !HideFromEffectsListWhenPermanent && Configs.UI.DisplayAlwaysActiveEffects.Value;
                default:
                    return true;
            }
        }

        protected override void modifyPrefabComponents(List<Type> componentTypes)
        {
            base.modifyPrefabComponents(componentTypes);
            componentTypes.AddRange([
                typeof(SetDontDestroyOnLoad),
                typeof(DestroyOnRunEnd),
                typeof(ChaosEffectDurationComponent),
                typeof(ObjectSerializationComponent)
            ]);
        }

        public override void BindConfigs()
        {
            base.BindConfigs();

            _timedType?.Bind(this);

            _fixedTimeDuration?.Bind(this);

            _stageCountDuration?.Bind(this);

            _allowDuplicatesOverrideConfig?.Bind(this);

            AlwaysActiveEnabledConfig?.Bind(this);

            AlwaysActiveStackCountConfig?.Bind(this);
        }

        public override bool CanActivate(in EffectCanActivateContext context)
        {
            if (!base.CanActivate(context))
                return false;

            if (!CanStack && !AllowDuplicates)
            {
                if (ChaosEffectTracker.Instance && ChaosEffectTracker.Instance.IsAnyInstanceOfTimedEffectRelevantForContext(this, context))
                {
                    Log.Debug($"Duplicate effect {this} cannot activate");

                    return false;
                }
            }

            return true;
        }
    }
}
