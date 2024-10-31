using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;

namespace RiskOfChaos.SaveHandling.DataContainers
{
    [Serializable]
    public class SerializableObjectField
    {
        [JsonProperty("n")]
        public string Name;

        [JsonProperty("v")]
        public JToken Value;
    }
}
