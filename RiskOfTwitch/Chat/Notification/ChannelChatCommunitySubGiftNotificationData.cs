using Newtonsoft.Json;

namespace RiskOfTwitch.Chat.Notification
{
    public class ChannelChatCommunitySubGiftNotificationData
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("total")]
        public int Amount { get; set; }

        [JsonProperty("sub_tier")]
        public string SubTier { get; set; }

        [JsonProperty("cumulative_total")]
        public int? TotalGiftedByUser { get; set; }
    }
}
