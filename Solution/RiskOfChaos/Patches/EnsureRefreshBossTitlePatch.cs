using RiskOfChaos.Utilities;
using RoR2;

namespace RiskOfChaos.Patches
{
    static class EnsureRefreshBossTitlePatch
    {
        [SystemInitializer]
        static void Init()
        {
            Language.onCurrentLanguageChanged += Language_onCurrentLanguageChanged;
            Inventory.onInventoryChangedGlobal += Inventory_onInventoryChangedGlobal;
            CharacterBodyEvents.OnBuffFirstStackGained += CharacterBody_OnBuffFirstStackGained;
            CharacterBodyEvents.OnBuffFinalStackLost += CharacterBody_OnBuffFinalStackLost;
        }

        static void Language_onCurrentLanguageChanged()
        {
            foreach (BossGroup bossGroup in InstanceTracker.GetInstancesList<BossGroup>())
            {
                BossUtils.RefreshBossTitle(bossGroup);
            }
        }

        static void Inventory_onInventoryChangedGlobal(Inventory inventory)
        {
            if (inventory && inventory.TryGetComponent(out CharacterMaster master))
            {
                BossUtils.TryRefreshBossTitleFor(master);
            }
        }

        static void CharacterBody_OnBuffFirstStackGained(CharacterBody body, BuffDef buffDef)
        {
            BossUtils.TryRefreshBossTitleFor(body.master);
        }

        static void CharacterBody_OnBuffFinalStackLost(CharacterBody body, BuffDef buffDef)
        {
            BossUtils.TryRefreshBossTitleFor(body.master);
        }
    }
}
