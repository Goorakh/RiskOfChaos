using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace RiskOfTwitch
{
    public class Authentication
    {
        public const string CLIENT_ID = "2h96zmad9nhz11unv407c9ou6i6ofj";

        public const string AUTH_REDIRECT_URL = "http://localhost:4000/roc/oauth/redirect";

        static readonly byte[] _authRedirectResponseBytes = Encoding.ASCII.GetBytes("""
                     <!DOCTYPE html>
                     <html lang="en">
                     <head>
                         <meta charset="UTF-8">
                         <meta name="viewport" content="width=device-width, initial-scale=1.0">
                     </head>
                     <body>
                     Authentication complete. You may close this window.
                        <script>
                            var url = window.location;
                            url.replace(window.location.hash, "");
                            fetch(url, {
                               method: 'GET',
                               headers: {
                                  'fragment': window.location.hash
                               }
                            });
                        </script>
                     </body>
                     """);

        public static string CreateAuthorizeUrl(string scopes, out string authState)
        {
            using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
            {
                const int AUTH_STATE_LENGTH = 16;

                byte[] rngBytes = new byte[AUTH_STATE_LENGTH];
                rng.GetBytes(rngBytes);

                StringBuilder authStateBuilder = new StringBuilder(AUTH_STATE_LENGTH * 2);
                for (int i = 0; i < AUTH_STATE_LENGTH; i++)
                {
                    authStateBuilder.Append(rngBytes[i].ToString("x2"));
                }

                authState = authStateBuilder.ToString();
            }

            return $"https://id.twitch.tv/oauth2/authorize?response_type=token&client_id={CLIENT_ID}&redirect_uri={AUTH_REDIRECT_URL}&scope={HttpUtility.UrlEncode(scopes)}&state={authState}";
        }

        public static async Task<string> AuthenticateUserAccessToken(string scopes, CancellationToken cancellationToken = default)
        {
            string authorizeUrl = CreateAuthorizeUrl(scopes, out string authState);

            Process.Start(new ProcessStartInfo
            {
                FileName = authorizeUrl,
                UseShellExecute = true
            });

            using (HttpListener httpListener = new HttpListener())
            {
                httpListener.Prefixes.Add(AUTH_REDIRECT_URL + "/");
                httpListener.Start();

                HttpListenerContext context = await httpListener.GetContextAsync();

                context.Response.ContentType = "text/html";
                context.Response.ContentEncoding = Encoding.ASCII;
                context.Response.ContentLength64 = _authRedirectResponseBytes.LongLength;

                await context.Response.OutputStream.WriteAsync(_authRedirectResponseBytes, 0, _authRedirectResponseBytes.Length, cancellationToken);

                context.Response.Close();

                context = await httpListener.GetContextAsync();

                string fragmentHeader = context.Request.Headers["fragment"];
                Uri url = context.Request.Url;

                Dictionary<string, string> queries = [];
                if (url != null)
                {
                    string query = url.Query;
                    if (!string.IsNullOrEmpty(query))
                    {
                        if (query[0] == '?')
                        {
                            // Remove leading '?' character
                            query = query.Substring(1);
                        }

                        UrlUtils.SplitUrlQueries(query, queries);
                    }
                }

                Dictionary<string, string> fragments = [];
                if (!string.IsNullOrEmpty(fragmentHeader))
                {
                    if (fragmentHeader[0] == '#')
                    {
                        // Remove leading '#' character
                        fragmentHeader = fragmentHeader.Substring(1);
                    }

                    UrlUtils.SplitUrlQueries(fragmentHeader, fragments);
                }

                context.Response.StatusCode = (int)HttpStatusCode.OK;
                context.Response.KeepAlive = false;
                context.Response.Close();

                if (!fragments.TryGetValue("state", out string receivedAuthState) || !string.Equals(authState, receivedAuthState))
                {
                    Log.Error("Invalid auth state");
                    return null;
                }

                if (!fragments.TryGetValue("access_token", out string accessToken))
                {
                    Log.Error("No access token was received");
                    return null;
                }

                return accessToken;
            }
        }
    }
}
