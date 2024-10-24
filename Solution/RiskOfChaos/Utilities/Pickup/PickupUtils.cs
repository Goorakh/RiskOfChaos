using R2API.Networking;
using RiskOfChaos.Networking;
using RoR2;

namespace RiskOfChaos.Utilities.Pickup
{
    public static class PickupUtils
    {
        public static void QueuePickupMessage(CharacterMaster characterMaster, PickupIndex pickupIndex, bool pushNotification, bool playPickupSound)
        {
            PickupNotificationMessage pickupMessage = new PickupNotificationMessage(characterMaster, pickupIndex, pushNotification, playPickupSound);

            NetworkMessageQueue.EnqueueMessage(pickupMessage, NetworkDestination.Clients);
        }
    }
}
