using RoR2;
using UnityEngine;

namespace RiskOfChaos.Patches
{
    // MultiShopController doesn't update it's cost hologram display value if the cost type is set to anything but Money. For some reason.
    static class MultiShopControllerCostDisplayFixPatch
    {
        [SystemInitializer]
        static void Init()
        {
            On.RoR2.MultiShopController.UpdateHologramContent += MultiShopController_UpdateHologramContent;
        }

        static void MultiShopController_UpdateHologramContent(On.RoR2.MultiShopController.orig_UpdateHologramContent orig, MultiShopController self, GameObject hologramContentObject, Transform viewer)
        {
            if (self.costType != CostTypeIndex.Money && hologramContentObject.TryGetComponent(out CostHologramContent costHologramContent))
            {
                costHologramContent.displayValue = self.cost;
            }

            orig(self, hologramContentObject, viewer);
        }
    }
}
