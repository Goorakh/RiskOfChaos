using HG;
using RiskOfChaos.EffectDefinitions;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Data;
using RiskOfChaos.Utilities.Extensions;
using RiskOfOptions;
using RiskOfOptions.Options;
using RoR2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectHandling
{
    public static class ChaosEffectCatalog
    {
        const string CONFIG_SECTION_NAME = "Effects";

        public const string CONFIG_MOD_GUID = $"RoC_Config_{CONFIG_SECTION_NAME}";
        public const string CONFIG_MOD_NAME = $"Risk of Chaos: {CONFIG_SECTION_NAME}";

        public static ResourceAvailability Availability = new ResourceAvailability();

        static ChaosEffectInfo[] _effects;
        public static ReadOnlyArray<ChaosEffectInfo> AllEffects { get; private set; }

        static int _effectCount;
        public static int EffectCount => _effectCount;

        static Dictionary<Type, ChaosEffectIndex> _effectTypeToIndexMap = new Dictionary<Type, ChaosEffectIndex>();

        static readonly WeightedSelection<ChaosEffectInfo> _pickNextEffectSelection = new WeightedSelection<ChaosEffectInfo>();

        public delegate void EffectDisplayNameModifier(in ChaosEffectInfo effectInfo, ref string displayName);
        public static event EffectDisplayNameModifier EffectDisplayNameModificationProvider;

        public delegate void OnEffectInstantiatedDelegate(in ChaosEffectInfo effectInfo, in CreateEffectInstanceArgs args, BaseEffect instance);
        public static event OnEffectInstantiatedDelegate OnEffectInstantiatedServer;

        public delegate void EffectCanActivateDelegate(in ChaosEffectInfo effectInfo, in EffectCanActivateContext context, ref bool canActivate);
        public static event EffectCanActivateDelegate OverrideEffectCanActivate;

        internal static void InitConfig()
        {
            // ModSettingsManager.SetModIcon(effects_icon, GUID, NAME);
            ModSettingsManager.SetModDescription("Effect config options for Risk of Chaos", CONFIG_MOD_GUID, CONFIG_MOD_NAME);
        }

        [SystemInitializer]
        static void Init()
        {
            _effects = HG.Reflection.SearchableAttribute.GetInstances<ChaosEffectAttribute>()
                                                        .Cast<ChaosEffectAttribute>()
                                                        .OrderBy(static e => e.Identifier, StringComparer.OrdinalIgnoreCase)
                                                        .Select(static (e, i) => e.BuildEffectInfo((ChaosEffectIndex)i))
                                                        .ToArray();

            AllEffects = new ReadOnlyArray<ChaosEffectInfo>(_effects);

            _effectCount = _effects.Length;

            _effectTypeToIndexMap = _effects.ToDictionary(e => e.EffectType, e => e.EffectIndex);

            _pickNextEffectSelection.Capacity = _effectCount;

            checkFindEffectIndex();

            foreach (ChaosEffectInfo effectInfo in _effects.OrderBy(ei => ei.ConfigSectionName, StringComparer.OrdinalIgnoreCase))
            {
                effectInfo.Validate();
                effectInfo.BindConfigs();
            }

            Log.Info($"Registered {_effectCount} effects");

            Availability.MakeAvailable();

            RoR2Application.onNextUpdate += () =>
            {
                foreach (ChaosEffectInfo effectInfo in _effects.OrderBy(ei => ei.ConfigSectionName, StringComparer.OrdinalIgnoreCase))
                {
                    foreach (MemberInfo member in effectInfo.EffectType.GetMembers(BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.DeclaredOnly).WithAttribute<MemberInfo, InitEffectMemberAttribute>())
                    {
                        foreach (InitEffectMemberAttribute initEffectMember in member.GetCustomAttributes<InitEffectMemberAttribute>())
                        {
                            if (initEffectMember.Priority == InitEffectMemberAttribute.InitializationPriority.EffectCatalogInitialized)
                            {
                                initEffectMember.ApplyTo(member, effectInfo);
                            }
                        }
                    }
                }
            };
        }

        static void checkFindEffectIndex()
        {
            for (ChaosEffectIndex effectIndex = 0; effectIndex < (ChaosEffectIndex)_effectCount; effectIndex++)
            {
                ChaosEffectInfo effectInfo = GetEffectInfo(effectIndex);

                if (FindEffectIndex(effectInfo.Identifier) != effectIndex)
                {
                    Log.Error($"Effect Find Test: {effectInfo.Identifier} failed case-sensitive check");
                }

                if (FindEffectIndex(effectInfo.Identifier.ToUpper()) != effectIndex)
                {
                    Log.Error($"Effect Find Test: {effectInfo.Identifier} failed case-insensitive check");
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T[] PerEffectArray<T>()
        {
            return new T[_effectCount];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ChaosEffectInfo GetEffectInfo(ChaosEffectIndex effectIndex)
        {
            return ArrayUtils.GetSafe(_effects, (int)effectIndex);
        }

        public static ChaosEffectIndex FindEffectIndex(string identifier)
        {
            int index = Array.BinarySearch(_effects, identifier, ChaosEffectInfoIdentityComparer.Instance);

            if (index < 0)
            {
                Log.Warning($"unable to find effect index for identifier '{identifier}'");
                return ChaosEffectIndex.Invalid;
            }

            return (ChaosEffectIndex)index;
        }

        public static ChaosEffectIndex FindEffectIndex(Type effectType)
        {
            if (_effectTypeToIndexMap.TryGetValue(effectType, out ChaosEffectIndex effectIndex))
            {
                return effectIndex;
            }
            else
            {
                return ChaosEffectIndex.Invalid;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void AddEffectConfigOption(BaseOption option)
        {
            ModSettingsManager.AddOption(option, CONFIG_MOD_GUID, CONFIG_MOD_NAME);
        }

        public static WeightedSelection<ChaosEffectInfo> GetAllActivatableEffects(in EffectCanActivateContext context, HashSet<ChaosEffectInfo> excludeEffects = null)
        {
            _pickNextEffectSelection.Clear();

            foreach (ChaosEffectInfo effect in _effects)
            {
                if (CanEffectActivate(effect, context) && (excludeEffects == null || !excludeEffects.Contains(effect)))
                {
                    _pickNextEffectSelection.AddChoice(effect, effect.TotalSelectionWeight);
                }
            }

            return _pickNextEffectSelection;
        }

        public static ChaosEffectInfo PickActivatableEffect(Xoroshiro128Plus rng, in EffectCanActivateContext context, HashSet<ChaosEffectInfo> excludeEffects = null)
        {
            WeightedSelection<ChaosEffectInfo> weightedSelection = GetAllActivatableEffects(context, excludeEffects);

            ChaosEffectInfo effect;
            if (weightedSelection.Count > 0)
            {
                float nextNormalizedFloat = rng.nextNormalizedFloat;
                effect = weightedSelection.Evaluate(nextNormalizedFloat);

#if DEBUG
                float effectWeight = weightedSelection.GetChoice(weightedSelection.EvaluateToChoiceIndex(nextNormalizedFloat)).weight;
                Log.Debug($"effect {effect.Identifier} selected, weight={effectWeight} ({weightedSelection.GetSelectionChance(effectWeight):P} chance)");
#endif
            }
            else
            {
                Log.Warning("No activatable effects, defaulting to Nothing");

                effect = Nothing.EffectInfo;
            }

            return effect;
        }

        public static string GetEffectDisplayName(in ChaosEffectInfo effectInfo)
        {
            string displayName = effectInfo.DisplayName;

            EffectDisplayNameModificationProvider?.Invoke(effectInfo, ref displayName);

            return displayName;
        }

        public static BaseEffect CreateEffectInstance(in ChaosEffectInfo effectInfo, in CreateEffectInstanceArgs args)
        {
            if (effectInfo.EffectType == null)
            {
                Log.Error($"Cannot instantiate effect {effectInfo}, {nameof(effectInfo.EffectType)} is null!");
                return null;
            }

            BaseEffect effectInstance = (BaseEffect)Activator.CreateInstance(effectInfo.EffectType);
            effectInstance.Initialize(args);

            if (NetworkServer.active)
            {
                OnEffectInstantiatedServer?.Invoke(effectInfo, args, effectInstance);
            }

            return effectInstance;
        }

        public static bool CanEffectActivate(in ChaosEffectInfo effectInfo, in EffectCanActivateContext context)
        {
            bool canActivate = effectInfo.CanActivate(context);
            OverrideEffectCanActivate?.Invoke(effectInfo, context, ref canActivate);
            return canActivate;
        }
    }
}
