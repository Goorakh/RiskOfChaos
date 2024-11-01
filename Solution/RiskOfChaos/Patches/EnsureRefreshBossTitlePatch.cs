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
            On.RoR2.CharacterBody.OnBuffFirstStackGained += CharacterBody_OnBuffFirstStackGained;
            On.RoR2.CharacterBody.OnBuffFinalStackLost += CharacterBody_OnBuffFinalStackLost;
        }

        static void Inventory_onInventoryChangedGlobal(Inventory inventory)
        {
            if (inventory && inventory.TryGetComponent(out CharacterMaster master))
            {
                BossUtils.TryRefreshBossTitleFor(master);
            }
        }

        static void CharacterBody_OnBuffFirstStackGained(On.RoR2.CharacterBody.orig_OnBuffFirstStackGained orig, CharacterBody self, BuffDef buffDef)
        {
            orig(self, buffDef);

            BossUtils.TryRefreshBossTitleFor(self.master);
        }

        static void CharacterBody_OnBuffFinalStackLost(On.RoR2.CharacterBody.orig_OnBuffFinalStackLost orig, CharacterBody self, BuffDef buffDef)
        {
            orig(self, buffDef);

            BossUtils.TryRefreshBossTitleFor(self.master);
        }
    }
}
