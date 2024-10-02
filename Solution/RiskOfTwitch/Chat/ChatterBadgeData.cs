using Newtonsoft.Json;

namespace RiskOfTwitch.Chat
{
    public class ChatterBadgeData
    {
        [JsonProperty("set_id")]
        public string BadgeSetID { get; set; }

        [JsonProperty("id")]
        public string BadgeID { get; set; }

        [JsonProperty("info")]
        public string BadgeMetadata { get; set; }
    }
}
