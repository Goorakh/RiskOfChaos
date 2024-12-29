using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.Patches;
using RoR2;
using UnityEngine;

namespace RiskOfChaos.EffectDefinitions.Character
{
    [ChaosTimedEffect("all_attacks_backstab", 90f, AllowDuplicates = false)]
    public sealed class AllAttacksBackstab : MonoBehaviour
    {
        void Start()
        {
            CharacterBodyEvents.PostRecalculateStats += postRecalculateStats;
        }

        void OnDestroy()
        {
            CharacterBodyEvents.PostRecalculateStats -= postRecalculateStats;
        }

        static void postRecalculateStats(CharacterBody body)
        {
            body.canPerformBackstab = true;
        }
    }
}
