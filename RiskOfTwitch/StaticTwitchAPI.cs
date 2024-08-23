using Newtonsoft.Json;
using RiskOfTwitch.Chat.Poll;
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

            using HttpResponseMessage getUsersResponseMessage = await client.GetAsync($"https://api.twitch.tv/helix/users?{query}", cancellationToken).ConfigureAwait(false);
            if (!getUsersResponseMessage.IsSuccessStatusCode)
            {
                Log.Error($"Twitch API responded with error code {getUsersResponseMessage.StatusCode:D} {getUsersResponseMessage.ReasonPhrase}");
                return GetUsersResponse.Empty;
            }

            GetUsersResponse getUsersResponse;
            try
            {
                getUsersResponse = JsonConvert.DeserializeObject<GetUsersResponse>(await getUsersResponseMessage.Content.ReadAsStringAsync().ConfigureAwait(false));
            }
            catch (JsonException e)
            {
                Log.Error_NoCallerPrefix($"Failed to deserialize user data: {e}");
                return GetUsersResponse.Empty;
            }

            return getUsersResponse;
        }

        public static async Task<Result<PollData>> CreatePoll(CreatePollArgs createArgs, CancellationToken cancellationToken = default)
        {
            if (!createArgs.Validate(out Exception argsValidationException))
            {
                return new Result<PollData>(argsValidationException);
            }

            using HttpClient client = new HttpClient();

            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {createArgs.AccessToken}");
            client.DefaultRequestHeaders.Add("Client-Id", Authentication.CLIENT_ID);

            using HttpContent content = createArgs.GetHttpContent();

            using HttpResponseMessage httpResponse = await client.PostAsync($"https://api.twitch.tv/helix/polls", content, cancellationToken).ConfigureAwait(false);

            if (!httpResponse.IsSuccessStatusCode)
            {
                string message = $"{httpResponse.StatusCode:D} {httpResponse.ReasonPhrase}";

                if (httpResponse.Content != null)
                {
                    message += $", Content='{await httpResponse.Content.ReadAsStringAsync().ConfigureAwait(false)}'";
                }

                return new Result<PollData>(new HttpRequestException(message));
            }

            CreatePollResponse response;
            try
            {
                response = JsonConvert.DeserializeObject<CreatePollResponse>(await httpResponse.Content.ReadAsStringAsync().ConfigureAwait(false));
            }
            catch (JsonException e)
            {
                return new Result<PollData>(e);
            }

            if (response == null || response.Polls.Length <= 0)
            {
                return new Result<PollData>(new Exception("Create poll request result is OK, but no poll data was returned"));
            }

            return response.Polls[0];
        }

        public static async Task<Result<GetPollsResponse>> GetPolls(GetPollsArgs args, CancellationToken cancellationToken = default)
        {
            using HttpClient client = new HttpClient();

            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {args.AccessToken}");
            client.DefaultRequestHeaders.Add("Client-Id", Authentication.CLIENT_ID);

            using HttpResponseMessage httpResponse = await client.GetAsync(args.GetRequestUri(), cancellationToken).ConfigureAwait(false);

            if (!httpResponse.IsSuccessStatusCode)
            {
                string message = $"{httpResponse.StatusCode:D} {httpResponse.ReasonPhrase}";

                if (httpResponse.Content != null)
                {
                    message += $", Content='{await httpResponse.Content.ReadAsStringAsync().ConfigureAwait(false)}'";
                }

                return new Result<GetPollsResponse>(new HttpRequestException(message));
            }

            GetPollsResponse response;
            try
            {
                response = JsonConvert.DeserializeObject<GetPollsResponse>(await httpResponse.Content.ReadAsStringAsync().ConfigureAwait(false));
            }
            catch (JsonException e)
            {
                return new Result<GetPollsResponse>(e);
            }

            if (response == null || response.Polls.Length <= 0)
            {
                return new Result<GetPollsResponse>(new Exception("Get poll request result is OK, but no poll data was returned"));
            }

            return response;
        }

        public static async Task<Result<PollData>> EndPoll(EndPollArgs args, CancellationToken cancellationToken = default)
        {
            using HttpClient client = new HttpClient();

            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {args.AccessToken}");
            client.DefaultRequestHeaders.Add("Client-Id", Authentication.CLIENT_ID);

            using HttpRequestMessage requestMessage = new HttpRequestMessage(new HttpMethod("PATCH"), "https://api.twitch.tv/helix/polls")
            {
                Content = args.GetRequestContent()
            };

            using HttpResponseMessage httpResponse = await client.SendAsync(requestMessage, cancellationToken).ConfigureAwait(false);
            if (!httpResponse.IsSuccessStatusCode)
            {
                string message = $"{httpResponse.StatusCode:D} {httpResponse.ReasonPhrase}";

                if (httpResponse.Content != null)
                {
                    message += $", Content='{await httpResponse.Content.ReadAsStringAsync().ConfigureAwait(false)}'";
                }

                return new Result<PollData>(new HttpRequestException(message));
            }

            EndPollResponse response;
            try
            {
                response = JsonConvert.DeserializeObject<EndPollResponse>(await httpResponse.Content.ReadAsStringAsync().ConfigureAwait(false));
            }
            catch (JsonException e)
            {
                return new Result<PollData>(e);
            }

            if (response == null || response.Polls.Length <= 0)
            {
                return new Result<PollData>(new Exception("End poll request result is OK, but no poll data was returned"));
            }

            return response.Polls[0];
        }
    }
}
