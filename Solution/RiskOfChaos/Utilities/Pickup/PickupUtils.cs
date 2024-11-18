using R2API.Networking;
using RiskOfChaos.Networking;
using RoR2;

namespace RiskOfChaos.Utilities.Pickup
{
    public static class PickupUtils
    {
        public const PickupNotificationFlags DEFAULT_NOTIFICATION_FLAGS = PickupNotificationFlags.DisplayPushNotification | PickupNotificationFlags.PlaySound | PickupNotificationFlags.SendChatMessage;

        public static void QueuePickupMessage(CharacterMaster characterMaster, PickupIndex pickupIndex, PickupNotificationFlags notificationFlags = DEFAULT_NOTIFICATION_FLAGS)
        {
            PickupsNotificationMessage pickupMessage = new PickupsNotificationMessage(characterMaster, [pickupIndex])
            {
                Flags = notificationFlags
            };

            NetworkMessageQueue.EnqueueMessage(pickupMessage, NetworkDestination.Clients);
        }

        public static void QueuePickupMessage(string messageToken, PickupIndex pickupIndex, uint pickupQuantity, PickupNotificationFlags notificationFlags = DEFAULT_NOTIFICATION_FLAGS)
        {
            PickupsNotificationMessage pickupMessage = new PickupsNotificationMessage(messageToken, [pickupIndex], [pickupQuantity])
            {
                Flags = notificationFlags
            };

            NetworkMessageQueue.EnqueueMessage(pickupMessage, NetworkDestination.Clients);
        }

        public static void QueuePickupsMessage(CharacterMaster characterMaster, PickupIndex[] pickupIndices, PickupNotificationFlags notificationFlags = DEFAULT_NOTIFICATION_FLAGS)
        {
            if (pickupIndices.Length == 0)
                return;

            PickupsNotificationMessage pickupMessage = new PickupsNotificationMessage(characterMaster, pickupIndices)
            {
                Flags = notificationFlags
            };

            NetworkMessageQueue.EnqueueMessage(pickupMessage, NetworkDestination.Clients);
        }

        public static void QueuePickupsMessage(string messageToken, PickupIndex[] pickupIndices, uint[] pickupQuantities, PickupNotificationFlags notificationFlags = DEFAULT_NOTIFICATION_FLAGS)
        {
            if (pickupIndices.Length == 0)
                return;

            PickupsNotificationMessage pickupMessage = new PickupsNotificationMessage(messageToken, pickupIndices, pickupQuantities)
            {
                Flags = notificationFlags
            };

            NetworkMessageQueue.EnqueueMessage(pickupMessage, NetworkDestination.Clients);
        }
    }
}
