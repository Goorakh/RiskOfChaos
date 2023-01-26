using R2API.Networking.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine.Networking;

namespace RiskOfChaos.Networking
{
    public sealed class RefreshDifficultyIconsMessage : INetMessage
    {
        public delegate void OnReceive();
        public static event OnReceive OnReceived;

        public RefreshDifficultyIconsMessage()
        {
        }

        void ISerializableObject.Serialize(NetworkWriter writer)
        {
        }

        void ISerializableObject.Deserialize(NetworkReader reader)
        {
        }

        void INetMessage.OnReceived()
        {
            OnReceived?.Invoke();
        }
    }
}
