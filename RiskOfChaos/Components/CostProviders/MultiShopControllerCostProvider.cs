using RoR2;

namespace RiskOfChaos.Components.CostProviders
{
    public readonly struct MultiShopControllerCostProvider : ICostProvider
    {
        readonly MultiShopController _multiShopController;

        public MultiShopControllerCostProvider(MultiShopController multiShopController)
        {
            _multiShopController = multiShopController;
        }

        public CostTypeIndex CostType
        {
            get => _multiShopController.costType;
            set => _multiShopController.costType = value;
        }

        public int Cost
        {
            get => _multiShopController.Networkcost;
            set => _multiShopController.Networkcost = value;
        }
    }
}
