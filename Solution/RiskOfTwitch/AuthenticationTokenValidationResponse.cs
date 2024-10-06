using Newtonsoft.Json;
using System;

namespace RiskOfTwitch
{
    [Serializable]
    public class AuthenticationTokenValidationResponse
    {
        [JsonProperty("client_id")]
        public string ClientId { get; set; } = string.Empty;

        [JsonProperty("login")]
        public string Username { get; set; } = string.Empty;

        [JsonProperty("scopes")]
        public string[] Scopes { get; set; } = [];

        [JsonProperty("user_id")]
        public string UserID { get; set; } = string.Empty;

        [JsonProperty("expires_in")]
        int expiresInSeconds
        {
            get
            {
                return (int)Math.Round(ExpiryDate.TimeUntil.TotalSeconds);
            }
            set
            {
                ExpiryDate = DateTime.Now.AddSeconds(value);
            }
        }

        [JsonIgnore]
        public DateTimeStamp ExpiryDate { get; set; }
    }
}
