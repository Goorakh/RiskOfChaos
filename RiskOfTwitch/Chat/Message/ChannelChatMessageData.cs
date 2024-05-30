using Newtonsoft.Json;

namespace RiskOfTwitch.Chat.Message
{
    public class ChannelChatMessageData
    {
        [JsonProperty("text")]
        public string FullText { get; set; }

        [JsonProperty("fragments")]
        public ChatMessageFragment[] Fragments { get; set; }
    }
}
