using Newtonsoft.Json;

namespace RiskOfTwitch.Chat.Notification
{
    public class ChannelChatPrimePaidUpgradeNotificationData
    {
        [JsonProperty("sub_tier")]
        public string Tier { get; set; }
    }
}
