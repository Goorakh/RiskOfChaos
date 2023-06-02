using RoR2;

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
            get => _purchaseInteraction.costType;
            set => _purchaseInteraction.costType = value;
        }
    }
}
