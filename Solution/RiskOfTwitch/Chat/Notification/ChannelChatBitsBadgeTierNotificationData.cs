using Newtonsoft.Json;

namespace RiskOfTwitch.Chat.Notification
{
    public sealed class ChannelChatBitsBadgeTierNotificationData
    {
        [JsonProperty("tier")]
        public int Tier { get; set; }
    }
}
