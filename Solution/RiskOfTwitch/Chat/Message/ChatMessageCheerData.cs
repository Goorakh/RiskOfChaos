using Newtonsoft.Json;

namespace RiskOfTwitch.Chat.Message
{
    public class ChatMessageCheerData
    {
        [JsonProperty("bits")]
        public int TotalBits { get; set; }
    }
}
