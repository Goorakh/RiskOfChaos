using RiskOfTwitch.API;
using System;
using System.Collections.Generic;

namespace RiskOfTwitch.Chat.Poll
{
    public class GetPollsArgs
    {
        public const int MIN_IDS_COUNT = 0;
        public const int MAX_IDS_COUNT = 20;

        public string AccessToken { get; }

        public string BroadcasterId { get; }

        public string[] PollIDs { get; }

        public PaginationRequest Pagination { get; set; }

        public GetPollsArgs(string accessToken, string broadcasterId, string[] pollIDs)
        {
            if (string.IsNullOrWhiteSpace(accessToken))
                throw new ArgumentException($"'{nameof(accessToken)}' cannot be null or whitespace.", nameof(accessToken));

            if (string.IsNullOrWhiteSpace(broadcasterId))
                throw new ArgumentException($"'{nameof(broadcasterId)}' cannot be null or whitespace.", nameof(broadcasterId));

            if (pollIDs == null)
            {
                pollIDs = [];
            }
            else if (pollIDs.Length < MIN_IDS_COUNT || pollIDs.Length > MAX_IDS_COUNT)
            {
                throw new ArgumentException($"{nameof(pollIDs)} may only contain between {MIN_IDS_COUNT} and {MAX_IDS_COUNT} elements");
            }

            AccessToken = accessToken;
            BroadcasterId = broadcasterId;
            PollIDs = pollIDs ?? [];
        }

        public string GetRequestUri()
        {
            List<string> queries = [
                $"broadcaster_id={BroadcasterId}"
            ];

            if (PollIDs != null)
            {
                foreach (string pollId in PollIDs)
                {
                    queries.Add($"id={pollId}");
                }
            }

            if (Pagination != null)
            {
                string paginationSuffix = Pagination.GetRequestUriSuffix();
                if (!string.IsNullOrWhiteSpace(paginationSuffix))
                {
                    queries.Add(paginationSuffix);
                }
            }

            return $"https://api.twitch.tv/helix/polls?{string.Join("&", queries)}";
        }
    }
}
