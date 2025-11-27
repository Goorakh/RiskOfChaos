using Newtonsoft.Json;

namespace RiskOfTwitch.Emotes
{
    public sealed class EmoteSetEmoteImageData
    {
        [JsonProperty("url_1x")]
        public string SmallUrl { get; set; }

        [JsonProperty("url_2x")]
        public string MediumUrl { get; set; }

        [JsonProperty("url_4x")]
        public string LargeUrl { get; set; }
    }
}
