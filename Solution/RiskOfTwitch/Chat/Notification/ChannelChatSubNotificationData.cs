using Newtonsoft.Json;

namespace RiskOfTwitch.Chat.Notification
{
    public class ChannelChatSubNotificationData
    {
        [JsonProperty("sub_tier")]
        public string Tier { get; set; }

        [JsonProperty("is_prime")]
        public bool IsPrime { get; set; }

        [JsonProperty("duration_months")]
        public int DurationMonths { get; set; }
    }
}
