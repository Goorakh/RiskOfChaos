using Newtonsoft.Json;
using System;

namespace RiskOfChaos.SaveHandling.DataContainers
{
    [Serializable]
    public class SerializableObjectComponent
    {
        [JsonProperty("t")]
        public Type ComponentType;

        [JsonProperty("f")]
        public SerializableObjectField[] Fields;
    }
}
