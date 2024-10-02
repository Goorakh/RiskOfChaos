using R2API.Networking.Interfaces;
using RiskOfChaos.Utilities;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.Networking
{
    public sealed class TeleportBodyMessage : INetMessage
    {
        GameObject _bodyObject;
        Vector3 _targetFootPosition;

        public TeleportBodyMessage(GameObject bodyObject, Vector3 targetFootPosition)
        {
            _bodyObject = bodyObject;
            _targetFootPosition = targetFootPosition;
        }

        public TeleportBodyMessage()
        {
        }

        void ISerializableObject.Serialize(NetworkWriter writer)
        {
            writer.Write(_bodyObject);
            writer.Write(_targetFootPosition);
        }

        void ISerializableObject.Deserialize(NetworkReader reader)
        {
            _bodyObject = reader.ReadGameObject();
            _targetFootPosition = reader.ReadVector3();
        }

        void INetMessage.OnReceived()
        {
            if (!_bodyObject)
            {
                Log.Error("Null body object");
                return;
            }

            CharacterBody body = _bodyObject.GetComponent<CharacterBody>();
            if (!body)
            {
                Log.Error($"{_bodyObject} has no {nameof(CharacterBody)} component");
                return;
            }

            TeleportUtils.TeleportBody(body, _targetFootPosition);
        }
    }
}
