using Newtonsoft.Json;

namespace RiskOfTwitch.Chat.Notification
{
    public sealed class ChannelChatGiftPaidUpgradeNotificationData
    {
        [JsonProperty("gifter_is_anonymous")]
        public bool GifterIsAnonymous { get; set; }

        [JsonProperty("gifter_user_id")]
        public string GifterUserId { get; set; }

        [JsonProperty("gifter_user_name")]
        public string GifterDisplayName { get; set; }

        [JsonProperty("gifter_user_login")]
        public string GifterLoginName { get; set; }
    }
}
