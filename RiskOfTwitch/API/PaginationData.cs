using Newtonsoft.Json;

namespace RiskOfTwitch.API
{
    public class PaginationData
    {
        [JsonProperty("cursor")]
        public string Cursor { get; set; }
    }
}
