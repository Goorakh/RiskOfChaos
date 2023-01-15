using BepInEx.Configuration;
using HG;
using RiskOfOptions;
using RiskOfOptions.Options;
using RoR2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace RiskOfChaos.EffectHandling
{
    public static class ChaosEffectCatalog
    {
        const string CONFIG_SECTION_NAME = "Effects";

        const string CONFIG_MOD_GUID = $"RoC_Config_{CONFIG_SECTION_NAME}";
        const string CONFIG_MOD_NAME = $"Risk of Chaos: {CONFIG_SECTION_NAME}";

        static ChaosEffectInfo[] _effects;

        static int _effectCount;
        public static int EffectCount => _effectCount;

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
                                                        .Select(static (e, i) => e.BuildEffectInfo(i))
                                                        .ToArray();

            _effectCount = _effects.Length;

            checkFindEffectIndex();

            foreach (ChaosEffectInfo effectInfo in _effects.OrderBy(ei => ei.ConfigSectionName, StringComparer.OrdinalIgnoreCase))
            {
                effectInfo.AddRiskOfOptionsEntries();
            }
        }

        static void checkFindEffectIndex()
        {
            for (uint effectIndex = 0; effectIndex < _effectCount; effectIndex++)
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
        public static ChaosEffectInfo GetEffectInfo(uint effectIndex)
        {
            return ArrayUtils.GetSafe(_effects, (int)effectIndex);
        }

        public static int FindEffectIndex(string identifier)
        {
            int index = Array.BinarySearch(_effects, identifier, ChaosEffectInfoIdentityComparer.Instance);

            if (index < 0)
            {
                Log.Warning($"unable to find effect index for identifier '{identifier}'");
            }

            return index;
        }

        internal static string GetConfigSectionName(string effectIdentifier)
        {
            int index = FindEffectIndex(effectIdentifier);
            if (index < 0)
            {
                Log.Error($"unable to find index for identifier {effectIdentifier}");
                return null;
            }

            return GetEffectInfo((uint)index).ConfigSectionName;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void AddEffectConfigOption(BaseOption option)
        {
            ModSettingsManager.AddOption(option, CONFIG_MOD_GUID, CONFIG_MOD_NAME);
        }

        public static WeightedSelection<ChaosEffectInfo> GetAllActivatableEffects()
        {
            WeightedSelection<ChaosEffectInfo> weightedSelection = new WeightedSelection<ChaosEffectInfo>(_effectCount);

            foreach (ChaosEffectInfo effect in _effects)
            {
                if (effect.CanActivate)
                {
                    weightedSelection.AddChoice(effect, effect.TotalSelectionWeight);
                }
            }

            return weightedSelection;
        }
    }
}
