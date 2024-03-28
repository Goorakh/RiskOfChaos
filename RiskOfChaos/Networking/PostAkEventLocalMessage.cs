using R2API.Networking.Interfaces;
using UnityEngine.Networking;

namespace RiskOfChaos.Networking
{
    public class PostAkEventLocalMessage : INetMessage
    {
        bool _isEventName;

        string _eventName;
        uint _eventID;

        public PostAkEventLocalMessage()
        {
        }

        PostAkEventLocalMessage(bool isEventName, string eventName, uint eventID)
        {
            _isEventName = isEventName;
            _eventName = eventName;
            _eventID = eventID;
        }

        public PostAkEventLocalMessage(uint eventID) : this(false, default, eventID)
        {
        }

        public PostAkEventLocalMessage(string eventName) : this(true, eventName, default)
        {
        }

        public void Serialize(NetworkWriter writer)
        {
            writer.Write(_isEventName);
            if (_isEventName)
            {
                writer.Write(_eventName);
            }
            else
            {
                writer.WritePackedUInt32(_eventID);
            }
        }

        public void Deserialize(NetworkReader reader)
        {
            _isEventName = reader.ReadBoolean();

            if (_isEventName)
            {
                _eventName = reader.ReadString();
            }
            else
            {
                _eventID = reader.ReadPackedUInt32();
            }
        }

        public void OnReceived()
        {
            uint eventID = _isEventName ? AkSoundEngine.GetIDFromString(_eventName) : _eventID;
            if (eventID == 0)
                return;

            foreach (AkAudioListener audioListener in AkAudioListener.DefaultListeners.ListenerList)
            {
                AkSoundEngine.PostEvent(eventID, audioListener.gameObject);
            }
        }
    }
}
