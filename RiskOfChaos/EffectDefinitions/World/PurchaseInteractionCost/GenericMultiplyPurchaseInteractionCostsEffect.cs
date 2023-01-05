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
            foreach (PurchaseInteraction purchaseInteraction in InstanceTracker.GetInstancesList<PurchaseInteraction>())
            {
                switch (purchaseInteraction.costType)
                {
                    case CostTypeIndex.Money:
                    case CostTypeIndex.PercentHealth:
                    case CostTypeIndex.LunarCoin:
                    case CostTypeIndex.VoidCoin:
                        purchaseInteraction.ScaleCost(multiplier);
                        break;
                }
            }
        }
    }
}
