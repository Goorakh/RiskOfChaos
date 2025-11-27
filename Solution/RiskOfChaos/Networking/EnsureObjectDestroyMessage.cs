using R2API.Networking.Interfaces;
using RoR2;
using System;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.Networking
{
    public sealed class EnsureObjectDestroyMessage : INetRequest<EnsureObjectDestroyMessage, EnsureObjectDestroyMessage.Reply>
    {
        public sealed class Reply : INetRequestReply<EnsureObjectDestroyMessage, Reply>
        {
            public static event Action<Reply> OnReceived;

            public NetworkInstanceId ObjectId { get; private set; }

            public int ClientConnectionId { get; private set; }

            public Reply(NetworkInstanceId objectId, int clientConnectionId)
            {
                ObjectId = objectId;
                ClientConnectionId = clientConnectionId;
            }

            public Reply()
            {
            }

            public void Serialize(NetworkWriter writer)
            {
                writer.Write(ObjectId);
                writer.WritePackedUInt32((uint)ClientConnectionId);
            }

            public void Deserialize(NetworkReader reader)
            {
                ObjectId = reader.ReadNetworkId();
                ClientConnectionId = (int)reader.ReadPackedUInt32();
            }

            public void OnReplyReceived()
            {
                OnReceived?.Invoke(this);
            }
        }

        NetworkInstanceId _objectNetId;
        int _clientConnectionId;

        public EnsureObjectDestroyMessage(NetworkInstanceId objectId, int connectionId)
        {
            _objectNetId = objectId;
            _clientConnectionId = connectionId;
        }

        public EnsureObjectDestroyMessage()
        {
        }

        public void Serialize(NetworkWriter writer)
        {
            writer.Write(_objectNetId);
            writer.WritePackedUInt32((uint)_clientConnectionId);
        }

        public void Deserialize(NetworkReader reader)
        {
            _objectNetId = reader.ReadNetworkId();
            _clientConnectionId = (int)reader.ReadPackedUInt32();
        }

        public Reply OnRequestReceived()
        {
            GameObject gameObject = Util.FindNetworkObject(_objectNetId);
            if (gameObject)
            {
                Log.Debug($"Destroying object {Util.GetGameObjectHierarchyName(gameObject)}");
                GameObject.Destroy(gameObject);
            }
            else
            {
                Log.Debug($"Could not find network object with id {_objectNetId}, assuming already destroyed");
            }

            return new Reply(_objectNetId, _clientConnectionId);
        }
    }
}
