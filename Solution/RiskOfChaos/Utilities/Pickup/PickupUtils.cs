using R2API.Networking;
using RiskOfChaos.Networking;
using RoR2;

namespace RiskOfChaos.Utilities.Pickup
{
    public static class PickupUtils
    {
        public static void QueuePickupMessage(CharacterMaster characterMaster, PickupIndex pickupIndex, bool pushNotification, bool playPickupSound)
        {
            PickupsNotificationMessage pickupMessage = new PickupsNotificationMessage(characterMaster, [pickupIndex])
            {
                DisplayPushNotification = pushNotification,
                PlaySound = playPickupSound
            };

            NetworkMessageQueue.EnqueueMessage(pickupMessage, NetworkDestination.Clients);
        }

        public static void QueuePickupMessage(string messageToken, PickupIndex pickupIndex, uint pickupQuantity, bool pushNotification, bool playPickupSound)
        {
            PickupsNotificationMessage pickupMessage = new PickupsNotificationMessage(messageToken, [pickupIndex], [pickupQuantity])
            {
                DisplayPushNotification = pushNotification,
                PlaySound = playPickupSound
            };

            NetworkMessageQueue.EnqueueMessage(pickupMessage, NetworkDestination.Clients);
        }

        public static void QueuePickupsMessage(CharacterMaster characterMaster, PickupIndex[] pickupIndices, bool pushNotification, bool playPickupSound)
        {
            if (pickupIndices.Length == 0)
                return;

            PickupsNotificationMessage pickupMessage = new PickupsNotificationMessage(characterMaster, pickupIndices)
            {
                DisplayPushNotification = pushNotification,
                PlaySound = playPickupSound
            };

            NetworkMessageQueue.EnqueueMessage(pickupMessage, NetworkDestination.Clients);
        }

        public static void QueuePickupsMessage(string messageToken, PickupIndex[] pickupIndices, uint[] pickupQuantities, bool pushNotification, bool playPickupSound)
        {
            if (pickupIndices.Length == 0)
                return;

            PickupsNotificationMessage pickupMessage = new PickupsNotificationMessage(messageToken, pickupIndices, pickupQuantities)
            {
                DisplayPushNotification = pushNotification,
                PlaySound = playPickupSound
            };

            NetworkMessageQueue.EnqueueMessage(pickupMessage, NetworkDestination.Clients);
        }
    }
}
