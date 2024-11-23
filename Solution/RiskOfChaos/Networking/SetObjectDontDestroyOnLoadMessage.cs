using R2API.Networking.Interfaces;
using RiskOfChaos.Utilities.Extensions;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.Networking
{
    public class SetObjectDontDestroyOnLoadMessage : INetMessage
    {
        GameObject _obj;
        bool _dontDestroyOnLoad;

        public SetObjectDontDestroyOnLoadMessage()
        {
        }

        public SetObjectDontDestroyOnLoadMessage(GameObject obj, bool dontDestroyOnLoad)
        {
            _obj = obj;
            _dontDestroyOnLoad = dontDestroyOnLoad;
        }

        void ISerializableObject.Serialize(NetworkWriter writer)
        {
            writer.Write(_obj);
            writer.Write(_dontDestroyOnLoad);
        }

        void ISerializableObject.Deserialize(NetworkReader reader)
        {
            _obj = reader.ReadGameObject();
            _dontDestroyOnLoad = reader.ReadBoolean();
        }

        void INetMessage.OnReceived()
        {
            if (NetworkServer.active)
                return;

            Log.Debug($"Received object DontDestroyOnLoad state: {_obj} ({_dontDestroyOnLoad})");

            if (_obj)
            {
                _obj.SetDontDestroyOnLoad(_dontDestroyOnLoad);
            }
        }
    }
}
