using R2API.Networking.Interfaces;
using RiskOfChaos.EffectHandling;
using RiskOfChaos.EffectHandling.Controllers;
using RiskOfChaos.Utilities.Extensions;
using UnityEngine.Networking;

namespace RiskOfChaos.Networking
{
    public sealed class NetworkedEffectDispatchedMessage : INetMessage
    {
        public delegate void OnReceiveDelegate(ChaosEffectInfo effectInfo, in ChaosEffectDispatchArgs args, byte[] serializedEffectData);
        public static event OnReceiveDelegate OnReceive;

        ChaosEffectIndex _effectIndex;
        ChaosEffectDispatchArgs _dispatchArgs;
        byte[] _serializedEffectData;

        public NetworkedEffectDispatchedMessage(ChaosEffectInfo effectInfo, in ChaosEffectDispatchArgs args, byte[] serializedEffectData)
        {
            _effectIndex = effectInfo.EffectIndex;
            _dispatchArgs = args;
            _serializedEffectData = serializedEffectData;
        }

        public NetworkedEffectDispatchedMessage()
        {
        }

        void ISerializableObject.Serialize(NetworkWriter writer)
        {
            writer.WriteChaosEffectIndex(_effectIndex);
            _dispatchArgs.Serialize(writer);
            writer.WriteBytesFull(_serializedEffectData);
        }

        void ISerializableObject.Deserialize(NetworkReader reader)
        {
            _effectIndex = reader.ReadChaosEffectIndex();
            _dispatchArgs = new ChaosEffectDispatchArgs(reader);
            _serializedEffectData = reader.ReadBytesAndSize();
        }

        void INetMessage.OnReceived()
        {
            OnReceive?.Invoke(ChaosEffectCatalog.GetEffectInfo(_effectIndex), _dispatchArgs, _serializedEffectData);
        }
    }
}
