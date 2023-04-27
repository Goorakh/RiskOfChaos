using RoR2;
using UnityEngine;

namespace RiskOfChaos.Components.CostTypeProvider
{
    public readonly struct MultiShopControllerCostTypeProvider : ICostTypeProvider
    {
        readonly MultiShopController _multiShopController;

        public MultiShopControllerCostTypeProvider(MultiShopController multiShopController)
        {
            _multiShopController = multiShopController;
        }

        public CostTypeIndex CostType
        {
            get
            {
                return _multiShopController.costType;
            }
            set
            {
                _multiShopController.costType = value;
            }
        }
    }
}
