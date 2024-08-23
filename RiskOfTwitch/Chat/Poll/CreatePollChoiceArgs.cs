using Newtonsoft.Json;
using System;

namespace RiskOfTwitch.Chat.Poll
{
    public class CreatePollChoiceArgs
    {
        public const int MAX_TITLE_LENGTH = 25;

        [JsonProperty("title")]
        public string Title { get; }

        public CreatePollChoiceArgs(string title)
        {
            if (string.IsNullOrWhiteSpace(title))
                throw new ArgumentException($"'{nameof(title)}' cannot be null or whitespace.", nameof(title));

            if (title.Length > MAX_TITLE_LENGTH)
            {
                Log.Warning($"Poll choice title '{title}' is too long, resulting poll will be cut off at {MAX_TITLE_LENGTH} characters");
                title = title.Remove(MAX_TITLE_LENGTH);
            }

            Title = title;
        }
    }
}
