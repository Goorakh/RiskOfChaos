using RoR2;
using UnityEngine;

namespace RiskOfChaos.Patches
{
    static class TeamIndicatorFixPatch
    {
        [SystemInitializer]
        static void Init()
        {
            TeamComponent.onJoinTeamGlobal += onJoinTeamGlobal;
        }

        static void onJoinTeamGlobal(TeamComponent teamComponent, TeamIndex oldTeam)
        {
            if (teamComponent.indicator)
            {
                Log.Debug($"Destroying old position indicator {Util.GetGameObjectHierarchyName(teamComponent.indicator)} for {Util.GetBestBodyName(teamComponent.gameObject)}");

                GameObject.Destroy(teamComponent.indicator);
            }
        }
    }
}
