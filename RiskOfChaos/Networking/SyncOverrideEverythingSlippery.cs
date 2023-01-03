using R2API.Networking.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine.Networking;

namespace RiskOfChaos.Networking
{
    public class SyncOverrideEverythingSlippery : INetMessage
    {
        public delegate void OnReceiveDelegate(bool overrideIsSlippery);
        public static event OnReceiveDelegate OnReceive;

        bool _overrideIsSlippery;

        public SyncOverrideEverythingSlippery(bool overrideIsSlippery)
        {
            _overrideIsSlippery = overrideIsSlippery;
        }

        public SyncOverrideEverythingSlippery()
        {
        }

        void ISerializableObject.Serialize(NetworkWriter writer)
        {
            writer.Write(_overrideIsSlippery);
        }

        void ISerializableObject.Deserialize(NetworkReader reader)
        {
            _overrideIsSlippery = reader.ReadBoolean();
        }

        void INetMessage.OnReceived()
        {
            OnReceive?.Invoke(_overrideIsSlippery);
        }
    }
}
