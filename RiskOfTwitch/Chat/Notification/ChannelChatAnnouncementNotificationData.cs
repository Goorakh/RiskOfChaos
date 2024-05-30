using Newtonsoft.Json;

namespace RiskOfTwitch.Chat.Notification
{
    public class ChannelChatAnnouncementNotificationData
    {
        [JsonProperty("color")]
        public string Color { get; set; }
    }
}
