using R2API.Networking;
using RiskOfChaos.Networking;
using RoR2;

namespace RiskOfChaos.Utilities.Pickup
{
    public static class PickupUtils
    {
        public static void QueuePickupMessage(CharacterMaster characterMaster, PickupIndex pickupIndex, bool pushNotification, bool playPickupSound)
        {
            PickupsNotificationMessage pickupMessage = new PickupsNotificationMessage(characterMaster, [pickupIndex], pushNotification, playPickupSound);

            NetworkMessageQueue.EnqueueMessage(pickupMessage, NetworkDestination.Clients);
        }

        public static void QueuePickupsMessage(CharacterMaster characterMaster, PickupIndex[] pickupIndices, bool pushNotification, bool playPickupSound)
        {
            if (pickupIndices.Length == 0)
                return;

            PickupsNotificationMessage pickupMessage = new PickupsNotificationMessage(characterMaster, pickupIndices, pushNotification, playPickupSound);

            NetworkMessageQueue.EnqueueMessage(pickupMessage, NetworkDestination.Clients);
        }
    }
}
