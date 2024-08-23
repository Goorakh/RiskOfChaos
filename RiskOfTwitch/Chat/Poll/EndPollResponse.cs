using Newtonsoft.Json;

namespace RiskOfTwitch.Chat.Poll
{
    public class EndPollResponse
    {
        [JsonProperty("data")]
        public PollData[] Polls { get; } = [];
    }
}
