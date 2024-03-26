using HG;
using MonoMod.Utils;
using RiskOfChaos.ConfigHandling;
using RiskOfChaos.EffectHandling;
using RiskOfChaos.EffectHandling.Controllers;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Data;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.ModifierController.Cost;
using RiskOfChaos.Patches;
using RiskOfOptions.OptionConfigs;
using RoR2;
using System;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectDefinitions.World
{
    [ChaosTimedEffect("all_costs_health", TimedEffectType.UntilStageEnd, AllowDuplicates = false, DefaultSelectionWeight = 0.8f)]
    [EffectConfigBackwardsCompatibility("Effect: Blood Money (Lasts 1 stage)")]
    public sealed class AllCostsHealth : TimedEffect, ICostModificationProvider
    {
        [InitEffectInfo]
        static readonly TimedEffectInfo _effectInfo;

        static ConfigHolder<bool>[] _enabledConfigByCostType = [];

        [SystemInitializer(typeof(CostTypeCatalog), typeof(ChaosEffectCatalog))]
        static void Init()
        {
            _enabledConfigByCostType = new ConfigHolder<bool>[CostTypeCatalog.costTypeCount];

            for (CostTypeIndex i = 0; i < (CostTypeIndex)CostTypeCatalog.costTypeCount; i++)
            {
                if (i == CostTypeIndex.None || i == CostTypeIndex.PercentHealth)
                    continue;

                CostTypeDef costTypeDef = CostTypeCatalog.GetCostTypeDef(i);
                if (costTypeDef is null)
                    continue;

                string key = i switch
                {
                    CostTypeIndex.VolatileBattery => "Fuel Array",
                    CostTypeIndex.ArtifactShellKillerItem => "Artifact Key",
                    CostTypeIndex.TreasureCacheItem => "Rusted Key",
                    CostTypeIndex.TreasureCacheVoidItem => "Encrusted Key",
                    _ => i.ToString().SpacedPascalCase()
                };

                bool defaultEnabled = i switch
                {
                    CostTypeIndex.Money or CostTypeIndex.LunarCoin or CostTypeIndex.VoidCoin => true,
                    _ => false
                };

                ConfigHolder<bool> costTypeEnabledConfig =
                    ConfigFactory<bool>.CreateConfig($"Convert {key} Costs", defaultEnabled)
                                       .Description($"If the effect should be able to turn {key} costs into health costs")
                                       .OptionConfig(new CheckBoxConfig())
                                       .OnValueChanged(() =>
                                       {
                                           if (!NetworkServer.active || !TimedChaosEffectHandler.Instance)
                                               return;

                                           TimedChaosEffectHandler.Instance.InvokeEventOnAllInstancesOfEffect<AllCostsHealth>(e => e.OnValueDirty);
                                       })
                                       .Build();

                costTypeEnabledConfig.Bind(_effectInfo);

                _enabledConfigByCostType[(int)i] = costTypeEnabledConfig;
            }
        }

        static bool canConvertCostType(CostTypeIndex costType)
        {
            ConfigHolder<bool> enabledConfig = ArrayUtils.GetSafe(_enabledConfigByCostType, (int)costType);
            return enabledConfig is not null && enabledConfig.Value;
        }

        [EffectCanActivate]
        static bool CanActivate()
        {
            return CostModificationManager.Instance;
        }

        public event Action OnValueDirty;

        public override void OnStart()
        {
            CostModificationManager.Instance.RegisterModificationProvider(this);

            CharacterMoneyChangedHook.OnCharacterMoneyChanged += onCharacterMoneyChanged;
        }

        public override void OnEnd()
        {
            if (CostModificationManager.Instance)
            {
                CostModificationManager.Instance.UnregisterModificationProvider(this);
            }

            CharacterMoneyChangedHook.OnCharacterMoneyChanged -= onCharacterMoneyChanged;
        }

        static float getCostTypeToPercentHealthConversionHalfwayValue(CostTypeIndex costType)
        {
            switch (costType)
            {
                case CostTypeIndex.Money:
                    return 50f;
                case CostTypeIndex.VoidCoin:
                case CostTypeIndex.LunarCoin:
                    return 2.5f;
                case CostTypeIndex.WhiteItem:
                    return 2f;
                case CostTypeIndex.GreenItem:
                    return 1f;
                case CostTypeIndex.RedItem:
                    return 0.5f;
                case CostTypeIndex.Equipment:
                case CostTypeIndex.VolatileBattery:
                case CostTypeIndex.LunarItemOrEquipment:
                case CostTypeIndex.ArtifactShellKillerItem:
                    return 3f;
                case CostTypeIndex.BossItem:
                    return 0.5f;
                case CostTypeIndex.TreasureCacheItem:
                case CostTypeIndex.TreasureCacheVoidItem:
                    return 3f;
                default:
                    return -1f;
            }
        }

        static float convertCostToHealthCost(float cost, float halfwayValue)
        {
            return Mathf.Max(1f, (1f - (halfwayValue / (cost + halfwayValue))) * 100f);
        }

        void onCharacterMoneyChanged(CharacterMaster master, long moneyDiff)
        {
            if (moneyDiff <= 0 || !master.playerCharacterMasterController)
                return;

            CharacterBody body = master.GetBody();
            if (!body)
                return;
            
            float healFraction = convertCostToHealthCost(moneyDiff, getCostTypeToPercentHealthConversionHalfwayValue(CostTypeIndex.Money)) / 100f;

            float difficultyCoefficient = Run.instance ? Run.instance.difficultyCoefficient : 1f;

            float healScale = moneyDiff / (5f * Mathf.Pow(difficultyCoefficient, 1.25f));
            healScale = -Mathf.Exp(-healScale) + 1f;
            healScale *= 0.5f;

            healFraction *= healScale;

            if (healFraction > 0.01f)
            {
#if DEBUG
                Log.Debug($"Healing {Util.GetBestMasterName(master)} for {healFraction:P} health (+${moneyDiff})");
#endif

                body.healthComponent.HealFraction(healFraction, new ProcChainMask());
            }
            else
            {
#if DEBUG
                Log.Debug($"Not healing {Util.GetBestMasterName(master)}, below threshold heal: {healFraction:P} (+${moneyDiff})");
#endif
            }
        }

        public void ModifyValue(ref CostModificationInfo value)
        {
            if (!canConvertCostType(value.CostType))
                return;

            float halfwayCostValue = getCostTypeToPercentHealthConversionHalfwayValue(value.CostType);
            if (halfwayCostValue < 0f)
                return;

            value.CostType = CostTypeIndex.PercentHealth;

            float baseCost = value.OriginalCostProvider.EstimatedBaseCost * value.CostMultiplier;
            float currentMultipliedCost = value.CurrentCost;

            if (baseCost > 0f && currentMultipliedCost > 0f)
            {
                float healthCost = convertCostToHealthCost(baseCost, halfwayCostValue);
                value.CostMultiplier = healthCost / value.CurrentCost;
            }
        }
    }
}
