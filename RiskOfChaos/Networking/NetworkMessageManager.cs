using R2API.Networking;

namespace RiskOfChaos.Networking
{
    internal static class NetworkMessageManager
    {
        public static void RegisterMessages()
        {
            NetworkingAPI.RegisterMessageType<NetworkedEffectDispatchedMessage>();
            NetworkingAPI.RegisterMessageType<NetworkedTimedEffectEndMessage>();

            NetworkingAPI.RegisterMessageType<TeleportBodyMessage>();
        }
    }
}
