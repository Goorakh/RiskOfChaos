using RoR2;

namespace RiskOfChaos.Components.CostProviders
{
    public readonly struct MultiShopControllerCostProvider : ICostProvider
    {
        public readonly MultiShopController MultiShopController;

        public MultiShopControllerCostProvider(MultiShopController multiShopController)
        {
            MultiShopController = multiShopController;
        }

        public CostTypeIndex CostType
        {
            get => MultiShopController.costType;
            set => MultiShopController.costType = value;
        }

        public int Cost
        {
            get => MultiShopController.cost;
            set => MultiShopController.Networkcost = value;
        }
    }
}
