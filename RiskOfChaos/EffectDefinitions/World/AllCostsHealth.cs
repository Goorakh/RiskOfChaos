using HarmonyLib;
using RiskOfChaos.EffectHandling;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.Patches;
using RoR2;
using System;
using UnityEngine;

namespace RiskOfChaos.EffectDefinitions.World
{
    [ChaosTimedEffect("all_costs_health", TimedEffectType.UntilStageEnd, AllowDuplicates = false, DefaultSelectionWeight = 0.8f)]
    [EffectConfigBackwardsCompatibility("Effect: Blood Money (Lasts 1 stage)")]
    public sealed class AllCostsHealth : TimedEffect
    {
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
            if (!purchaseInteraction)
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
