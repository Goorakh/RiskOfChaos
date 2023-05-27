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
                    float healFraction = convertCostToHealthCost(moneyDiff, MONEY_TO_HEALTH_HALFWAY_VALUE) / 100f;

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
            float halfwayCostValue = purchaseInteraction.costType switch
            {
                CostTypeIndex.Money => MONEY_TO_HEALTH_HALFWAY_VALUE,
                CostTypeIndex.LunarCoin or CostTypeIndex.VoidCoin => 2.5f,
                CostTypeIndex.WhiteItem => 2f,
                CostTypeIndex.GreenItem => 1f,
                CostTypeIndex.RedItem => 0.5f,
                CostTypeIndex.Equipment or CostTypeIndex.VolatileBattery or CostTypeIndex.LunarItemOrEquipment => 3f,
                CostTypeIndex.BossItem => 0.5f,
                CostTypeIndex.ArtifactShellKillerItem => 3f,
                CostTypeIndex.TreasureCacheItem or CostTypeIndex.TreasureCacheVoidItem => 3f,
                _ => -1f,
            };

            if (halfwayCostValue < 0f)
                return;

            int healthCost = convertCostToHealthCost(purchaseInteraction.cost, halfwayCostValue);

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
