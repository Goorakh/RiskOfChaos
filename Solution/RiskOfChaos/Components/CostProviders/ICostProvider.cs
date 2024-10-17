using RoR2;
using UnityEngine;

namespace RiskOfChaos.Components.CostProviders
{
    public interface ICostProvider
    {
        CostTypeIndex CostType { get; set; }

        int Cost { get; set; }

        public static ICostProvider GetFromObject(GameObject obj)
        {
            if (!obj)
                return null;

            if (obj.TryGetComponent(out PurchaseInteraction purchaseInteraction))
                return new PurchaseInteractionCostProvider(purchaseInteraction);

            if (obj.TryGetComponent(out MultiShopController multiShopController))
                return new MultiShopControllerCostProvider(multiShopController);

            return null;
        }
    }
}
