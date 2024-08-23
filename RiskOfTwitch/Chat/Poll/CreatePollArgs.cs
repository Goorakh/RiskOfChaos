using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Text;

namespace RiskOfTwitch.Chat.Poll
{
    public class CreatePollArgs
    {
        // Value limits and documentation: https://dev.twitch.tv/docs/api/reference/#create-poll

        public const int MAX_TITLE_LENGTH = 60;

        public const int MIN_CHOICE_COUNT = 2;
        public const int MAX_CHOICE_COUNT = 5;

        public const int MIN_DURATION = 15;
        public const int MAX_DURATION = 1800;

        public const int MIN_CHANNEL_POINTS_PER_VOTE = 1;
        public const int MAX_CHANNEL_POINTS_PER_VOTE = 1_000_000;

        [JsonIgnore]
        public string AccessToken { get; }

        [JsonProperty("broadcaster_id")]
        public string BroadcasterId { get; }

        [JsonProperty("title")]
        public string Title { get; }

        [JsonProperty("choices")]
        public CreatePollChoiceArgs[] Choices { get; }

        [JsonProperty("duration")]
        public int DurationSeconds { get; }

        [JsonProperty("channel_points_voting_enabled")]
        public bool AllowChannelPointVoting { get; set; } = false;

        [JsonProperty("channel_points_per_vote")]
        public uint ChannelPointsPerVote { get; set; }

        public CreatePollArgs(string accessToken, string broadcasterId, string title, CreatePollChoiceArgs[] choices, int durationSeconds)
        {
            if (string.IsNullOrWhiteSpace(accessToken))
                throw new ArgumentException($"'{nameof(accessToken)}' cannot be null or whitespace.", nameof(accessToken));

            if (string.IsNullOrWhiteSpace(broadcasterId))
                throw new ArgumentException($"'{nameof(broadcasterId)}' cannot be null or whitespace.", nameof(broadcasterId));

            if (string.IsNullOrWhiteSpace(title))
                throw new ArgumentException($"'{nameof(title)}' cannot be null or whitespace.", nameof(title));

            if (title.Length > MAX_TITLE_LENGTH)
            {
                Log.Warning($"Poll title '{title}' is too long, resulting poll will be cut off at {MAX_TITLE_LENGTH} characters");
                title = title.Remove(MAX_TITLE_LENGTH);
            }

            if (choices == null)
                throw new ArgumentNullException(nameof(choices));

            if (choices.Length < MIN_CHOICE_COUNT || choices.Length > MAX_CHOICE_COUNT)
                throw new ArgumentException($"'{nameof(choices)}' must contain between {MIN_CHOICE_COUNT} and {MAX_CHOICE_COUNT} elements", nameof(choices));

            if (durationSeconds < MIN_DURATION || durationSeconds > MAX_DURATION)
                throw new ArgumentOutOfRangeException(nameof(durationSeconds), $"duration must be between {MIN_DURATION} and {MAX_DURATION}");

            AccessToken = accessToken;
            BroadcasterId = broadcasterId;
            Title = title;
            Choices = choices;
            DurationSeconds = durationSeconds;
        }

        public bool Validate(out Exception exception)
        {
            if (AllowChannelPointVoting)
            {
                if (ChannelPointsPerVote < MIN_CHANNEL_POINTS_PER_VOTE || ChannelPointsPerVote > MAX_CHANNEL_POINTS_PER_VOTE)
                {
                    exception = new ArgumentException($"{nameof(ChannelPointsPerVote)} must be between {MIN_CHANNEL_POINTS_PER_VOTE} and {MAX_CHANNEL_POINTS_PER_VOTE}");
                    return false;
                }
            }

            exception = null;
            return true;
        }

        public HttpContent GetHttpContent()
        {
            return new StringContent(JsonConvert.SerializeObject(this), Encoding.UTF8, "application/json");
        }
    }
}
