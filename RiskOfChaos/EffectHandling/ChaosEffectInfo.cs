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
using UnityEngine;

namespace RiskOfChaos.EffectHandling
{
    public readonly struct ChaosEffectInfo
    {
        public readonly int EffectIndex;

        public readonly string Identifier;

        public readonly string NameToken;
        // public readonly string DescriptionToken;

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

        public readonly float EffectWeightMultiplierPerActivation;
        public readonly EffectActivationCountMode EffectRepetitionWeightCalculationMode;

        public readonly int HardActivationCountCap;
        public readonly bool HasHardActivationCountCap => HardActivationCountCap >= 0;

        public int ActivationCount => ChaosEffectDispatcher.GetEffectActivationCount(EffectIndex, EffectRepetitionWeightCalculationMode);

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
            // DescriptionToken = attribute.HasDescription ? $"EFFECT_{Identifier.ToUpper()}_DESC" : null;

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

            EffectWeightMultiplierPerActivation = 1f - (attribute.EffectWeightReductionPercentagePerActivation / 100f);
            EffectRepetitionWeightCalculationMode = attribute.EffectRepetitionWeightCalculationMode;

            HardActivationCountCap = attribute.EffectActivationCountHardCap;

            ConfigSectionName = "Effect: " + (attribute.ConfigName ?? Language.GetString(NameToken, "en"));

            IsEnabledConfig = Main.Instance.Config.Bind(new ConfigDefinition(ConfigSectionName, "Effect Enabled"), true, new ConfigDescription("If the effect should be able to be picked"));

            SelectionWeightConfig = Main.Instance.Config.Bind(new ConfigDefinition(ConfigSectionName, "Effect Weight"), attribute.DefaultSelectionWeight, new ConfigDescription("How likely the effect is to be picked, higher value means more likely, lower value means less likely"));
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
    }
}
