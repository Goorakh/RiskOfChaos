using Newtonsoft.Json;

namespace RiskOfTwitch.Chat.Poll
{
    public class CreatePollResponse
    {
        [JsonProperty("data")]
        public PollData[] Polls { get; set; } = [];
    }
}
