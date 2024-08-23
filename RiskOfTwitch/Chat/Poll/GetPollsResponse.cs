using Newtonsoft.Json;
using RiskOfTwitch.API;

namespace RiskOfTwitch.Chat.Poll
{
    public class GetPollsResponse
    {
        [JsonProperty("data")]
        public PollData[] Polls { get; set; } = [];

        [JsonProperty("pagination")]
        public PaginationData Pagination { get; set; }
    }
}
