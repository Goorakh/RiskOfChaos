using BepInEx.Configuration;
using HG;
using RiskOfChaos.EffectDefinitions;
using RiskOfChaos.EffectHandling.Controllers;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Data;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
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
    public readonly struct ChaosEffectInfo : IEquatable<ChaosEffectInfo>
    {
        public readonly ChaosEffectIndex EffectIndex;

        public readonly string Identifier;

        public readonly string NameToken;

        public readonly Type EffectType;

        public readonly string ConfigSectionName;

        readonly ChaosEffectCanActivateMethod[] _canActivateMethods;

        public readonly bool CanActivate(EffectCanActivateContext context)
        {
            if (_canActivateMethods == null)
            {
                Log.Warning($"effect {Identifier} has null {nameof(_canActivateMethods)} array");
                return false;
            }

            if (_isEnabledConfig != null && !_isEnabledConfig.Value)
            {
#if DEBUG
                Log.Debug($"effect {Identifier} cannot activate due to: Disabled in config");
#endif
                return false;
            }

            if (HasHardStageActivationCountCap && GetActivationCount(EffectActivationCountMode.PerStage) >= HardStageActivationCountCap)
            {
#if DEBUG
                Log.Debug($"effect {Identifier} cannot activate due to stage activation cap of {HardStageActivationCountCap} reached ({GetActivationCount(EffectActivationCountMode.PerStage)} activations)");
#endif
                return false;
            }

            if (_canActivateMethods.Length != 0 && _canActivateMethods.Any(m => m.Invoke(context) == false))
            {
                return false;
            }

            return true;
        }

        readonly ConfigEntry<bool> _isEnabledConfig;
        readonly ConfigEntry<float> _selectionWeightConfig;

        readonly ConfigEntry<float> _weightReductionPerActivation;
        readonly float _weightReductionPerActivationDefaultValue;

        public float EffectWeightMultiplierPerActivation => 1f - Mathf.Clamp01(_weightReductionPerActivation?.Value ?? _weightReductionPerActivationDefaultValue);

        readonly ConfigEntry<EffectActivationCountMode> _effectRepetitionCountMode;
        readonly EffectActivationCountMode _effectRepetitionCountModeDefaultValue;

        public EffectActivationCountMode EffectRepetitionCountMode => _effectRepetitionCountMode?.Value ?? _effectRepetitionCountModeDefaultValue;

        public readonly int HardStageActivationCountCap;
        public readonly bool HasHardStageActivationCountCap => HardStageActivationCountCap >= 0;

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
                float weight = Mathf.Pow(EffectWeightMultiplierPerActivation, ActivationCount) * _selectionWeightConfig.Value;

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

        public readonly bool IsNetworked;

        readonly string[] _previousConfigSectionNames;

        public ChaosEffectInfo(ChaosEffectIndex effectIndex, ChaosEffectAttribute attribute)
        {
            EffectIndex = effectIndex;
            Identifier = attribute.Identifier;

            NameToken = $"EFFECT_{Identifier.ToUpper()}_NAME";

            if (attribute.target is Type effectType)
            {
                EffectType = effectType;

                EffectConfigBackwardsCompatibilityAttribute configBackwardsCompatibilityAttribute = EffectType.GetCustomAttribute<EffectConfigBackwardsCompatibilityAttribute>();
                if (configBackwardsCompatibilityAttribute != null)
                {
                    _previousConfigSectionNames = configBackwardsCompatibilityAttribute.ConfigSectionNames;
                }
                else
                {
                    _previousConfigSectionNames = Array.Empty<string>();
                }

                if (!typeof(BaseEffect).IsAssignableFrom(effectType))
                {
                    Log.Error($"effect type {effectType.FullName} is not {nameof(BaseEffect)}");
                }
                else
                {
                    const BindingFlags FLAGS = BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public;
                    IEnumerable<MethodInfo> allMethods = effectType.GetAllMethodsRecursive(FLAGS);

                    _canActivateMethods = allMethods.WithAttribute<MethodInfo, EffectCanActivateAttribute>().Select(m => new ChaosEffectCanActivateMethod(m)).ToArray();

                    _weightMultSelectorMethods = allMethods.WithAttribute<MethodInfo, EffectWeightMultiplierSelectorAttribute>().ToArray();

                    _getEffectNameFormatArgsMethod = allMethods.WithAttribute<MethodInfo, EffectNameFormatArgsAttribute>().FirstOrDefault();
                }
            }
            else
            {
                Log.Error($"attribute target is not a Type ({attribute.target})");
            }

            HardStageActivationCountCap = attribute.EffectStageActivationCountHardCap;

            IsNetworked = attribute.IsNetworked;

            ConfigSectionName = "Effect: " + (attribute.ConfigName ?? Language.GetString(NameToken, "en")).FilterConfigKey();

            if (_previousConfigSectionNames != null && _previousConfigSectionNames.Length > 0)
            {
                int index = Array.IndexOf(_previousConfigSectionNames, ConfigSectionName);
                if (index >= 0)
                {
                    ArrayUtils.ArrayRemoveAtAndResize(ref _previousConfigSectionNames, index);
                }
            }

            _isEnabledConfig = BindConfig("Effect Enabled", true, new ConfigDescription("If the effect should be able to be picked"));

            _selectionWeightConfig = BindConfig("Effect Weight", attribute.DefaultSelectionWeight, new ConfigDescription("How likely the effect is to be picked, higher value means more likely, lower value means less likely"));

            _weightReductionPerActivationDefaultValue = attribute.EffectWeightReductionPercentagePerActivation / 100f;
            _weightReductionPerActivation = BindConfig("Effect Repetition Reduction Percentage", _weightReductionPerActivationDefaultValue, new ConfigDescription("The percentage reduction to apply to the weight value per activation, setting this to any value above 0 will make the effect less likely to happen several times"));

            _effectRepetitionCountModeDefaultValue = attribute.EffectRepetitionWeightCalculationMode;
            _effectRepetitionCountMode = BindConfig("Effect Repetition Count Mode", _effectRepetitionCountModeDefaultValue, new ConfigDescription($"Controls how the Reduction Percentage will be applied.\n\n{nameof(EffectActivationCountMode.PerStage)}: Only the activations on the current stage are considered, and the weight reduction is reset on stage start.\n\n{nameof(EffectActivationCountMode.PerRun)}: All activations during the current run are considered."));

            foreach (MemberInfo member in EffectType.GetMembers(BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.DeclaredOnly)
                                                    .WithAttribute<MemberInfo, InitEffectMemberAttribute>())
            {
                foreach (InitEffectMemberAttribute initEffectMember in member.GetCustomAttributes<InitEffectMemberAttribute>())
                {
                    initEffectMember.ApplyTo(member, this);
                }
            }
        }

        public readonly ConfigEntry<T> BindConfig<T>(string key, T defaultValue, ConfigDescription description, IEqualityComparer<T> valueComparer = null)
        {
            valueComparer ??= EqualityComparer<T>.Default;

            ConfigDefinition configDefinition = new ConfigDefinition(ConfigSectionName, key);
            if (Main.Instance.Config.TryGetEntry(configDefinition, out ConfigEntry<T> existingEntry))
                return existingEntry;

            ConfigEntry<T> result = Main.Instance.Config.Bind(configDefinition, defaultValue, description);

            if (_previousConfigSectionNames != null)
            {
                for (int i = _previousConfigSectionNames.Length - 1; i >= 0; i--)
                {
                    // TryGetValue only works if the config is already binded, so we have to re-bind it every time to check :(
                    ConfigEntry<T> previousConfigEntry = Main.Instance.Config.Bind(_previousConfigSectionNames[i], key, defaultValue);
                    if (!valueComparer.Equals(previousConfigEntry.Value, defaultValue))
                    {
                        result.Value = previousConfigEntry.Value;

#if DEBUG
                        Log.Debug($"Previous config entry found for {ConfigSectionName}:{key}, overriding value");
#endif

                        break;
                    }
                }

                foreach (string configSectionName in _previousConfigSectionNames)
                {
                    Main.Instance.Config.Remove(new ConfigDefinition(configSectionName, key));
                }
            }

            return result;
        }

        internal readonly void Validate()
        {
            string displayName = DisplayName;
            if (string.IsNullOrWhiteSpace(displayName))
            {
                Log.Error($"{this}: Null or empty display name");
            }

            if (displayName == NameToken)
            {
                Log.Error($"{this}: Invalid name token");
            }

            if (Identifier.Any(char.IsUpper))
            {
                Log.Warning($"{this}: Effect identifier has uppercase characters");
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly int GetActivationCount(EffectActivationCountMode countMode)
        {
            ChaosEffectActivationCounterHandler effectActivationCounterHandler = ChaosEffectActivationCounterHandler.Instance;
            if (!effectActivationCounterHandler)
                return 0;

            return effectActivationCounterHandler.GetEffectActivationCount(this, countMode);
        }

        public readonly void AddRiskOfOptionsEntries()
        {
            if (_isEnabledConfig != null)
            {
                ChaosEffectCatalog.AddEffectConfigOption(new CheckBoxOption(_isEnabledConfig));
            }

            if (_selectionWeightConfig != null)
            {
                ChaosEffectCatalog.AddEffectConfigOption(new StepSliderOption(_selectionWeightConfig, new StepSliderConfig
                {
                    formatString = "{0:F1}",
                    increment = 0.1f,
                    min = 0f,
                    max = 2.5f
                }));
            }

            if (_weightReductionPerActivation != null)
            {
                ChaosEffectCatalog.AddEffectConfigOption(new StepSliderOption(_weightReductionPerActivation, new StepSliderConfig
                {
                    formatString = "-{0:P0}",
                    increment = 0.01f,
                    min = 0f,
                    max = 1f
                }));
            }

            if (_effectRepetitionCountMode != null)
            {
                ChaosEffectCatalog.AddEffectConfigOption(new ChoiceOption(_effectRepetitionCountMode));
            }
        }

        public override readonly string ToString()
        {
            return Identifier;
        }

        public override bool Equals(object obj)
        {
            return obj is ChaosEffectInfo info && Equals(info);
        }

        public bool Equals(ChaosEffectInfo other)
        {
            return EffectIndex == other.EffectIndex;
        }

        public override int GetHashCode()
        {
            return -865576688 + EffectIndex.GetHashCode();
        }

        public static bool operator ==(ChaosEffectInfo left, ChaosEffectInfo right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ChaosEffectInfo left, ChaosEffectInfo right)
        {
            return !(left == right);
        }
    }
}
