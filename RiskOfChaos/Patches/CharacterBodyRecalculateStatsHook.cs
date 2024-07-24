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
                setPatchesActive(true);
            }
            remove
            {
                _preRecalculateStats -= value;
                refreshPatchesActive();
            }
        }

        public static event RecalculateStatsDelegate PostRecalculateStats
        {
            add
            {
                _postRecalculateStats += value;
                setPatchesActive(true);
            }
            remove
            {
                _postRecalculateStats -= value;
                refreshPatchesActive();
            }
        }

        static void refreshPatchesActive()
        {
            static bool hasAnyListeners<T>(T del) where T : Delegate
            {
                return del?.GetInvocationList()?.Length is > 0;
            }

            setPatchesActive(hasAnyListeners(_preRecalculateStats) || hasAnyListeners(_postRecalculateStats));
        }

        static void setPatchesActive(bool active)
        {
            if (_appliedPatches == active)
                return;

            _appliedPatches = active;

            if (_appliedPatches)
            {
                On.RoR2.CharacterBody.RecalculateStats += CharacterBody_RecalculateStats;
            }
            else
            {
                On.RoR2.CharacterBody.RecalculateStats -= CharacterBody_RecalculateStats;
            }
        }

        static void CharacterBody_RecalculateStats(On.RoR2.CharacterBody.orig_RecalculateStats orig, CharacterBody self)
        {
            _preRecalculateStats?.Invoke(self);
            orig(self);
            _postRecalculateStats?.Invoke(self);
        }
    }
}
