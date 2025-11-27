using Newtonsoft.Json;

namespace RiskOfTwitch.User
{
    public sealed class TwitchUserData
    {
        [JsonProperty("id")]
        public string UserId { get; set; }

        [JsonProperty("login")]
        public string UserLoginName { get; set; }

        [JsonProperty("display_name")]
        public string UserDisplayName { get; set; }

        [JsonProperty("type")]
        public string UserType { get; set; }

        [JsonProperty("broadcaster_type")]
        public string BroadcasterType { get; set; }

        [JsonProperty("description")]
        public string UserDescription { get; set; }

        [JsonProperty("profile_image_url")]
        public string ProfileImageURL { get; set; }

        [JsonProperty("offline_image_url")]
        public string OfflineImageURL { get; set; }

        [JsonProperty("email")]
        public string UserEmail { get; set; }

        [JsonProperty("created_at")]
        public string UserCreatedAt { get; set; }
    }
}
