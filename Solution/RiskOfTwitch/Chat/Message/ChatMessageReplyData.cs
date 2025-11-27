using Newtonsoft.Json;

namespace RiskOfTwitch.Chat.Message
{
    public sealed class ChatMessageReplyData
    {
        [JsonProperty("parent_message_id")]
        public string ReplyingToMessageID { get; set; }

        [JsonProperty("parent_message_body")]
        public string ReplyingToMessageBody { get; set; }

        [JsonProperty("parent_user_id")]
        public string ReplyingToUserID { get; set; }

        [JsonProperty("parent_user_name")]
        public string ReplyingToUserDisplayName { get; set; }

        [JsonProperty("parent_user_login")]
        public string ReplyingToUserLoginName { get; set; }

        [JsonProperty("thread_message_id")]
        public string ThreadRootMessageID { get; set; }

        [JsonProperty("thread_user_id")]
        public string ThreadRootUserID { get; set; }

        [JsonProperty("thread_user_name")]
        public string ThreadRootUserDisplayName { get; set; }

        [JsonProperty("thread_user_login")]
        public string ThreadRootUserLoginName { get; set; }
    }
}
