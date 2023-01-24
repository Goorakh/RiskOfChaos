using BepInEx.Configuration;
using RiskOfChaos.EffectDefinitions;
using RiskOfChaos.Utilities.Extensions;
using RiskOfOptions.OptionConfigs;
using RiskOfOptions.Options;
using RoR2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace RiskOfChaos.EffectHandling
{
    public readonly struct ChaosEffectInfo
    {
        public readonly int EffectIndex;

        public readonly string Identifier;

        public readonly string NameToken;

        public readonly Type EffectType;

        public readonly string ConfigSectionName;

        public readonly ConfigEntry<bool> IsEnabledConfig;
        public readonly ConfigEntry<float> SelectionWeightConfig;

        readonly MethodInfo[] _canActivateMethods;
        public bool CanActivate
        {
            get
            {
                if (_canActivateMethods == null)
                {
                    Log.Warning($"effect {Identifier} has null {nameof(_canActivateMethods)} array");
                    return false;
                }

                if (HasHardActivationCountCap && ActivationCount >= HardActivationCountCap)
                {
#if DEBUG
                    Log.Debug($"effect {Identifier} cannot activate due to activation cap of {HardActivationCountCap} reached ({ActivationCount} activations)");
#endif
                    return false;
                }

                if (_canActivateMethods.Length != 0 && _canActivateMethods.Any(m => (bool)m.Invoke(null, null) == false))
                {
                    return false;
                }

                return true;
            }
        }

        readonly ConfigEntry<float> _weightReductionPerActivation;
        readonly float _weightReductionPerActivationDefaultValue;

        public float EffectWeightMultiplierPerActivation => 1f - Mathf.Clamp01(_weightReductionPerActivation?.Value ?? _weightReductionPerActivationDefaultValue);

        readonly ConfigEntry<EffectActivationCountMode> _effectRepetitionCountMode;
        readonly EffectActivationCountMode _effectRepetitionCountModeDefaultValue;

        public EffectActivationCountMode EffectRepetitionCountMode => _effectRepetitionCountMode?.Value ?? _effectRepetitionCountModeDefaultValue;

        public readonly int HardActivationCountCap;
        public readonly bool HasHardActivationCountCap => HardActivationCountCap >= 0;

        public int ActivationCount
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => GetActivationCount(EffectRepetitionCountMode);
        }

        readonly MethodInfo[] _weightMultSelectorMethods;
        public float TotalSelectionWeight
        {
            get
            {
                float weight = Mathf.Pow(EffectWeightMultiplierPerActivation, ActivationCount) * SelectionWeightConfig.Value;

                if (_weightMultSelectorMethods != null)
                {
                    foreach (MethodInfo weightSelector in _weightMultSelectorMethods)
                    {
                        weight *= (float)weightSelector.Invoke(null, null);
                    }
                }

                return weight;
            }
        }

        readonly MethodInfo _getEffectNameFormatArgsMethod;
        public string DisplayName
        {
            get
            {
                if (_getEffectNameFormatArgsMethod != null)
                {
                    object[] args = (object[])_getEffectNameFormatArgsMethod.Invoke(null, null);
                    return Language.GetStringFormatted(NameToken, args);
                }
                else
                {
                    return Language.GetString(NameToken);
                }
            }
        }

        public ChaosEffectInfo(int effectIndex, ChaosEffectAttribute attribute)
        {
            EffectIndex = effectIndex;
            Identifier = attribute.Identifier;

            NameToken = $"EFFECT_{Identifier.ToUpper()}_NAME";

            if (attribute.target is Type effectType)
            {
                EffectType = effectType;

                if (!typeof(BaseEffect).IsAssignableFrom(effectType))
                {
                    Log.Error($"effect type {effectType.FullName} is not {nameof(BaseEffect)}");
                }
                else
                {
                    const BindingFlags FLAGS = BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public;
                    IEnumerable<MethodInfo> allMethods = effectType.GetAllMethodsRecursive(FLAGS);

                    _canActivateMethods = allMethods.WithAttribute<MethodInfo, EffectCanActivateAttribute>().ToArray();

                    _weightMultSelectorMethods = allMethods.WithAttribute<MethodInfo, EffectWeightMultiplierSelectorAttribute>().ToArray();

                    _getEffectNameFormatArgsMethod = allMethods.WithAttribute<MethodInfo, EffectNameFormatArgsAttribute>().FirstOrDefault();
                }
            }
            else
            {
                Log.Error($"attribute target is not a Type ({attribute.target})");
            }

            HardActivationCountCap = attribute.EffectActivationCountHardCap;

            ConfigSectionName = "Effect: " + (attribute.ConfigName ?? Language.GetString(NameToken, "en")).FilterConfigKey();

            IsEnabledConfig = Main.Instance.Config.Bind(new ConfigDefinition(ConfigSectionName, "Effect Enabled"), true, new ConfigDescription("If the effect should be able to be picked"));

            SelectionWeightConfig = Main.Instance.Config.Bind(new ConfigDefinition(ConfigSectionName, "Effect Weight"), attribute.DefaultSelectionWeight, new ConfigDescription("How likely the effect is to be picked, higher value means more likely, lower value means less likely"));

            _weightReductionPerActivationDefaultValue = attribute.EffectWeightReductionPercentagePerActivation / 100f;
            _weightReductionPerActivation = Main.Instance.Config.Bind(new ConfigDefinition(ConfigSectionName, "Effect Repetition Reduction Percentage"), _weightReductionPerActivationDefaultValue, new ConfigDescription("The percentage reduction to apply to the weight value per activation, setting this to any value above 0 will make the effect less likely to happen several times"));

            _effectRepetitionCountModeDefaultValue = attribute.EffectRepetitionWeightCalculationMode;
            _effectRepetitionCountMode = Main.Instance.Config.Bind(new ConfigDefinition(ConfigSectionName, "Effect Repetition Count Mode"), _effectRepetitionCountModeDefaultValue, new ConfigDescription($"Controls how the Reduction Percentage will be applied.\n\n{nameof(EffectActivationCountMode.PerStage)}: Only the activations on the current stage are considered, and the weight reduction is reset on stage start.\n\n{nameof(EffectActivationCountMode.PerRun)}: All activations during the current run are considered."));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly int GetActivationCount(EffectActivationCountMode countMode)
        {
            return ChaosEffectDispatcher.GetEffectActivationCount(EffectIndex, countMode);
        }

        public readonly void AddRiskOfOptionsEntries()
        {
            ChaosEffectCatalog.AddEffectConfigOption(new CheckBoxOption(IsEnabledConfig));

            ChaosEffectCatalog.AddEffectConfigOption(new StepSliderOption(SelectionWeightConfig, new StepSliderConfig
            {
                formatString = "{0:F1}",
                increment = 0.1f,
                min = 0f,
                max = 2.5f
            }));

            ChaosEffectCatalog.AddEffectConfigOption(new StepSliderOption(_weightReductionPerActivation, new StepSliderConfig
            {
                formatString = "-{0:P0}",
                increment = 0.01f,
                min = 0f,
                max = 1f
            }));

            ChaosEffectCatalog.AddEffectConfigOption(new ChoiceOption(_effectRepetitionCountMode));
        }

        public readonly BaseEffect InstantiateEffect(Xoroshiro128Plus effectRNG)
        {
            if (EffectType == null)
            {
                Log.Error($"Cannot instantiate effect {Identifier}, {nameof(EffectType)} is null!");
                return null;
            }

            BaseEffect effectInstance = (BaseEffect)Activator.CreateInstance(EffectType);
            effectInstance.RNG = effectRNG;
            return effectInstance;
        }

        public readonly string GetActivationMessage()
        {
            return Language.GetStringFormatted("CHAOS_EFFECT_ACTIVATE", DisplayName);
        }

        public override readonly string ToString()
        {
            return Identifier;
        }
    }
}
