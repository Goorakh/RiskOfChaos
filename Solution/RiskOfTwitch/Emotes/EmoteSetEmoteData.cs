using Newtonsoft.Json;

namespace RiskOfTwitch.Emotes
{
    public class EmoteSetEmoteData
    {
        [JsonProperty("id")]
        public string EmoteId { get; set; }

        [JsonProperty("name")]
        public string EmoteName { get; set; }

        [JsonProperty("images")]
        public EmoteSetEmoteImageData ImageData { get; set; }

        [JsonProperty("emote_type")]
        public string EmoteType { get; set; }

        [JsonProperty("emote_set_id")]
        public string EmoteSetId { get; set; }

        [JsonProperty("owner_id")]
        public string EmoteOwnerId { get; set; }

        [JsonProperty("format")]
        public string[] EmoteFormats { get; set; }

        [JsonProperty("scale")]
        public string[] Scales { get; set; }

        [JsonProperty("theme_mode")]
        public string[] ThemeModes { get; set; }
    }
}
