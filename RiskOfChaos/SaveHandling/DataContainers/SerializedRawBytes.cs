using System;
using System.Runtime.Serialization;

namespace RiskOfChaos.SaveHandling.DataContainers
{
    [Serializable]
    public class SerializedRawBytes
    {
        [DataMember(Name = "b64")]
        public string Base64;

        [IgnoreDataMember]
        public byte[] Bytes
        {
            get
            {
                return Convert.FromBase64String(Base64);
            }
            set
            {
                Base64 = Convert.ToBase64String(value);
            }
        }

        public static implicit operator SerializedRawBytes(string data)
        {
            return new SerializedRawBytes { Base64 = data };
        }

        public static implicit operator SerializedRawBytes(byte[] data)
        {
            return new SerializedRawBytes { Bytes = data };
        }
    }
}
