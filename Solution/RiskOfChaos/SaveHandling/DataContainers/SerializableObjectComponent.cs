using Newtonsoft.Json;
using RiskOfChaos.Serialization.Converters;
using System;

namespace RiskOfChaos.SaveHandling.DataContainers
{
    [Serializable]
    public class SerializableObjectComponent
    {
        [JsonProperty("t")]
        public Type ComponentType;

        [JsonProperty("f")]
        [JsonConverter(typeof(ObjectFieldCollectionConverter))]
        public SerializableObjectField[] Fields;
    }
}
