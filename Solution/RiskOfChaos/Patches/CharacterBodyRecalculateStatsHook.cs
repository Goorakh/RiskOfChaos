using RoR2;
using System;

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
            }
            remove
            {
                _preRecalculateStats -= value;
            }
        }

        public static event RecalculateStatsDelegate PostRecalculateStats
        {
            add
            {
                _postRecalculateStats += value;
                tryApplyPatches();
            }
            remove
            {
                _postRecalculateStats -= value;
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
