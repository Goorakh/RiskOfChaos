using RiskOfChaos.Utilities;
using RoR2;

namespace RiskOfChaos.Patches
{
    static class EnsureRefreshBossTitlePatch
    {
        [SystemInitializer]
        static void Init()
        {
            Inventory.onInventoryChangedGlobal += Inventory_onInventoryChangedGlobal;
        }

        static void Inventory_onInventoryChangedGlobal(Inventory inventory)
        {
            if (inventory && inventory.TryGetComponent(out CharacterMaster master))
            {
                BossUtils.TryRefreshBossTitleFor(master);
            }
        }
    }
}
