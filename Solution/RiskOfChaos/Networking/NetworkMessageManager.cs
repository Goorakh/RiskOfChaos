using R2API.Networking;

namespace RiskOfChaos.Networking
{
    internal static class NetworkMessageManager
    {
        public static void RegisterMessages()
        {
            NetworkingAPI.RegisterMessageType<TeleportBodyMessage>();
            NetworkingAPI.RegisterMessageType<SetObjectDontDestroyOnLoadMessage>();

            NetworkingAPI.RegisterMessageType<PickupTransformationNotificationMessage>();
        }
    }
}
