using Newtonsoft.Json;

namespace RiskOfTwitch.User
{
    public class GetUsersResponse
    {
        public static GetUsersResponse Empty { get; } = new GetUsersResponse
        {
            Users = []
        };

        [JsonProperty("data")]
        public TwitchUserData[] Users { get; set; } = [];
    }
}
