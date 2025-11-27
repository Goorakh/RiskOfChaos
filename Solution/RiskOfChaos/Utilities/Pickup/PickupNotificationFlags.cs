using System;

namespace RiskOfChaos.Utilities.Pickup
{
    [Flags]
    public enum PickupNotificationFlags : uint
    {
        None = 0,
        DisplayPushNotification = 1 << 0,
        DisplayPushNotificationIfNoneQueued = 1 << 1,
        PlaySound = 1 << 2,
        SendChatMessage = 1 << 3,
    }
}
