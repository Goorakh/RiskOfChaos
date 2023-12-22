using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RoR2;
using UnityEngine;

namespace RiskOfChaos.EffectDefinitions.Character.Player
{
    [ChaosEffect("reset_player_level")]
    public sealed class ResetPlayerLevel : BaseEffect
    {
        [EffectCanActivate]
        static bool CanActivate()
        {
            return TeamManager.instance && TeamManager.instance.GetTeamExperience(TeamIndex.Player) > 0;
        }

        [EffectWeightMultiplierSelector]
        static float GetEffectWeightMultiplier()
        {
            TeamManager teamManager = TeamManager.instance;
            if (!teamManager)
                return 0f;

            // Scale the weight multiplier up to 1 as player level increases to MIN_LEVEL rather than just having a large reduction percentage
            const uint MIN_LEVEL = 4;
            return Mathf.Clamp((teamManager.GetTeamLevel(TeamIndex.Player) - 1f) / (MIN_LEVEL - 1), 0.15f, 1f);
        }

        public override void OnStart()
        {
            TeamManager.instance.SetTeamLevel(TeamIndex.Player, 0);
        }
    }
}
