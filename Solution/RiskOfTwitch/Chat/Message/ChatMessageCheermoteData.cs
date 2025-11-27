using Newtonsoft.Json;

namespace RiskOfTwitch.Chat.Message
{
    public sealed class ChatMessageCheermoteData
    {
        [JsonProperty("prefix")]
        public string Prefix { get; set; }

        [JsonProperty("bits")]
        public int BitAmount { get; set; }

        [JsonProperty("tier")]
        public int Tier { get; set; }
    }
}
