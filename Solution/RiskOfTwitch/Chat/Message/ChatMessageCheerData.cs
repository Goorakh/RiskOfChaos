using Newtonsoft.Json;

namespace RiskOfTwitch.Chat.Message
{
    public sealed class ChatMessageCheerData
    {
        [JsonProperty("bits")]
        public int TotalBits { get; set; }
    }
}
