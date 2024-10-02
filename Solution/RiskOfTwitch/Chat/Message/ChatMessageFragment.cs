using Newtonsoft.Json;

namespace RiskOfTwitch.Chat.Message
{
    public class ChatMessageFragment
    {
        [JsonProperty("type")]
        public string FragmentType { get; set; }

        [JsonProperty("text")]
        public string FragmentText { get; set; }

        [JsonProperty("cheermote")]
        public ChatMessageCheermoteData CheermoteData { get; set; }

        [JsonProperty("emote")]
        public ChatMessageEmoteData EmoteData { get; set; }

        [JsonProperty("mention")]
        public ChatMessageMentionData MentionData { get; set; }
    }
}
