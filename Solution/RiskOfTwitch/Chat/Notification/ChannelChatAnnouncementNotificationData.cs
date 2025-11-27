using Newtonsoft.Json;

namespace RiskOfTwitch.Chat.Notification
{
    public sealed class ChannelChatAnnouncementNotificationData
    {
        [JsonProperty("color")]
        public string Color { get; set; }
    }
}
