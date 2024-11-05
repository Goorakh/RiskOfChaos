using Newtonsoft.Json;
using RiskOfTwitch.User;
using System;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RiskOfTwitch
{
    public static class StaticTwitchAPI
    {
        public static async Task<Result<GetUsersResponse>> GetUsers(string accessToken, string[] userIds, string[] usernames, CancellationToken cancellationToken = default)
        {
            userIds ??= [];
            usernames ??= [];

            int combinedCount = userIds.Length + usernames.Length;

            if (combinedCount == 0)
                return GetUsersResponse.Empty;

            if (combinedCount > 100)
            {
                return new Result<GetUsersResponse>(new ArgumentOutOfRangeException($"{nameof(userIds)}, {nameof(usernames)}", "Combined size of user ids and usernames cannot exceed 100"));
            }

            using HttpClient client = new HttpClient();

            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");
            client.DefaultRequestHeaders.Add("Client-Id", Authentication.CLIENT_ID);

            StringBuilder queryBuilder = new StringBuilder(combinedCount * 25);
            foreach (string userId in userIds)
            {
                if (queryBuilder.Length > 0)
                    queryBuilder.Append('&');

                queryBuilder.AppendFormat("id={0}", userId);
            }

            foreach (string username in usernames)
            {
                if (queryBuilder.Length > 0)
                    queryBuilder.Append('&');

                queryBuilder.AppendFormat("login={0}", username);
            }

            using HttpResponseMessage getUsersResponseMessage = await client.GetAsync($"https://api.twitch.tv/helix/users?{queryBuilder}", cancellationToken).ConfigureAwait(false);
            if (!getUsersResponseMessage.IsSuccessStatusCode)
            {
                return new Result<GetUsersResponse>(new HttpResponseException(getUsersResponseMessage));
            }

            GetUsersResponse getUsersResponse;
            try
            {
                getUsersResponse = JsonConvert.DeserializeObject<GetUsersResponse>(await getUsersResponseMessage.Content.ReadAsStringAsync().ConfigureAwait(false));
            }
            catch (JsonException e)
            {
                return new Result<GetUsersResponse>(e);
            }

            return getUsersResponse;
        }
    }
}
