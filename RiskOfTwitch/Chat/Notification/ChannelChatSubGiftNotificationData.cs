using Newtonsoft.Json;

namespace RiskOfTwitch.Chat.Notification
{
    public class ChannelChatSubGiftNotificationData
    {
        [JsonProperty("duration_months")]
        public int DurationMonths { get; set; }

        [JsonProperty("cumulative_total")]
        public int? TotalSubsGivenByUser { get; set; }

        [JsonProperty("recipient_user_id")]
        public string RecipientUserId { get; set; }

        [JsonProperty("recipient_user_name")]
        public string RecipientDisplayName { get; set; }

        [JsonProperty("recipient_user_login")]
        public string RecipientLoginName { get; set; }

        [JsonProperty("sub_tier")]
        public string SubTier { get; set; }

        [JsonProperty("community_gift_id")]
        public string CommunityGiftId { get; set; }
    }
}
