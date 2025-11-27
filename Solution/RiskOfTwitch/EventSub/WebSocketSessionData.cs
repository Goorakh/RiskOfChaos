using Newtonsoft.Json;
using System;

namespace RiskOfTwitch.EventSub
{
    [Serializable]
    public sealed class WebSocketSessionData
    {
        [JsonProperty("id")]
        public string SessionID { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("connected_at")]
        public string ConnectedAt { get; set; }

        [JsonProperty("keepalive_timeout_seconds")]
        public int? KeepaliveTimeoutSeconds { get; set; }

        [JsonProperty("reconnect_url")]
        public string ReconnectUrl { get; set; }
    }
}
