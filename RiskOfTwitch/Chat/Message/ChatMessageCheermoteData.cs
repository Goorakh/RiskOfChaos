using Newtonsoft.Json;

namespace RiskOfTwitch.Chat.Message
{
    public class ChatMessageCheermoteData
    {
        [JsonProperty("prefix")]
        public string Prefix { get; set; }

        [JsonProperty("bits")]
        public int BitAmount { get; set; }

        [JsonProperty("tier")]
        public int Tier { get; set; }
    }
}
