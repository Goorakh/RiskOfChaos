using R2API.Networking.Interfaces;
using RiskOfChaos.EffectHandling;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine.Networking;

namespace RiskOfChaos.Networking
{
    public sealed class NetworkedTimedEffectEndMessage : INetMessage
    {
        public delegate void OnReceiveDelegate(in ChaosEffectInfo effectInfo);
        public static event OnReceiveDelegate OnReceive;

        ChaosEffectInfo _effectInfo;

        public NetworkedTimedEffectEndMessage(ChaosEffectInfo effectInfo)
        {
            _effectInfo = effectInfo;
        }

        public NetworkedTimedEffectEndMessage()
        {
        }

        void ISerializableObject.Serialize(NetworkWriter writer)
        {
            writer.WritePackedIndex32(_effectInfo.EffectIndex);
        }

        void ISerializableObject.Deserialize(NetworkReader reader)
        {
            int effectIndex = reader.ReadPackedIndex32();
            if (effectIndex >= 0)
            {
                _effectInfo = ChaosEffectCatalog.GetEffectInfo((uint)effectIndex);
            }
        }

        void INetMessage.OnReceived()
        {
            OnReceive?.Invoke(_effectInfo);
        }
    }
}
