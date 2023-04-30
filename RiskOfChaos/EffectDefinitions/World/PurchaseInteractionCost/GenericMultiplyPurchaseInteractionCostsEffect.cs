using RoR2;

namespace RiskOfChaos.EffectDefinitions.World.PurchaseInteractionCost
{
    public abstract class GenericMultiplyPurchaseInteractionCostsEffect : TimedEffect
    {
        protected abstract float multiplier { get; }

        public override void OnStart()
        {
            foreach (PurchaseInteraction purchaseInteraction in InstanceTracker.GetInstancesList<PurchaseInteraction>())
            {
                multiplyCost(purchaseInteraction);
            }

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
            purchaseInteraction.ScaleCost(multiplier);

            if (purchaseInteraction.TryGetComponent(out ShopTerminalBehavior shopTerminalBehavior) && shopTerminalBehavior.serverMultiShopController)
            {
                shopTerminalBehavior.serverMultiShopController.Networkcost = purchaseInteraction.cost;
            }
        }
    }
}
