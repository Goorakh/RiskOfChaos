using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Net.Http.Headers;

namespace RiskOfTwitch.Chat.Poll
{
    public class EndPollArgs
    {
        public string AccessToken { get; }

        public string BroadcasterId { get; }

        public string PollId { get; }

        public PollEndType EndType { get; }

        public EndPollArgs(string accessToken, string broadcasterId, string pollId, PollEndType endType)
        {
            if (string.IsNullOrWhiteSpace(accessToken))
                throw new ArgumentException($"'{nameof(accessToken)}' cannot be null or whitespace.", nameof(accessToken));

            if (string.IsNullOrWhiteSpace(broadcasterId))
                throw new ArgumentException($"'{nameof(broadcasterId)}' cannot be null or whitespace.", nameof(broadcasterId));

            if (string.IsNullOrWhiteSpace(pollId))
                throw new ArgumentException($"'{nameof(pollId)}' cannot be null or whitespace.", nameof(pollId));

            AccessToken = accessToken;
            BroadcasterId = broadcasterId;
            PollId = pollId;
            EndType = endType;
        }

        public HttpContent GetRequestContent()
        {
            StringContent content = new StringContent(JsonConvert.SerializeObject(new
            {
                broadcaster_id = BroadcasterId,
                id = PollId,
                status = EndType switch
                {
                    PollEndType.Terminate => "TERMINATED",
                    PollEndType.Archive => "ARCHIVED",
                    _ => throw new NotImplementedException($"End status type {EndType} is not implemented")
                }
            }));

            content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

            return content;
        }
    }
}
