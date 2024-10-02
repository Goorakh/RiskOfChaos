using R2API.Networking;
using R2API.Networking.Interfaces;
using RiskOfChaos.Networking;
using RoR2;
using UnityEngine.Networking;

namespace RiskOfChaos.Utilities
{
    public static class CharacterMasterNotificationQueueUtils
    {
        public static void SendPickupTransformNotification(CharacterMaster characterMaster, PickupIndex fromPickupIndex, PickupIndex toPickupIndex, CharacterMasterNotificationQueue.TransformationType transformationType)
        {
            if (!NetworkServer.active)
            {
                Log.Error("Called on client");
                return;
            }

            new PickupTransformationNotificationMessage(characterMaster, fromPickupIndex, toPickupIndex, transformationType).Send(NetworkDestination.Clients | NetworkDestination.Server);
        }
    }
}
