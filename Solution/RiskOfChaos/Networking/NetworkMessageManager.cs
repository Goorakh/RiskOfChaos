using R2API.Networking;

namespace RiskOfChaos.Networking
{
    internal static class NetworkMessageManager
    {
        public static void RegisterMessages()
        {
            NetworkingAPI.RegisterMessageType<TeleportBodyMessage>();
            NetworkingAPI.RegisterMessageType<SetObjectDontDestroyOnLoadMessage>();

            NetworkingAPI.RegisterMessageType<SetMasterLoadoutMessage>();

            NetworkingAPI.RegisterMessageType<PickupsNotificationMessage>();
            NetworkingAPI.RegisterMessageType<PickupTransformationNotificationMessage>();

            NetworkingAPI.RegisterMessageType<GrantTemporaryItemsOnJumpMessage>();

            NetworkingAPI.RegisterRequestTypes<EnsureObjectDestroyMessage, EnsureObjectDestroyMessage.Reply>();
        }
    }
}
