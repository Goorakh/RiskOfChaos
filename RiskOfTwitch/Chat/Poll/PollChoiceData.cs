using Newtonsoft.Json;

namespace RiskOfTwitch.Chat.Poll
{
    public class PollChoiceData
    {
        [JsonProperty("id")]
        public string ChoiceID { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("votes")]
        public int Votes { get; set; }

        [JsonProperty("channel_points_votes")]
        public int ChannelPointVotes { get; set; }
    }
}
