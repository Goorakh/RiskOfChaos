using RoR2;
using UnityEngine;

namespace RiskOfChaos.Components.CostTypeProvider
{
    public readonly struct PurchaseInteractionCostTypeProvider : ICostTypeProvider
    {
        readonly PurchaseInteraction _purchaseInteraction;

        public PurchaseInteractionCostTypeProvider(PurchaseInteraction purchaseInteraction)
        {
            _purchaseInteraction = purchaseInteraction;
        }

        public CostTypeIndex CostType
        {
            get
            {
                return _purchaseInteraction.costType;
            }
            set
            {
                _purchaseInteraction.costType = value;
            }
        }
    }
}
