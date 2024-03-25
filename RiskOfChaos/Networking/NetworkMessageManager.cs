using R2API.Networking;

namespace RiskOfChaos.Networking
{
    internal static class NetworkMessageManager
    {
        public static void RegisterMessages()
        {
            NetworkingAPI.RegisterMessageType<NetworkedEffectDispatchedMessage>();
            NetworkingAPI.RegisterMessageType<NetworkedEffectSetSerializedDataMessage>();
            NetworkingAPI.RegisterMessageType<NetworkedTimedEffectEndMessage>();

            NetworkingAPI.RegisterMessageType<TeleportBodyMessage>();
            NetworkingAPI.RegisterMessageType<SetObjectDontDestroyOnLoadMessage>();

            NetworkingAPI.RegisterMessageType<PickupTransformationNotificationMessage>();
        }
    }
}
