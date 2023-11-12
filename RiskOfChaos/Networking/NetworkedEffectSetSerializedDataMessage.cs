using R2API.Networking.Interfaces;
using RiskOfChaos.EffectHandling.Controllers;
using RiskOfChaos.EffectHandling;
using UnityEngine.Networking;

namespace RiskOfChaos.Networking
{
    public sealed class NetworkedEffectSetSerializedDataMessage : INetMessage
    {
        public delegate void OnReceiveDelegate(ulong effectDispatchID, byte[] serializedEffectData);
        public static event OnReceiveDelegate OnReceive;

        ulong _effectDispatchID;
        byte[] _serializedData;

        public NetworkedEffectSetSerializedDataMessage(ulong effectDispatchID, byte[] serializedData)
        {
            _effectDispatchID = effectDispatchID;
            _serializedData = serializedData;
        }

        public NetworkedEffectSetSerializedDataMessage()
        {
        }

        void ISerializableObject.Serialize(NetworkWriter writer)
        {
            writer.WritePackedUInt64(_effectDispatchID);
            writer.WriteBytesFull(_serializedData);
        }

        void ISerializableObject.Deserialize(NetworkReader reader)
        {
            _effectDispatchID = reader.ReadPackedUInt64();
            _serializedData = reader.ReadBytesAndSize();
        }

        void INetMessage.OnReceived()
        {
            OnReceive?.Invoke(_effectDispatchID, _serializedData);
        }
    }
}
