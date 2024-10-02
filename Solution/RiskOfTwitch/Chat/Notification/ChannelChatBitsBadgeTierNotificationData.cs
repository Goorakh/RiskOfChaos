using Newtonsoft.Json;

namespace RiskOfTwitch.Chat.Notification
{
    public class ChannelChatBitsBadgeTierNotificationData
    {
        [JsonProperty("tier")]
        public int Tier { get; set; }
    }
}
