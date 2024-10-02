using R2API.Networking.Interfaces;
using UnityEngine.Networking;

namespace RiskOfChaos.Networking
{
    public sealed class NetworkedTimedEffectEndMessage : INetMessage
    {
        public delegate void OnReceiveDelegate(ulong effectDispatchID);
        public static event OnReceiveDelegate OnReceive;

        ulong _effectDispatchID;

        public NetworkedTimedEffectEndMessage(ulong effectDispatchID)
        {
            _effectDispatchID = effectDispatchID;
        }

        public NetworkedTimedEffectEndMessage()
        {
        }

        void ISerializableObject.Serialize(NetworkWriter writer)
        {
            writer.WritePackedUInt64(_effectDispatchID);
        }

        void ISerializableObject.Deserialize(NetworkReader reader)
        {
            _effectDispatchID = reader.ReadPackedUInt64();
        }

        void INetMessage.OnReceived()
        {
            OnReceive?.Invoke(_effectDispatchID);
        }
    }
}
