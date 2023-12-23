using BepInEx.Configuration;
using HG;
using MonoMod.Utils;
using RiskOfChaos.ConfigHandling;
using RiskOfChaos.EffectDefinitions;
using RiskOfChaos.EffectHandling.Controllers;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Data;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.EffectHandling.Formatting;
using RiskOfChaos.Utilities;
using RiskOfChaos.Utilities.Extensions;
using RiskOfOptions.OptionConfigs;
using RoR2;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectHandling
{
    public class ChaosEffectInfo : IEquatable<ChaosEffectInfo>
    {
        public readonly ChaosEffectIndex EffectIndex;

        public readonly string Identifier;

        public readonly string NameToken;

        public readonly Type EffectType;

        public readonly string ConfigSectionName;

        readonly ChaosEffectCanActivateMethod[] _canActivateMethods = Array.Empty<ChaosEffectCanActivateMethod>();

        public readonly ReadOnlyCollection<TimedEffectInfo> IncompatibleEffects = Empty<TimedEffectInfo>.ReadOnlyCollection;

        public readonly ConfigHolder<bool> IsEnabledConfig;
        readonly ConfigHolder<float> _selectionWeightConfig;

        readonly ConfigHolder<KeyboardShortcut> _activationShortcut;
        public bool IsActivationShortcutPressed => _activationShortcut != null && _activationShortcut.Value.IsDownIgnoringBlockerKeys();

        readonly EffectWeightMultiplierDelegate[] _effectWeightMultipliers = Array.Empty<EffectWeightMultiplierDelegate>();
        public float TotalSelectionWeight
        {
            get
            {
                float weight = _selectionWeightConfig.Value;

                // For seeded selection to be deterministic, effect weights have to stay constant, so no variable weights allowed in this mode
                if (Configs.EffectSelection.SeededEffectSelection.Value)
                    return weight;

                foreach (EffectWeightMultiplierDelegate getEffectWeightMultiplier in _effectWeightMultipliers)
                {
                    weight *= getEffectWeightMultiplier();
                }

                return weight;
            }
        }

        readonly GetEffectNameFormatterDelegate _getEffectNameFormatter;

        EffectNameFormatter _cachedNameFormatter;

        public static event Action<ChaosEffectInfo> OnEffectNameFormatterDirty;

        bool _nameFormatterDirty;
        public bool NameFormatterDirty
        {
            get
            {
                return _nameFormatterDirty;
            }
            private set
            {
                if (_nameFormatterDirty == value)
                    return;

                _nameFormatterDirty = value;

                if (_nameFormatterDirty)
                {
                    OnEffectNameFormatterDirty?.Invoke(this);
                }
            }
        }

        public EffectNameFormatter LocalDisplayNameFormatter
        {
            get
            {
                if (_getEffectNameFormatter != null)
                {
                    if (_cachedNameFormatter is null || NameFormatterDirty)
                    {
                        _cachedNameFormatter = _getEffectNameFormatter();
                        NameFormatterDirty = false;
                    }

                    return _cachedNameFormatter;
                }
                else
                {
                    return EffectNameFormatter_None.Instance;
                }
            }
        }

        public readonly bool IsNetworked;

        public readonly string[] PreviousConfigSectionNames = Array.Empty<string>();

        public readonly ConfigFile ConfigFile;

        public ChaosEffectInfo(ChaosEffectIndex effectIndex, ChaosEffectAttribute attribute, ConfigFile configFile)
        {
            EffectIndex = effectIndex;
            Identifier = attribute.Identifier;

            NameToken = $"EFFECT_{Identifier.ToUpper()}_NAME";

            EffectType = attribute.target;

            EffectConfigBackwardsCompatibilityAttribute configBackwardsCompatibilityAttribute = EffectType.GetCustomAttribute<EffectConfigBackwardsCompatibilityAttribute>();
            if (configBackwardsCompatibilityAttribute != null)
            {
                PreviousConfigSectionNames = configBackwardsCompatibilityAttribute.ConfigSectionNames;
            }

            MethodInfo[] allMethods = EffectType.GetAllMethodsRecursive(BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public).ToArray();

            _canActivateMethods = allMethods.WithAttribute<MethodInfo, EffectCanActivateAttribute>()
                                            .Select(m => new ChaosEffectCanActivateMethod(m))
                                            .ToArray();

            _effectWeightMultipliers = allMethods.WithAttribute<MethodInfo, EffectWeightMultiplierSelectorAttribute>()
                                                 .Select(m => m.CreateDelegate<EffectWeightMultiplierDelegate>())
                                                 .ToArray();

            MethodInfo getEffectNameFormatArgsMethod = allMethods.WithAttribute<MethodInfo, GetEffectNameFormatterAttribute>().FirstOrDefault();
            _getEffectNameFormatter = getEffectNameFormatArgsMethod?.CreateDelegate<GetEffectNameFormatterDelegate>();

            Type[] incompatibleEffectTypes = EffectType.GetCustomAttributes<IncompatibleEffectsAttribute>(true)
                                                       .SelectMany(a => a.IncompatibleEffectTypes)
                                                       .Distinct()
                                                       .ToArray();

            if (incompatibleEffectTypes.Length > 0)
            {
                List<TimedEffectInfo> incompatibleEffects = new List<TimedEffectInfo>(incompatibleEffectTypes.Length);
                IncompatibleEffects = new ReadOnlyCollection<TimedEffectInfo>(incompatibleEffects);

                ChaosEffectCatalog.Availability.CallWhenAvailable(() =>
                {
                    incompatibleEffects.AddRange(ChaosEffectCatalog.AllTimedEffects.Where(e => e != this && incompatibleEffectTypes.Any(t => t.IsAssignableFrom(e.EffectType))));

#if DEBUG
                    Log.Debug($"Initialized incompatibility list for {ChaosEffectCatalog.GetEffectInfo(effectIndex)}: [{string.Join(", ", incompatibleEffects)}]");
#endif
                });
            }

            IsNetworked = attribute.IsNetworked;

            ConfigSectionName = "Effect: " + (attribute.ConfigName ?? Language.GetString(NameToken, "en")).FilterConfigKey();

            if (PreviousConfigSectionNames != null && PreviousConfigSectionNames.Length > 0)
            {
                int index = Array.IndexOf(PreviousConfigSectionNames, ConfigSectionName);
                if (index >= 0)
                {
                    ArrayUtils.ArrayRemoveAtAndResize(ref PreviousConfigSectionNames, index);
                }
            }

            ConfigFile = configFile;

            IsEnabledConfig = ConfigFactory<bool>.CreateConfig("Effect Enabled", true)
                                                 .Description("If the effect should be able to be picked")
                                                 .OptionConfig(new CheckBoxConfig())
                                                 .Build();

            _selectionWeightConfig = ConfigFactory<float>.CreateConfig("Effect Weight", attribute.DefaultSelectionWeight)
                                                         .Description("How likely the effect is to be picked, higher value means more likely, lower value means less likely")
                                                         .OptionConfig(new StepSliderConfig
                                                         {
                                                             formatString = "{0:F1}",
                                                             increment = 0.1f,
                                                             min = 0f,
                                                             max = 2.5f
                                                         })
                                                         .ValueConstrictor(CommonValueConstrictors.GreaterThanOrEqualTo(0f))
                                                         .Build();

            _activationShortcut =
                ConfigFactory<KeyboardShortcut>.CreateConfig("Activation Shortcut", KeyboardShortcut.Empty)
                                               .Description("A keyboard shortcut that, if pressed, will activate this effect at any time during a run")
                                               .OptionConfig(new KeyBindConfig())
                                               .Build();

            foreach (MemberInfo member in EffectType.GetMembers(BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.DeclaredOnly)
                                                    .WithAttribute<MemberInfo, InitEffectMemberAttribute>())
            {
                foreach (InitEffectMemberAttribute initEffectMember in member.GetCustomAttributes<InitEffectMemberAttribute>())
                {
                    if (initEffectMember.Priority == InitEffectMemberAttribute.InitializationPriority.EffectInfoCreation)
                    {
                        initEffectMember.ApplyTo(member, this);
                    }
                }
            }
        }

        internal virtual void Validate()
        {
            string displayName = GetLocalDisplayName(EffectNameFormatFlags.None);
            if (string.IsNullOrWhiteSpace(displayName))
            {
                Log.Error($"{this}: Null or empty display name");
            }

            if (Language.IsTokenInvalid(NameToken))
            {
                Log.Error($"{this}: Invalid name token");
            }

            if (Identifier.Any(char.IsUpper))
            {
                Log.Warning($"{this}: Effect identifier has uppercase characters");
            }
        }

        public virtual void BindConfigs()
        {
            IsEnabledConfig?.Bind(this);

            _selectionWeightConfig?.Bind(this);

            _activationShortcut?.Bind(this);
        }

        public virtual BaseEffect CreateInstance(in CreateEffectInstanceArgs args)
        {
            BaseEffect effectInstance = (BaseEffect)Activator.CreateInstance(EffectType);
            effectInstance.Initialize(args);
            return effectInstance;
        }

        public virtual bool IsEnabled()
        {
            if (!NetworkServer.active)
            {
                Log.Warning("Called on client");
                return false;
            }

            if (IsEnabledConfig != null && !IsEnabledConfig.Value)
            {
                return false;
            }

            return true;
        }

        public virtual bool CanActivate(in EffectCanActivateContext context)
        {
            if (!NetworkServer.active)
            {
                Log.Warning("Called on client");
                return false;
            }

            if (!IsEnabled())
            {
#if DEBUG
                Log.Debug($"effect {Identifier} cannot activate due to: Effect not enabled");
#endif
                return false;
            }

            if (_canActivateMethods.Length > 0)
            {
                foreach (ChaosEffectCanActivateMethod canActivateMethod in _canActivateMethods)
                {
                    if (!canActivateMethod.Invoke(context))
                        return false;
                }
            }

            if (!Configs.EffectSelection.SeededEffectSelection.Value && TimedChaosEffectHandler.Instance)
            {
                foreach (TimedEffectInfo incompatibleEffect in IncompatibleEffects)
                {
                    if (TimedChaosEffectHandler.Instance.AnyInstanceOfEffectActive(incompatibleEffect, context))
                    {
#if DEBUG
                        Log.Debug($"Effect {this} cannot activate: incompatible effect {incompatibleEffect} is active");
#endif

                        return false;
                    }
                }
            }

            return true;
        }

        public void MarkNameFormatterDirty()
        {
            NameFormatterDirty = true;
        }

        public string GetLocalDisplayName(EffectNameFormatFlags formatFlags = EffectNameFormatFlags.All)
        {
            return GetDisplayName(LocalDisplayNameFormatter, formatFlags);
        }

        public virtual string GetDisplayName(EffectNameFormatter formatter, EffectNameFormatFlags formatFlags = EffectNameFormatFlags.All)
        {
            string displayName = Language.GetString(NameToken);

            if ((formatFlags & EffectNameFormatFlags.RuntimeFormatArgs) != 0 && formatter is not null)
            {
                displayName = formatter.FormatEffectName(displayName);
            }

            return displayName;
        }

        public override string ToString()
        {
            return Identifier;
        }

        public override bool Equals(object obj)
        {
            return obj is ChaosEffectInfo effectInfo && Equals(effectInfo);
        }

        public bool Equals(ChaosEffectInfo other)
        {
            return other is not null && EffectIndex == other.EffectIndex;
        }

        public override int GetHashCode()
        {
            return -865576688 + EffectIndex.GetHashCode();
        }

        public static bool operator ==(ChaosEffectInfo left, ChaosEffectInfo right)
        {
            if (left is null || right is null)
                return left is null && right is null;

            return left.Equals(right);
        }

        public static bool operator !=(ChaosEffectInfo left, ChaosEffectInfo right)
        {
            return !(left == right);
        }
    }
}
