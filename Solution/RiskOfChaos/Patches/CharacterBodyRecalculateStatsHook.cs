using RiskOfChaos.Utilities;
using RoR2;

namespace RiskOfChaos.Patches
{
    static class CharacterBodyRecalculateStatsHook
    {
        public delegate void RecalculateStatsDelegate(CharacterBody body);
        static event RecalculateStatsDelegate _preRecalculateStats;
        static event RecalculateStatsDelegate _postRecalculateStats;

        static bool _appliedPatches;

        public static event RecalculateStatsDelegate PreRecalculateStats
        {
            add
            {
                _preRecalculateStats += value;
                tryApplyPatches();
                CharacterBodyUtils.MarkAllBodyStatsDirty();
            }
            remove
            {
                _preRecalculateStats -= value;
                CharacterBodyUtils.MarkAllBodyStatsDirty();
            }
        }

        public static event RecalculateStatsDelegate PostRecalculateStats
        {
            add
            {
                _postRecalculateStats += value;
                tryApplyPatches();
                CharacterBodyUtils.MarkAllBodyStatsDirty();
            }
            remove
            {
                _postRecalculateStats -= value;
                CharacterBodyUtils.MarkAllBodyStatsDirty();
            }
        }

        static void tryApplyPatches()
        {
            if (_appliedPatches)
                return;

            _appliedPatches = true;

            On.RoR2.CharacterBody.RecalculateStats += CharacterBody_RecalculateStats;
        }

        static void CharacterBody_RecalculateStats(On.RoR2.CharacterBody.orig_RecalculateStats orig, CharacterBody self)
        {
            _preRecalculateStats?.Invoke(self);
            orig(self);
            _postRecalculateStats?.Invoke(self);
        }
    }
}
