using Newtonsoft.Json;

namespace RiskOfTwitch.Chat.Notification
{
    public sealed class ChannelChatPrimePaidUpgradeNotificationData
    {
        [JsonProperty("sub_tier")]
        public string Tier { get; set; }
    }
}
