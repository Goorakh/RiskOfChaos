using R2API.Networking;
using System;
using System.Collections.Generic;
using System.Text;

namespace RiskOfChaos.Networking
{
    internal static class NetworkMessageManager
    {
        public static void RegisterMessages()
        {
            NetworkingAPI.RegisterMessageType<SyncSetGravity>();
        }
    }
}
