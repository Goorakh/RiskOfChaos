using BepInEx.Configuration;
using HG;
using RiskOfChaos.Content;
using RiskOfChaos.Content.AssetCollections;
using RiskOfChaos.EffectDefinitions;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Data;
using RiskOfChaos.EffectHandling.Formatting;
using RiskOfChaos.Utilities.Extensions;
using RiskOfOptions;
using RoR2;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace RiskOfChaos.EffectHandling
{
    public static class ChaosEffectCatalog
    {
        const string CONFIG_SECTION_NAME = "Effects";

        public const string CONFIG_MOD_GUID = $"RoC_Config_{CONFIG_SECTION_NAME}";
        public const string CONFIG_MOD_NAME = $"Risk of Chaos: {CONFIG_SECTION_NAME}";

        static ConfigFile _effectConfigFile;
        static Sprite effectsConfigIcon => Configs.GenericIcon;

        public static ResourceAvailability Availability = new ResourceAvailability();

        static ChaosEffectInfo[] _effects;
        public static ReadOnlyArray<ChaosEffectInfo> AllEffects { get; private set; }

        public static ReadOnlyArray<TimedEffectInfo> AllTimedEffects { get; private set; }

        static readonly Dictionary<string, ChaosEffectIndex> _effectIndexByNameToken = [];
        static readonly Dictionary<Type, ChaosEffectIndex> _effectIndexByType = [];

        public static int EffectCount => _effects.Length;

        static readonly WeightedSelection<ChaosEffectInfo> _pickNextEffectSelection = new WeightedSelection<ChaosEffectInfo>();

        internal static void InitConfig(ConfigFile config)
        {
            _effectConfigFile = config;

            if (effectsConfigIcon)
            {
                ModSettingsManager.SetModIcon(effectsConfigIcon, CONFIG_MOD_GUID, CONFIG_MOD_NAME);
            }

            ModSettingsManager.SetModDescription("Effect config options for Risk of Chaos", CONFIG_MOD_GUID, CONFIG_MOD_NAME);
        }

        [ContentInitializer]
        static IEnumerator LoadContent(NetworkedPrefabAssetCollection networkedPrefabs)
        {
            _effects = HG.Reflection.SearchableAttribute.GetInstances<ChaosEffectAttribute>()
                                                        .Cast<ChaosEffectAttribute>()
                                                        .Where(attr => attr.Validate())
                                                        .OrderBy(e => e.Identifier, StringComparer.OrdinalIgnoreCase)
                                                        .Select((e, i) => e.BuildEffectInfo((ChaosEffectIndex)i, _effectConfigFile))
                                                        .ToArray();

            for (int i = 0; i < _effects.Length; i++)
            {
                ChaosEffectInfo effect = _effects[i];
                if (!networkedPrefabs.Contains(effect.ControllerPrefab))
                {
                    networkedPrefabs.Add(effect.ControllerPrefab);
                }

                if (i > 0 && i % 25 == 0)
                {
                    yield return null;
                }
            }
        }

        [SystemInitializer]
        static void Init()
        {
            Stopwatch stopwatch = Stopwatch.StartNew();

            AllEffects = new ReadOnlyArray<ChaosEffectInfo>(_effects);

            AllTimedEffects = new ReadOnlyArray<TimedEffectInfo>([.. _effects.OfType<TimedEffectInfo>()]);

            foreach (ChaosEffectInfo effect in _effects)
            {
                if (_effectIndexByNameToken.ContainsKey(effect.NameToken))
                {
                    Log.Error($"Duplicate effect name token: {effect.NameToken}");
                }
                else
                {
                    _effectIndexByNameToken.Add(effect.NameToken, effect.EffectIndex);
                }

                if (_effectIndexByType.ContainsKey(effect.EffectComponentType))
                {
                    Log.Error($"Duplicate effect type: {effect.EffectComponentType}");
                }
                else
                {
                    _effectIndexByType.Add(effect.EffectComponentType, effect.EffectIndex);
                }
            }

            _pickNextEffectSelection.EnsureCapacity(EffectCount);

            checkFindEffectIndex();

            ChaosEffectInfo[] effectsByConfigName = new ChaosEffectInfo[EffectCount];
            _effects.CopyTo(effectsByConfigName, 0);
            Array.Sort(effectsByConfigName, (a, b) => StringComparer.OrdinalIgnoreCase.Compare(a.ConfigSectionName, b.ConfigSectionName));

            foreach (ChaosEffectInfo effectInfo in effectsByConfigName)
            {
                effectInfo.Validate();
                effectInfo.BindConfigs();
            }

            Log.Info($"Registered {EffectCount} effects");

            foreach (ChaosEffectInfo effectInfo in effectsByConfigName)
            {
                foreach (MemberInfo member in effectInfo.EffectComponentType.GetMembers(BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.DeclaredOnly).WithAttribute<MemberInfo, InitEffectMemberAttribute>())
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

            Dictionary<ChaosEffectIndex, HashSet<ChaosEffectIndex>> incompatibleEffectsDict = new Dictionary<ChaosEffectIndex, HashSet<ChaosEffectIndex>>(EffectCount);

            for (ChaosEffectIndex effectIndex = 0; effectIndex < (ChaosEffectIndex)EffectCount; effectIndex++)
            {
                ChaosEffectInfo effectInfo = GetEffectInfo(effectIndex);

                if (effectInfo.IncompatibleEffectComponentTypes.Count <= 0)
                    continue;

                HashSet<ChaosEffectIndex> incompatibleEffects = incompatibleEffectsDict.GetOrAddNew(effectIndex);
                incompatibleEffects.EnsureCapacity(effectInfo.IncompatibleEffectComponentTypes.Count);

                foreach (TimedEffectInfo timedEffect in AllTimedEffects)
                {
                    if (timedEffect == effectInfo)
                        continue;

                    if (incompatibleEffects.Contains(timedEffect.EffectIndex))
                        continue;

                    foreach (Type componentType in timedEffect.ControllerComponentTypes)
                    {
                        if (effectInfo.IncompatibleEffectComponentTypes.Any(t => t.IsAssignableFrom(componentType)))
                        {
                            incompatibleEffects.Add(timedEffect.EffectIndex);
                            break;
                        }
                    }
                }

                foreach (ChaosEffectIndex incompatibleEffectIndex in incompatibleEffects)
                {
                    HashSet<ChaosEffectIndex> otherIncompatibleEffects = incompatibleEffectsDict.GetOrAddNew(incompatibleEffectIndex);
                    otherIncompatibleEffects.Add(effectIndex);
                }

                incompatibleEffectsDict[effectIndex] = incompatibleEffects;
            }

            foreach (KeyValuePair<ChaosEffectIndex, HashSet<ChaosEffectIndex>> kvp in incompatibleEffectsDict)
            {
                kvp.Deconstruct(out ChaosEffectIndex effectIndex, out HashSet<ChaosEffectIndex> incompatibleEffects);

                if (incompatibleEffects.Count > 0)
                {
                    ChaosEffectInfo effectInfo = GetEffectInfo(effectIndex);
                    effectInfo.SetIncompatibleEffects([.. incompatibleEffects]);

                    Log.Debug($"Initialized incompatibility list for {effectInfo}: [{string.Join(", ", incompatibleEffects.Select(GetEffectInfo))}]");
                }
            }

            Availability.MakeAvailable();

            stopwatch.Stop();
            Log.Info($"Effect catalog initialized in {stopwatch.Elapsed.TotalSeconds:F1} seconds");
        }

        static void checkFindEffectIndex()
        {
            for (ChaosEffectIndex effectIndex = 0; effectIndex < (ChaosEffectIndex)EffectCount; effectIndex++)
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
        public static ChaosEffectInfo GetEffectInfo(ChaosEffectIndex effectIndex)
        {
            return ArrayUtils.GetSafe(_effects, (int)effectIndex);
        }

        public static EffectNameFormatter GetEffectStaticNameFormatter(ChaosEffectIndex effectIndex)
        {
            ChaosEffectInfo effectInfo = GetEffectInfo(effectIndex);
            return effectInfo?.StaticDisplayNameFormatterProvider?.NameFormatter ?? EffectNameFormatter_None.Instance;
        }

        public static ChaosEffectIndex FindEffectIndex(string identifier)
        {
            int index = Array.BinarySearch(_effects, identifier, ChaosEffectInfoIdentifierComparer.Instance);

            if (index < 0)
            {
                Log.Warning($"unable to find effect index for identifier '{identifier}'");
                return ChaosEffectIndex.Invalid;
            }

            return (ChaosEffectIndex)index;
        }

        public static ChaosEffectIndex FindEffectIndexByNameToken(string token)
        {
            if (_effectIndexByNameToken.TryGetValue(token, out ChaosEffectIndex effectIndex))
                return effectIndex;

            return ChaosEffectIndex.Invalid;
        }

        public static ChaosEffectIndex FindEffectIndexByComponentType(Type type)
        {
            if (_effectIndexByType.TryGetValue(type, out ChaosEffectIndex effectIndex))
            {
                return effectIndex;
            }
            else
            {
                Log.Error($"{type} is not an effect type");
                return ChaosEffectIndex.Invalid;
            }
        }

        public static ChaosEffectInfo FindEffectInfoByComponentType(Type type)
        {
            ChaosEffectIndex effectIndex = FindEffectIndexByComponentType(type);
            if (effectIndex != ChaosEffectIndex.Invalid)
            {
                return GetEffectInfo(effectIndex);
            }
            else
            {
                return null;
            }
        }

        public static bool IsEffectRelatedToken(string token)
        {
            switch (token)
            {
                case "CHAOS_ACTIVE_EFFECTS_BAR_TITLE":
                case "CHAOS_ACTIVE_EFFECT_UNTIL_STAGE_END_SINGLE_FORMAT":
                case "CHAOS_ACTIVE_EFFECT_UNTIL_STAGE_END_MULTI_FORMAT":
                case "CHAOS_ACTIVE_EFFECT_FIXED_DURATION_FORMAT":
                case "CHAOS_ACTIVE_EFFECT_FIXED_DURATION_LONG_FORMAT":
                case "CHAOS_ACTIVE_EFFECT_FALLBACK_FORMAT":
                case "CHAOS_NEXT_EFFECT_DISPLAY_FORMAT":
                case "CHAOS_NEXT_EFFECT_TIME_REMAINING_DISPLAY_FORMAT":
                case "CHAOS_EFFECT_VOTING_OPTION_FORMAT":
                case "CHAOS_EFFECT_ACTIVATE":
                case "CHAOS_EFFECT_VOTING_RANDOM_OPTION_NAME":
                case "TIMED_TYPE_UNTIL_STAGE_END_SINGLE_FORMAT":
                case "TIMED_TYPE_UNTIL_STAGE_END_MULTI_FORMAT":
                case "TIMED_TYPE_FIXED_DURATION_FORMAT":
                case "TIMED_TYPE_PERMANENT_FORMAT":
                    return true;
            }

            if (FindEffectIndexByNameToken(token) != ChaosEffectIndex.Invalid)
                return true;

            return false;

        }

        public static WeightedSelection<ChaosEffectInfo> GetAllEnabledEffects(HashSet<ChaosEffectInfo> excludeEffects = null)
        {
            _pickNextEffectSelection.Clear();

            foreach (ChaosEffectInfo effect in _effects)
            {
                if (effect.IsEnabled() && (excludeEffects == null || !excludeEffects.Contains(effect)))
                {
                    _pickNextEffectSelection.AddChoice(effect, effect.TotalSelectionWeight);
                }
            }

            return _pickNextEffectSelection;
        }

        public static ChaosEffectInfo PickEnabledEffect(Xoroshiro128Plus rng, HashSet<ChaosEffectInfo> excludeEffects = null)
        {
            return pickEffectFromSelection(rng, GetAllEnabledEffects(excludeEffects));
        }

        public static WeightedSelection<ChaosEffectInfo> GetAllActivatableEffects(in EffectCanActivateContext context, HashSet<ChaosEffectInfo> excludeEffects = null)
        {
            _pickNextEffectSelection.Clear();

            foreach (ChaosEffectInfo effect in _effects)
            {
                if (effect.CanActivate(context) && (excludeEffects == null || !excludeEffects.Contains(effect)))
                {
                    _pickNextEffectSelection.AddChoice(effect, effect.TotalSelectionWeight);
                }
            }

            return _pickNextEffectSelection;
        }

        public static ChaosEffectInfo PickActivatableEffect(Xoroshiro128Plus rng, in EffectCanActivateContext context, HashSet<ChaosEffectInfo> excludeEffects = null)
        {
            return pickEffectFromSelection(rng, GetAllActivatableEffects(context, excludeEffects));
        }

        static ChaosEffectInfo pickEffectFromSelection(Xoroshiro128Plus rng, WeightedSelection<ChaosEffectInfo> weightedSelection)
        {
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
                Log.Error($"No activatable effects, defaulting to Nothing\n{new StackTrace()}");

                effect = Nothing.EffectInfo;
            }

            return effect;
        }
    }
}
