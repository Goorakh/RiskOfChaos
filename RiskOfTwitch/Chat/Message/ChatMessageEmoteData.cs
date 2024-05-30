using Newtonsoft.Json;

namespace RiskOfTwitch.Chat.Message
{
    public class ChatMessageEmoteData
    {
        static readonly string[] _emoteModificationSuffixes = [
            "_BW", // Black and white
            "_HF", // Horizontal flip
            "_SG", // Sunglasses
            "_SQ", // Squished
            "_TK", // Thinking
        ];

        string _emoteID;

        [JsonProperty("id")]
        public string EmoteID
        {
            get
            {
                return _emoteID;
            }
            set
            {
                _emoteID = value;

                string baseEmoteId = _emoteID;
                foreach (string suffix in _emoteModificationSuffixes)
                {
                    if (baseEmoteId.EndsWith(suffix))
                    {
                        baseEmoteId = baseEmoteId.Remove(baseEmoteId.Length - suffix.Length);
                        break;
                    }
                }

                BaseEmoteID = baseEmoteId;
            }
        }

        [JsonIgnore]
        public string BaseEmoteID { get; private set; }

        [JsonProperty("emote_set_id")]
        public string EmoteSetID { get; set; }

        [JsonProperty("owner_id")]
        public string EmoteOwnerUserID { get; set; }

        [JsonProperty("format")]
        public string[] EmoteTypes { get; set; }
    }
}
