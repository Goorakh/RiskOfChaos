using HarmonyLib;
using HG;
using MonoMod.Utils;
using RiskOfChaos.ConfigHandling;
using RiskOfChaos.EffectHandling;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Data;
using RiskOfChaos.Patches;
using RiskOfOptions.OptionConfigs;
using RoR2;
using System;
using UnityEngine;

namespace RiskOfChaos.EffectDefinitions.World
{
    [ChaosTimedEffect("all_costs_health", TimedEffectType.UntilStageEnd, AllowDuplicates = false, DefaultSelectionWeight = 0.8f)]
    [EffectConfigBackwardsCompatibility("Effect: Blood Money (Lasts 1 stage)")]
    public sealed class AllCostsHealth : TimedEffect
    {
        [InitEffectInfo]
        static readonly TimedEffectInfo _effectInfo;

        static ConfigHolder<bool>[] _enabledConfigByCostType = Array.Empty<ConfigHolder<bool>>();

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

        public override void OnStart()
        {
            InstanceTracker.GetInstancesList<PurchaseInteraction>().Do(handlePurchaseInteraction);

            On.RoR2.PurchaseInteraction.Awake += PurchaseInteraction_Awake;

            CharacterMoneyChangedHook.OnCharacterMoneyChanged += onCharacterMoneyChanged;
        }

        public override void OnEnd()
        {
            On.RoR2.PurchaseInteraction.Awake -= PurchaseInteraction_Awake;

            CharacterMoneyChangedHook.OnCharacterMoneyChanged -= onCharacterMoneyChanged;
        }

        static float getCostTypeToPercentHealthConversionHalfwayValue(CostTypeIndex costType)
        {
            switch (costType)
            {
                case CostTypeIndex.Money:
                    return 150f;
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

        static int convertCostToHealthCost(int cost, float halfwayValue)
        {
            return Mathf.Max(1, Mathf.FloorToInt((1f - (halfwayValue / (cost + halfwayValue))) * 100f));
        }

        void onCharacterMoneyChanged(CharacterMaster master, int moneyDiff)
        {
            if (moneyDiff > 0 && master.playerCharacterMasterController)
            {
                CharacterBody body = master.GetBody();
                if (body)
                {
                    float healFraction = convertCostToHealthCost(moneyDiff, getCostTypeToPercentHealthConversionHalfwayValue(CostTypeIndex.Money)) / 100f;

#if DEBUG
                    Log.Debug($"Healing {Util.GetBestMasterName(master)} for {healFraction:P} health (+${moneyDiff})");
#endif

                    body.healthComponent.HealFraction(healFraction, new ProcChainMask());
                }
            }
        }

        static void PurchaseInteraction_Awake(On.RoR2.PurchaseInteraction.orig_Awake orig, PurchaseInteraction self)
        {
            orig(self);
            handlePurchaseInteraction(self);
        }

        static void handlePurchaseInteraction(PurchaseInteraction purchaseInteraction)
        {
            if (!purchaseInteraction || !canConvertCostType(purchaseInteraction.costType))
                return;

            int healthCost;
            if (purchaseInteraction.cost < 0)
            {
                healthCost = 0;
            }
            else
            {
                float halfwayCostValue = getCostTypeToPercentHealthConversionHalfwayValue(purchaseInteraction.costType);
                if (halfwayCostValue < 0f)
                    return;

                healthCost = convertCostToHealthCost(purchaseInteraction.cost, halfwayCostValue);
            }

            try
            {
                purchaseInteraction.costType = CostTypeIndex.PercentHealth;
                purchaseInteraction.Networkcost = healthCost;

                if (purchaseInteraction.TryGetComponent(out ShopTerminalBehavior shopTerminalBehavior) && shopTerminalBehavior.serverMultiShopController)
                {
                    shopTerminalBehavior.serverMultiShopController.costType = CostTypeIndex.PercentHealth;
                    shopTerminalBehavior.serverMultiShopController.Networkcost = healthCost;
                }
            }
            catch (Exception ex)
            {
                Log.Error_NoCallerPrefix($"Failed to convert {purchaseInteraction} ({purchaseInteraction.costType}) into health cost: {ex}");
            }
        }
    }
}
