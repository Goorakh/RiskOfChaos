using RoR2;
using System.Collections.Generic;
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
                {
                    switch (purchaseInteraction.costType)
                    {
                        case CostTypeIndex.Money:
                        case CostTypeIndex.PercentHealth:
                            purchaseInteraction.Networkcost = 0;
                            break;
                        default:
                            purchaseInteraction.Networkcost = 1;
                            break;
                    }
                }
                else
                {
                    if (purchaseInteraction.costType == CostTypeIndex.PercentHealth)
                    {
                        purchaseInteraction.Networkcost = Mathf.Min(purchaseInteraction.Networkcost, 99);
                    }
                }

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
