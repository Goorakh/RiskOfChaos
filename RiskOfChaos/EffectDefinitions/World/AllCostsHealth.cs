using RiskOfChaos.EffectHandling;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
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
        }

        public override void OnEnd()
        {
            On.RoR2.PurchaseInteraction.Awake -= PurchaseInteraction_Awake;
        }

        static void PurchaseInteraction_Awake(On.RoR2.PurchaseInteraction.orig_Awake orig, PurchaseInteraction self)
        {
            orig(self);
            handlePurchaseInteraction(self);
        }

        static void handlePurchaseInteraction(PurchaseInteraction purchaseInteraction)
        {
            int autoHealthCost(float halfwayValue)
            {
                return Mathf.FloorToInt((1f - (halfwayValue / (purchaseInteraction.cost + halfwayValue))) * 100f);
            }

            int healthCost;
            switch (purchaseInteraction.costType)
            {
                case CostTypeIndex.Money:
                    healthCost = autoHealthCost(150f);
                    break;
                case CostTypeIndex.LunarCoin:
                case CostTypeIndex.VoidCoin:
                    healthCost = autoHealthCost(2.5f);
                    break;
                case CostTypeIndex.WhiteItem:
                    healthCost = autoHealthCost(2f);
                    break;
                case CostTypeIndex.GreenItem:
                    healthCost = autoHealthCost(1f);
                    break;
                case CostTypeIndex.RedItem:
                    healthCost = autoHealthCost(0.5f);
                    break;
                case CostTypeIndex.Equipment:
                case CostTypeIndex.VolatileBattery:
                case CostTypeIndex.LunarItemOrEquipment:
                    healthCost = autoHealthCost(3f);
                    break;
                case CostTypeIndex.BossItem:
                    healthCost = autoHealthCost(0.5f);
                    break;
                case CostTypeIndex.ArtifactShellKillerItem:
                    healthCost = autoHealthCost(3f);
                    break;
                case CostTypeIndex.TreasureCacheItem:
                case CostTypeIndex.TreasureCacheVoidItem:
                    healthCost = autoHealthCost(3f);
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
