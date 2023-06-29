using HarmonyLib;
using RoR2;
using System;

namespace RiskOfChaos.EffectDefinitions.World.PurchaseInteractionCost
{
    public abstract class GenericMultiplyPurchaseInteractionCostsEffect : TimedEffect
    {
        protected abstract float multiplier { get; }

        public override void OnStart()
        {
            InstanceTracker.GetInstancesList<PurchaseInteraction>().Do(multiplyCost);

            On.RoR2.PurchaseInteraction.Awake += PurchaseInteraction_Awake;
        }

        public override void OnEnd()
        {
            On.RoR2.PurchaseInteraction.Awake -= PurchaseInteraction_Awake;
        }

        void PurchaseInteraction_Awake(On.RoR2.PurchaseInteraction.orig_Awake orig, PurchaseInteraction self)
        {
            orig(self);
            multiplyCost(self);
        }

        void multiplyCost(PurchaseInteraction purchaseInteraction)
        {
            try
            {
                purchaseInteraction.ScaleCost(multiplier);

                if (purchaseInteraction.TryGetComponent(out ShopTerminalBehavior shopTerminalBehavior) && shopTerminalBehavior.serverMultiShopController)
                {
                    shopTerminalBehavior.serverMultiShopController.Networkcost = purchaseInteraction.cost;
                }
            }
            catch (Exception ex)
            {
                Log.Error_NoCallerPrefix($"Failed to multiply interactable cost of {purchaseInteraction}: {ex}");
            }
        }
    }
}
