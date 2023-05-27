using RiskOfChaos.EffectHandling;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.Patches;
using RoR2;
using UnityEngine;

namespace RiskOfChaos.EffectDefinitions.World
{
    [ChaosEffect("all_costs_health", DefaultSelectionWeight = 0.8f)]
    [ChaosTimedEffect(TimedEffectType.UntilStageEnd, AllowDuplicates = false)]
    [EffectConfigBackwardsCompatibility("Effect: Blood Money (Lasts 1 stage)")]
    public sealed class AllCostsHealth : TimedEffect
    {
        public override void OnStart()
        {
            foreach (PurchaseInteraction purchaseInteraction in InstanceTracker.GetInstancesList<PurchaseInteraction>())
            {
                handlePurchaseInteraction(purchaseInteraction);
            }

            On.RoR2.PurchaseInteraction.Awake += PurchaseInteraction_Awake;

            CharacterMoneyChangedHook.OnCharacterMoneyChanged += onCharacterMoneyChanged;
        }

        public override void OnEnd()
        {
            On.RoR2.PurchaseInteraction.Awake -= PurchaseInteraction_Awake;

            CharacterMoneyChangedHook.OnCharacterMoneyChanged -= onCharacterMoneyChanged;
        }

        const float MONEY_TO_HEALTH_HALFWAY_VALUE = 150f;

        static int convertCostToHealthCost(float halfwayValue, int cost)
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
                    float healFraction = convertCostToHealthCost(MONEY_TO_HEALTH_HALFWAY_VALUE, moneyDiff) / 100f;

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
            int healthCost;
            switch (purchaseInteraction.costType)
            {
                case CostTypeIndex.Money:
                    healthCost = convertCostToHealthCost(MONEY_TO_HEALTH_HALFWAY_VALUE, purchaseInteraction.cost);
                    break;
                case CostTypeIndex.LunarCoin:
                case CostTypeIndex.VoidCoin:
                    healthCost = convertCostToHealthCost(2.5f, purchaseInteraction.cost);
                    break;
                case CostTypeIndex.WhiteItem:
                    healthCost = convertCostToHealthCost(2f, purchaseInteraction.cost);
                    break;
                case CostTypeIndex.GreenItem:
                    healthCost = convertCostToHealthCost(1f, purchaseInteraction.cost);
                    break;
                case CostTypeIndex.RedItem:
                    healthCost = convertCostToHealthCost(0.5f, purchaseInteraction.cost);
                    break;
                case CostTypeIndex.Equipment:
                case CostTypeIndex.VolatileBattery:
                case CostTypeIndex.LunarItemOrEquipment:
                    healthCost = convertCostToHealthCost(3f, purchaseInteraction.cost);
                    break;
                case CostTypeIndex.BossItem:
                    healthCost = convertCostToHealthCost(0.5f, purchaseInteraction.cost);
                    break;
                case CostTypeIndex.ArtifactShellKillerItem:
                    healthCost = convertCostToHealthCost(3f, purchaseInteraction.cost);
                    break;
                case CostTypeIndex.TreasureCacheItem:
                case CostTypeIndex.TreasureCacheVoidItem:
                    healthCost = convertCostToHealthCost(3f, purchaseInteraction.cost);
                    break;
                default:
                    return;
            }

            purchaseInteraction.costType = CostTypeIndex.PercentHealth;
            purchaseInteraction.Networkcost = healthCost;

            if (purchaseInteraction.TryGetComponent(out ShopTerminalBehavior shopTerminalBehavior) && shopTerminalBehavior.serverMultiShopController)
            {
                shopTerminalBehavior.serverMultiShopController.costType = CostTypeIndex.PercentHealth;
                shopTerminalBehavior.serverMultiShopController.Networkcost = healthCost;
            }
        }
    }
}
