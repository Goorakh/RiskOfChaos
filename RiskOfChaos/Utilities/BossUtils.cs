using RoR2;

namespace RiskOfChaos.Utilities
{
    public static class BossUtils
    {
        public static void TryRefreshBossTitleFor(CharacterBody characterBody)
        {
            BossGroup bossGroup = BossGroup.FindBossGroup(characterBody);
            if (bossGroup)
            {
                RefreshBossTitle(bossGroup);
            }
        }

        public static void RefreshBossTitle(BossGroup bossGroup)
        {
#pragma warning disable Publicizer001 // Accessing a member that was not originally public
            bossGroup.bestObservedName = string.Empty;
            bossGroup.bestObservedSubtitle = string.Empty;
#pragma warning restore Publicizer001 // Accessing a member that was not originally public
        }
    }
}
