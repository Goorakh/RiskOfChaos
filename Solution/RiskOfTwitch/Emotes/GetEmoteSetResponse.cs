using Newtonsoft.Json;
using System;

namespace RiskOfTwitch.Emotes
{
    public sealed class GetEmoteSetResponse
    {
        public static GetEmoteSetResponse Empty { get; } = new GetEmoteSetResponse
        {
            Emotes = [],
            UrlTemplate = string.Empty
        };

        [JsonProperty("data")]
        public EmoteSetEmoteData[] Emotes { get; set; }

        [JsonProperty("template")]
        public string UrlTemplate { get; set; }

        public bool TryFindEmote(string emoteId, out EmoteSetEmoteData emote)
        {
            foreach (EmoteSetEmoteData e in Emotes)
            {
                if (string.Equals(e.EmoteId, emoteId, StringComparison.Ordinal))
                {
                    emote = e;
                    return true;
                }
            }

            emote = null;
            return false;
        }

        public string GetEmoteFetchUrl(EmoteSetEmoteData emote, string format, string theme, string scale)
        {
            // https://static-cdn.jtvnw.net/emoticons/v2/{{id}}/{{format}}/{{theme_mode}}/{{scale}}

            return UrlTemplate.Replace("{{id}}", emote.EmoteId)
                              .Replace("{{format}}", format)
                              .Replace("{{theme_mode}}", theme)
                              .Replace("{{scale}}", scale);
        }
    }
}
