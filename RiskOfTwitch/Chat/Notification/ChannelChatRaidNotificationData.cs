using Newtonsoft.Json;

namespace RiskOfTwitch.Chat.Notification
{
    public class ChannelChatRaidNotificationData
    {
        [JsonProperty("user_id")]
        public string RaiderUserId { get; set; }

        [JsonProperty("user_name")]
        public string RaiderDisplayName { get; set; }

        [JsonProperty("user_login")]
        public string RaiderLoginName { get; set; }

        [JsonProperty("viewer_count")]
        public int NumViewers { get; set; }

        [JsonProperty("profile_image_url")]
        public string RaiderProfileImageUrl { get; set; }
    }
}
