using Newtonsoft.Json;

namespace RiskOfTwitch.Chat.Notification
{
    public class ChannelChatResubNotificationData
    {
        [JsonProperty("cumulative_months")]
        public int CumulativeMonths { get; set; }

        [JsonProperty("duration_months")]
        public int DurationMonths { get; set; }

        [JsonProperty("streak_months")]
        public int? StreakMonths { get; set; }

        [JsonProperty("sub_tier")]
        public string Tier { get; set; }

        [JsonProperty("is_prime")]
        public bool? IsPrime { get; set; }

        [JsonProperty("is_gift")]
        public bool IsGifted { get; set; }

        [JsonProperty("gifter_is_anonymous")]
        public bool? GifterIsAnonymous { get; set; }

        [JsonProperty("gifter_user_id")]
        public string GifterUserId { get; set; }

        [JsonProperty("gifter_user_name")]
        public string GifterDisplayName { get; set; }

        [JsonProperty("gifter_user_login")]
        public string GifterLoginName { get; set; }
    }
}
