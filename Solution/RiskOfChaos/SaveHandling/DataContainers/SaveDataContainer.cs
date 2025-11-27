using Newtonsoft.Json;
using System;

namespace RiskOfChaos.SaveHandling.DataContainers
{
    [Serializable]
    public sealed class SaveDataContainer
    {
        [JsonProperty("o")]
        public SerializableGameObject[] Objects;
    }
}
