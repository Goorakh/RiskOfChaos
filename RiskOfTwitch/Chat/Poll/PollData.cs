using Newtonsoft.Json;

namespace RiskOfTwitch.Chat.Poll
{
    public class PollData
    {
        [JsonProperty("id")]
        public string PollID { get; set; }

        [JsonProperty("broadcaster_id")]
        public string ChannelBroadcasterID { get; set; }

        [JsonProperty("broadcaster_name")]
        public string ChannelBroadcasterName { get; set; }

        [JsonProperty("broadcaster_login")]
        public string ChannelBroadcasterLogin { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("choices")]
        public PollChoiceData[] Choices { get; set; } = [];

        [JsonProperty("channel_points_voting_enabled")]
        public bool ChannelPointsVotingEnabled { get; set; }

        [JsonProperty("channel_points_per_vote")]
        public int ChannelPointsPerVote { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("duration")]
        public int DurationSeconds { get; set; }

        [JsonProperty("started_at")]
        public string StartedAt { get; set; }

        [JsonProperty("ended_at")]
        public string EndedAt { get; set; }
    }
}
