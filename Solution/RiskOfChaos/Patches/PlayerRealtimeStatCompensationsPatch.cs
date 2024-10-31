using RiskOfChaos.ModificationController.TimeScale;
using RoR2;
using UnityEngine;

namespace RiskOfChaos.Patches
{
    static class PlayerRealtimeStatCompensationsPatch
    {
        [SystemInitializer]
        static void Init()
        {
            CharacterBodyRecalculateStatsHook.PostRecalculateStats += postRecalculateStats;

            On.RoR2.Util.PlayAttackSpeedSound += Util_PlayAttackSpeedSound;

            TimeScaleModificationManager.OnPlayerCompensatedTimeScaleChanged += onPlayerCompensatedTimeScaleChanged;
        }

        static void onPlayerCompensatedTimeScaleChanged()
        {
            foreach (CharacterBody body in CharacterBody.readOnlyInstancesList)
            {
                if (body.isPlayerControlled)
                {
                    body.MarkAllStatsDirty();
                }
            }
        }

        static float getTotalTimeScaleMultiplier()
        {
            if (TimeScaleModificationManager.Instance)
            {
                return TimeScaleModificationManager.Instance.PlayerCompensatedTimeScale;
            }

            return 1f;
        }

        static void postRecalculateStats(CharacterBody body)
        {
            if (body.isPlayerControlled)
            {
                float timeScaleMultiplier = getTotalTimeScaleMultiplier();

                body.moveSpeed /= timeScaleMultiplier;
                body.attackSpeed /= timeScaleMultiplier;
                body.acceleration /= timeScaleMultiplier;
            }
        }

        static uint Util_PlayAttackSpeedSound(On.RoR2.Util.orig_PlayAttackSpeedSound orig, string soundString, GameObject gameObject, float attackSpeedStat)
        {
            if (gameObject && gameObject.TryGetComponent(out CharacterBody characterBody) && characterBody.isPlayerControlled)
            {
                attackSpeedStat *= getTotalTimeScaleMultiplier();
            }

            return orig(soundString, gameObject, attackSpeedStat);
        }
    }
}
