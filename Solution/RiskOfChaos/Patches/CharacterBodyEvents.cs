using RiskOfChaos.Utilities;
using RoR2;

namespace RiskOfChaos.Patches
{
    static class CharacterBodyEvents
    {
        public delegate void RecalculateStatsDelegate(CharacterBody body);
        public delegate void BuffStackDelegate(CharacterBody body, BuffDef buffDef);

        static event RecalculateStatsDelegate _preRecalculateStats;
        public static event RecalculateStatsDelegate PreRecalculateStats
        {
            add
            {
                _preRecalculateStats += value;
                CharacterBodyUtils.MarkAllBodyStatsDirty();
            }
            remove
            {
                _preRecalculateStats -= value;
                CharacterBodyUtils.MarkAllBodyStatsDirty();
            }
        }

        static event RecalculateStatsDelegate _postRecalculateStats;
        public static event RecalculateStatsDelegate PostRecalculateStats
        {
            add
            {
                _postRecalculateStats += value;
                CharacterBodyUtils.MarkAllBodyStatsDirty();
            }
            remove
            {
                _postRecalculateStats -= value;
                CharacterBodyUtils.MarkAllBodyStatsDirty();
            }
        }

        public static event BuffStackDelegate OnBuffFirstStackGained;

        public static event BuffStackDelegate OnBuffFinalStackLost;

        [SystemInitializer]
        static void tryApplyPatches()
        {
            On.RoR2.CharacterBody.RecalculateStats += CharacterBody_RecalculateStats;
            On.RoR2.CharacterBody.OnBuffFirstStackGained += CharacterBody_OnBuffFirstStackGained;
            On.RoR2.CharacterBody.OnBuffFinalStackLost += CharacterBody_OnBuffFinalStackLost;
        }

        static void CharacterBody_RecalculateStats(On.RoR2.CharacterBody.orig_RecalculateStats orig, CharacterBody self)
        {
            _preRecalculateStats?.Invoke(self);
            orig(self);
            _postRecalculateStats?.Invoke(self);
        }

        static void CharacterBody_OnBuffFirstStackGained(On.RoR2.CharacterBody.orig_OnBuffFirstStackGained orig, CharacterBody self, BuffDef buffDef)
        {
            orig(self, buffDef);
            OnBuffFirstStackGained?.Invoke(self, buffDef);
        }

        static void CharacterBody_OnBuffFinalStackLost(On.RoR2.CharacterBody.orig_OnBuffFinalStackLost orig, CharacterBody self, BuffDef buffDef)
        {
            orig(self, buffDef);
            OnBuffFinalStackLost?.Invoke(self, buffDef);
        }
    }
}
