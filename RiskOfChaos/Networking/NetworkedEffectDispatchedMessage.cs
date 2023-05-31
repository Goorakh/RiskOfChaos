using R2API.Networking.Interfaces;
using RiskOfChaos.EffectHandling;
using RiskOfChaos.Utilities.Extensions;
using UnityEngine.Networking;

namespace RiskOfChaos.Networking
{
    public sealed class NetworkedEffectDispatchedMessage : INetMessage
    {
        public delegate void OnReceiveDelegate(in ChaosEffectInfo effectInfo, EffectDispatchFlags dispatchFlags, byte[] serializedEffectData);
        public static event OnReceiveDelegate OnReceive;

        ChaosEffectIndex _effectIndex;
        EffectDispatchFlags _dispatchFlags;
        byte[] _serializedEffectData;

        public NetworkedEffectDispatchedMessage(in ChaosEffectInfo effectInfo, EffectDispatchFlags dispatchFlags, byte[] serializedEffectData)
        {
            _effectIndex = effectInfo.EffectIndex;
            _dispatchFlags = dispatchFlags;
            _serializedEffectData = serializedEffectData;
        }

        public NetworkedEffectDispatchedMessage()
        {
        }

        void ISerializableObject.Serialize(NetworkWriter writer)
        {
            writer.WriteChaosEffectIndex(_effectIndex);
            writer.WritePackedUInt32((uint)_dispatchFlags);
            writer.WriteBytesFull(_serializedEffectData);
        }

        void ISerializableObject.Deserialize(NetworkReader reader)
        {
            _effectIndex = reader.ReadChaosEffectIndex();
            _dispatchFlags = (EffectDispatchFlags)reader.ReadPackedUInt32();
            _serializedEffectData = reader.ReadBytesAndSize();
        }

        void INetMessage.OnReceived()
        {
            OnReceive?.Invoke(ChaosEffectCatalog.GetEffectInfo(_effectIndex), _dispatchFlags, _serializedEffectData);
        }
    }
}
