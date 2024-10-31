using BepInEx.Configuration;
using HG;
using MonoMod.Utils;
using RiskOfChaos.Collections;
using RiskOfChaos.Components;
using RiskOfChaos.ConfigHandling;
using RiskOfChaos.ConfigHandling.AcceptableValues;
using RiskOfChaos.Content;
using RiskOfChaos.EffectHandling.Controllers;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Data;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.EffectHandling.EffectComponents;
using RiskOfChaos.EffectHandling.Formatting;
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

        public readonly Type EffectComponentType;

        public readonly string ConfigSectionName;

        public readonly GameObject ControllerPrefab;

        public readonly ReadOnlyCollection<Type> ControllerComponentTypes;

        readonly ChaosEffectCanActivateMethod[] _canActivateMethods = [];

        public readonly ReadOnlyCollection<TimedEffectInfo> IncompatibleEffects = Empty<TimedEffectInfo>.ReadOnlyCollection;

        public readonly ConfigHolder<bool> IsEnabledConfig;
        readonly ConfigHolder<float> _selectionWeightConfig;

        readonly ConfigHolder<KeyboardShortcut> _activationShortcut;
        public bool IsActivationShortcutPressed => _activationShortcut != null && _activationShortcut.Value.IsDownIgnoringBlockerKeys();

        readonly EffectWeightMultiplierDelegate[] _effectWeightMultipliers = [];
        public float TotalSelectionWeight
        {
            get
            {
                float weight = _selectionWeightConfig.Value;

                // For seeded selection to be deterministic, effect weights have to stay constant, so no variable weights allowed in this mode
                if (!Configs.EffectSelection.SeededEffectSelection.Value)
                {
                    foreach (EffectWeightMultiplierDelegate getEffectWeightMultiplier in _effectWeightMultipliers)
                    {
                        weight *= getEffectWeightMultiplier();
                    }
                }

                return weight;
            }
        }

        readonly GetEffectNameFormatterDelegate _getEffectNameFormatter;

        EffectNameFormatter _cachedNameFormatter;

        public static event Action<ChaosEffectInfo> OnEffectNameFormatterDirty;

        bool _nameFormatterDirty;
        bool nameFormatterDirty
        {
            get
            {
                return _nameFormatterDirty;
            }
            set
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
                    if (_cachedNameFormatter is null || nameFormatterDirty)
                    {
                        _cachedNameFormatter = _getEffectNameFormatter();
                        nameFormatterDirty = false;
                    }

                    return _cachedNameFormatter;
                }
                else
                {
                    return EffectNameFormatter_None.Instance;
                }
            }
        }

        public readonly string[] PreviousConfigSectionNames = [];

        public readonly ConfigFile ConfigFile;

        public ChaosEffectInfo(ChaosEffectIndex effectIndex, ChaosEffectAttribute attribute, ConfigFile configFile)
        {
            EffectIndex = effectIndex;
            Identifier = attribute.Identifier;

            NameToken = $"EFFECT_{Identifier.ToUpper()}_NAME";

            EffectComponentType = attribute.target;

            EffectConfigBackwardsCompatibilityAttribute configBackwardsCompatibilityAttribute = EffectComponentType.GetCustomAttribute<EffectConfigBackwardsCompatibilityAttribute>();
            if (configBackwardsCompatibilityAttribute != null)
            {
                PreviousConfigSectionNames = configBackwardsCompatibilityAttribute.ConfigSectionNames;
            }

            ControllerPrefab = createPrefab();

            MonoBehaviour[] controllerComponents = ControllerPrefab.GetComponents<MonoBehaviour>();
            HashSet<Type> controllerComponentTypes = new HashSet<Type>(controllerComponents.Length);
            foreach (MonoBehaviour controllerComponent in controllerComponents)
            {
                controllerComponentTypes.Add(controllerComponent.GetType());
            }

            ControllerComponentTypes = new ReadOnlyCollection<Type>(controllerComponentTypes.ToList());

            HashSet<MethodInfo> processedMethods = [];

            List<ChaosEffectCanActivateMethod> canActivateMethodsList = [];
            List<EffectWeightMultiplierDelegate> effectWeightMultipliersList = [];
            GetEffectNameFormatterDelegate getEffectNameFormatterDelegate = null;
            HashSet<Type> incompatibleEffectTypes = [];

            foreach (Type type in ControllerComponentTypes)
            {
                foreach (IncompatibleEffectsAttribute incompatibleEffectsAttribute in type.GetCustomAttributes<IncompatibleEffectsAttribute>(true))
                {
                    incompatibleEffectTypes.UnionWith(incompatibleEffectsAttribute.IncompatibleEffectTypes);
                }

                foreach (MethodInfo method in type.GetAllMethodsRecursive(BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public))
                {
                    if (!processedMethods.Add(method))
                        continue;

                    if (method.GetCustomAttribute<EffectCanActivateAttribute>() != null)
                    {
                        canActivateMethodsList.Add(new ChaosEffectCanActivateMethod(method));
                    }
                    else if (method.GetCustomAttribute<EffectWeightMultiplierSelectorAttribute>() != null)
                    {
                        effectWeightMultipliersList.Add(method.CreateDelegate<EffectWeightMultiplierDelegate>());
                    }
                    else if (getEffectNameFormatterDelegate == null && method.GetCustomAttribute<GetEffectNameFormatterAttribute>() != null)
                    {
                        getEffectNameFormatterDelegate = method.CreateDelegate<GetEffectNameFormatterDelegate>();
                    }
                }
            }

            _canActivateMethods = canActivateMethodsList.ToArray();
            _effectWeightMultipliers = effectWeightMultipliersList.ToArray();
            _getEffectNameFormatter = getEffectNameFormatterDelegate;

            if (incompatibleEffectTypes.Count > 0)
            {
                List<TimedEffectInfo> incompatibleEffects = new List<TimedEffectInfo>(incompatibleEffectTypes.Count);
                IncompatibleEffects = new ReadOnlyCollection<TimedEffectInfo>(incompatibleEffects);

                ChaosEffectCatalog.Availability.CallWhenAvailable(() =>
                {
                    foreach (TimedEffectInfo timedEffect in ChaosEffectCatalog.AllTimedEffects)
                    {
                        if (timedEffect == this)
                            continue;

                        foreach (Type componentType in timedEffect.ControllerComponentTypes)
                        {
                            if (incompatibleEffectTypes.Any(t => t.IsAssignableFrom(componentType)))
                            {
                                incompatibleEffects.Add(timedEffect);
                                break;
                            }
                        }
                    }

#if DEBUG
                    Log.Debug($"Initialized incompatibility list for {this}: [{string.Join(", ", incompatibleEffects)}]");
#endif
                });
            }

            ConfigSectionName = "Effect: " + (attribute.ConfigName ?? Language.GetString(NameToken, "en").FilterConfigKey());

            if (PreviousConfigSectionNames != null && PreviousConfigSectionNames.Length > 0)
            {
                int index = Array.IndexOf(PreviousConfigSectionNames, ConfigSectionName);
                if (index >= 0)
                {
                    ArrayUtils.ArrayRemoveAtAndResize(ref PreviousConfigSectionNames, index);
                }
            }

            ConfigFile = configFile;

            IsEnabledConfig = 
                ConfigFactory<bool>.CreateConfig("Effect Enabled", true)
                                   .Description("If the effect should be able to be picked")
                                   .OptionConfig(new CheckBoxConfig())
                                   .Build();

            _selectionWeightConfig = 
                ConfigFactory<float>.CreateConfig("Effect Weight", attribute.DefaultSelectionWeight)
                                    .Description("How likely the effect is to be picked, higher value means more likely, lower value means less likely")
                                    .AcceptableValues(new AcceptableValueMin<float>(0f))
                                    .OptionConfig(new FloatFieldConfig { Min = 0f })
                                    .Build();

            _activationShortcut =
                ConfigFactory<KeyboardShortcut>.CreateConfig("Activation Shortcut", KeyboardShortcut.Empty)
                                               .Description("A keyboard shortcut that, if pressed, will activate this effect at any time during a run")
                                               .OptionConfig(new KeyBindConfig())
                                               .Build();

            foreach (MemberInfo member in EffectComponentType.GetMembers(BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.DeclaredOnly)
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

        protected virtual void modifyPrefabComponents(List<Type> componentTypes)
        {
        }

        protected virtual GameObject createPrefab()
        {
            List<Type> componentTypes = [
                typeof(NetworkIdentity),
                typeof(ChaosEffectTimeoutController),
                typeof(ChaosEffectComponent),
                EffectComponentType
            ];

            modifyPrefabComponents(componentTypes);

            GameObject prefab = Prefabs.CreateNetworkedPrefab($"{Identifier}_EffectController", componentTypes.ToArray());

            if (prefab.TryGetComponent(out ChaosEffectComponent effectComponent))
            {
                effectComponent.ChaosEffectIndex = EffectIndex;
            }

            return prefab;
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

            if (!context.IsShortcut && !IsEnabled())
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

            if (!Configs.EffectSelection.SeededEffectSelection.Value && ChaosEffectTracker.Instance)
            {
                foreach (TimedEffectInfo incompatibleEffect in IncompatibleEffects)
                {
                    if (ChaosEffectTracker.Instance.IsAnyInstanceOfTimedEffectRelevantForContext(incompatibleEffect, context))
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
            nameFormatterDirty = true;
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
            if (left is null)
                return right is null;

            return left.Equals(right);
        }

        public static bool operator !=(ChaosEffectInfo left, ChaosEffectInfo right)
        {
            return !(left == right);
        }
    }
}
