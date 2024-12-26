using RoR2;

namespace RiskOfChaos.Utilities.Extensions
{
    public static class CharacterBodyExtensions
    {
        public static bool IsPlayerOrPlayerAlly(this CharacterBody body)
        {
            if (body.isPlayerControlled)
                return true;

            TeamComponent teamComponent = body.teamComponent;
            if (teamComponent && teamComponent.teamIndex == TeamIndex.Player)
                return true;

            CharacterMaster master = body.master;
            if (master && master.IsPlayerOrPlayerAlly())
                return true;

            return false;
        }
    }
}
