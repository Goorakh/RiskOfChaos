using Newtonsoft.Json;
using RiskOfTwitch.User;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace RiskOfTwitch
{
    public static class StaticTwitchAPI
    {
        public static async Task<GetUsersResponse> GetUsers(string accessToken, string[] userIds, string[] usernames, CancellationToken cancellationToken = default)
        {
            userIds ??= [];
            usernames ??= [];

            if (userIds.Length == 0 && usernames.Length == 0)
                return GetUsersResponse.Empty;

            if (userIds.Length + usernames.Length > 100)
                throw new ArgumentOutOfRangeException($"{nameof(userIds)}, {nameof(usernames)}", "Combined size of user ids and usernames cannot exceed 100");

            using HttpClient client = new HttpClient();

            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");
            client.DefaultRequestHeaders.Add("Client-Id", Authentication.CLIENT_ID);

            string combinedUserIds = string.Join("&", Array.ConvertAll(userIds, id => $"id={id}"));
            string combinedUsernames = string.Join("&", Array.ConvertAll(usernames, username => $"login={username}"));

            string query;
            if (string.IsNullOrEmpty(combinedUsernames))
            {
                query = combinedUserIds;
            }
            else if (string.IsNullOrEmpty(combinedUserIds))
            {
                query = combinedUsernames;
            }
            else
            {
                query = string.Join("&", [combinedUserIds, combinedUsernames]);
            }

            using HttpResponseMessage getUsersResponseMessage = await client.GetAsync($"https://api.twitch.tv/helix/users?{query}", cancellationToken);
            if (!getUsersResponseMessage.IsSuccessStatusCode)
            {
                Log.Error($"Twitch API responded with error code {getUsersResponseMessage.StatusCode:D} {getUsersResponseMessage.ReasonPhrase}");
                return GetUsersResponse.Empty;
            }

            GetUsersResponse getUsersResponse;
            try
            {
                getUsersResponse = JsonConvert.DeserializeObject<GetUsersResponse>(await getUsersResponseMessage.Content.ReadAsStringAsync());
            }
            catch (JsonException e)
            {
                Log.Error_NoCallerPrefix($"Failed to deserialize user data: {e}");
                return GetUsersResponse.Empty;
            }

            return getUsersResponse;
        }
    }
}
