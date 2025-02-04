using BepInEx.Configuration;
using HarmonyLib;
using HG;
using MonoMod.Utils;
using RiskOfChaos.Collections;
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

        public readonly ReadOnlyCollection<Type> IncompatibleEffectComponentTypes = Empty<Type>.ReadOnlyCollection;

        public ReadOnlyArray<ChaosEffectIndex> IncompatibleEffects { get; private set; } = new ReadOnlyArray<ChaosEffectIndex>([]);

        public readonly ConfigHolder<bool> IsEnabledConfig;
        readonly ConfigHolder<float> _selectionWeightConfig;

        public readonly bool EnabledInSingleplayer;
        public readonly bool EnabledInMultiplayer;

        readonly ConfigHolder<KeyboardShortcut> _activationShortcut;
        public bool IsActivationShortcutPressed => _activationShortcut != null && _activationShortcut.Value.IsDownIgnoringBlockerKeys();

        readonly EffectWeightMultiplierDelegate[] _effectWeightMultipliers = [];

        readonly GetEffectNameFormatterDelegate _getStaticEffectNameFormatter;

        public readonly EffectNameFormatterProvider StaticDisplayNameFormatterProvider;

        public readonly string[] PreviousConfigSectionNames = [];

        public readonly ConfigFile ConfigFile;

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

        public ChaosEffectInfo(ChaosEffectIndex effectIndex, ChaosEffectAttribute attribute, ConfigFile configFile)
        {
            EffectIndex = effectIndex;
            Identifier = attribute.Identifier;

            NameToken = $"EFFECT_{Identifier.ToUpper()}_NAME";

            EffectComponentType = attribute.target;

            EnabledInSingleplayer = attribute.EnabledInSingleplayer;
            EnabledInMultiplayer = attribute.EnabledInMultiplayer;

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

            ControllerComponentTypes = new ReadOnlyCollection<Type>([.. controllerComponentTypes]);

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
                    else if (method.GetCustomAttribute<GetEffectNameFormatterAttribute>() != null)
                    {
                        if (getEffectNameFormatterDelegate != null)
                        {
                            Log.Warning($"Duplicate name formatter getter for effect {Identifier}: {getEffectNameFormatterDelegate.Method.FullDescription()}, {method.FullDescription()}");
                        }

                        getEffectNameFormatterDelegate = method.CreateDelegate<GetEffectNameFormatterDelegate>();
                    }
                }
            }

            _canActivateMethods = [.. canActivateMethodsList];
            _effectWeightMultipliers = [.. effectWeightMultipliersList];
            _getStaticEffectNameFormatter = getEffectNameFormatterDelegate;

            StaticDisplayNameFormatterProvider = new EffectNameFormatterProvider(GetDefaultNameFormatter(), true);

            IncompatibleEffectComponentTypes = new ReadOnlyCollection<Type>(incompatibleEffectTypes.ToArray());

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
                typeof(ChaosEffectComponent),
                typeof(ChaosEffectNameComponent),
            ];

            modifyPrefabComponents(componentTypes);

            componentTypes.Add(EffectComponentType);

            GameObject prefab = Prefabs.CreateNetworkedPrefab($"{Identifier}_EffectController", [.. componentTypes]);

            if (prefab.TryGetComponent(out ChaosEffectComponent effectComponent))
            {
                effectComponent.ChaosEffectIndex = EffectIndex;
            }

            return prefab;
        }

        internal virtual void Validate()
        {
            string displayName = GetStaticDisplayName(EffectNameFormatFlags.None);
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

        public void SetIncompatibleEffects(ChaosEffectIndex[] incompatibleEffectIndices)
        {
            ChaosEffectIndex[] incompatibleEffects = ArrayUtils.Clone(incompatibleEffectIndices);
            Array.Sort(incompatibleEffects);
            IncompatibleEffects = incompatibleEffects;
        }

        public bool IsIncompatibleWith(ChaosEffectIndex otherEffectIndex)
        {
            return ReadOnlyArray<ChaosEffectIndex>.BinarySearch(IncompatibleEffects, otherEffectIndex) >= 0;
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
                return false;

            if (!EnabledInSingleplayer && RoR2Application.isInSinglePlayer)
                return false;
            
            if (!EnabledInMultiplayer && RoR2Application.isInMultiPlayer)
                return false;

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
                Log.Debug($"effect {Identifier} cannot activate due to: Effect not enabled");
                return false;
            }

            foreach (ChaosEffectCanActivateMethod canActivateMethod in _canActivateMethods)
            {
                if (!canActivateMethod.Invoke(context))
                {
                    return false;
                }
            }

            if (!Configs.EffectSelection.SeededEffectSelection.Value && ChaosEffectTracker.Instance)
            {
                for (int i = 0; i < IncompatibleEffects.Length; i++)
                {
                    ChaosEffectIndex incompatibleEffect = IncompatibleEffects[i];
                    if (ChaosEffectTracker.Instance.IsAnyInstanceOfTimedEffectRelevantForContext(incompatibleEffect, context))
                    {
                        Log.Debug($"Effect {this} cannot activate: incompatible effect {ChaosEffectCatalog.GetEffectInfo(incompatibleEffect)} is active");

                        return false;
                    }
                }
            }

            return true;
        }

        public EffectNameFormatter GetDefaultNameFormatter()
        {
            EffectNameFormatter nameFormatter = null;
            if (_getStaticEffectNameFormatter != null)
            {
                nameFormatter = _getStaticEffectNameFormatter();
            }

            return nameFormatter ?? EffectNameFormatter_None.Instance;
        }

        public void RestoreStaticDisplayNameFormatter()
        {
            StaticDisplayNameFormatterProvider.NameFormatter = GetDefaultNameFormatter();
        }

        public string GetStaticDisplayName(EffectNameFormatFlags formatFlags = EffectNameFormatFlags.All)
        {
            return StaticDisplayNameFormatterProvider.NameFormatter.GetEffectDisplayName(this, formatFlags);
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
