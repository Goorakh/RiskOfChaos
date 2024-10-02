using Newtonsoft.Json;

namespace RiskOfTwitch.Chat.Message
{
    public class ChannelChatMessageEvent
    {
        [JsonProperty("broadcaster_user_id")]
        public string BroadcasterUserId { get; set; }

        [JsonProperty("broadcaster_user_name")]
        public string BroadcasterDisplayName { get; set; }

        [JsonProperty("broadcaster_user_login")]
        public string BroadcasterLoginName { get; set; }

        [JsonProperty("chatter_user_id")]
        public string ChatterUserId { get; set; }

        [JsonProperty("chatter_user_name")]
        public string ChatterDisplayName { get; set; }

        [JsonProperty("chatter_user_login")]
        public string ChatterLoginName { get; set; }

        [JsonProperty("message")]
        public ChannelChatMessageData MessageData { get; set; }

        [JsonProperty("message_type")]
        public string MessageType { get; set; }

        [JsonProperty("badges")]
        public ChatterBadgeData[] UserBadges { get; set; }

        [JsonProperty("cheer")]
        public ChatMessageCheerData CheerData { get; set; }

        [JsonProperty("color")]
        public string UserColor { get; set; }

        [JsonProperty("reply")]
        public ChatMessageReplyData ReplyData { get; set; }

        [JsonProperty("channel_points_custom_reward_id")]
        public string ChannelPointRedeemID { get; set; }
    }
}
