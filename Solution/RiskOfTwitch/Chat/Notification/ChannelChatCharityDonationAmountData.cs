using Newtonsoft.Json;

namespace RiskOfTwitch.Chat.Notification
{
    public class ChannelChatCharityDonationAmountData
    {
        [JsonProperty("value")]
        public int Value { get; set; }

        [JsonProperty("decimal_place")]
        public int DecimalPlace { get; set; }

        [JsonProperty("currency")]
        public string CurrencyCode { get; set; }
    }
}
