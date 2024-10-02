using RoR2;

namespace RiskOfChaos.Utilities
{
    public static class BossUtils
    {
        public static void TryRefreshBossTitleFor(CharacterMaster master)
        {
            if (!master)
                return;

            CharacterBody body = master.GetBody();
            if (!body)
                return;

            TryRefreshBossTitleFor(body);
        }

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
            bossGroup.bestObservedName = string.Empty;
            bossGroup.bestObservedSubtitle = string.Empty;
        }
    }
}
