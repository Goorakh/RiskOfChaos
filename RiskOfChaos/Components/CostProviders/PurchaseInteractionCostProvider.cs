using RoR2;

namespace RiskOfChaos.Components.CostProviders
{
    public readonly struct PurchaseInteractionCostProvider : ICostProvider
    {
        readonly PurchaseInteraction _purchaseInteraction;

        public PurchaseInteractionCostProvider(PurchaseInteraction purchaseInteraction)
        {
            _purchaseInteraction = purchaseInteraction;
        }

        public CostTypeIndex CostType
        {
            get => _purchaseInteraction.costType;
            set => _purchaseInteraction.costType = value;
        }

        public int Cost
        {
            get => _purchaseInteraction.cost;
            set => _purchaseInteraction.Networkcost = value;
        }
    }
}
