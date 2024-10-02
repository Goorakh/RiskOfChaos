using Newtonsoft.Json;

namespace RiskOfTwitch.Chat
{
    public class ChannelChatClearUserMessagesEvent
    {
        [JsonProperty("broadcaster_user_id")]
        public string BroadcasterUserId { get; set; }

        [JsonProperty("broadcaster_user_name")]
        public string BroadcasterDisplayName { get; set; }

        [JsonProperty("broadcaster_user_login")]
        public string BroadcasterLoginName { get; set; }

        [JsonProperty("target_user_id")]
        public string TargetUserId { get; set; }

        [JsonProperty("target_user_name")]
        public string TargetUserDisplayName { get; set; }

        [JsonProperty("target_user_login")]
        public string TargetUserLoginName { get; set; }
    }
}
