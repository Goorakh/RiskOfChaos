using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace RiskOfChaos.EffectDefinitions.World.PurchaseInteractionCost
{
    public abstract class GenericMultiplyPurchaseInteractionCostsEffect : BaseEffect
    {
        protected abstract float multiplier { get; }

        public override void OnStart()
        {
            HashSet<MultiShopController> modifiedMultiShopControllers = new HashSet<MultiShopController>();

            foreach (PurchaseInteraction purchaseInteraction in InstanceTracker.GetInstancesList<PurchaseInteraction>())
            {
                purchaseInteraction.ScaleCost(multiplier);

                if (purchaseInteraction.cost <= 0)
                    purchaseInteraction.Networkcost = 1;

                if (purchaseInteraction.TryGetComponent(out ShopTerminalBehavior shopTerminalBehavior) && shopTerminalBehavior.serverMultiShopController)
                {
                    if (modifiedMultiShopControllers.Add(shopTerminalBehavior.serverMultiShopController))
                    {
                        shopTerminalBehavior.serverMultiShopController.Networkcost = purchaseInteraction.cost;
                    }
                }
            }
        }
    }
}
