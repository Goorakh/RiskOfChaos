using Newtonsoft.Json;
using System;
using UnityEngine.Networking;

namespace RiskOfChaos.SaveHandling.DataContainers
{
    [Serializable]
    public sealed class SerializableGameObject
    {
        [JsonProperty("id")]
        public NetworkHash128 PrefabAssetId;

        [JsonProperty("c")]
        public SerializableObjectComponent[] Components;
    }
}
