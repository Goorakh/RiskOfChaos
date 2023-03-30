using RoR2;
using UnityEngine.Networking;

namespace RiskOfChaos.Utilities.Extensions
{
    public static class CharacterMasterExtensions
    {
        public static bool IsAlive(this CharacterMaster master)
        {
            if (NetworkServer.active)
            {
                return !master.IsDeadAndOutOfLivesServer();
            }
            else
            {
                CharacterBody body = master.GetBody();
                return body && body.healthComponent.alive;
            }
        }
    }
}
