using System;
using System.Collections.Generic;
using System.IO;
using TwitchLib.Client.Models;
using TwitchLib.Client.Models.Builders;

namespace RiskOfChaos.EffectHandling.Controllers.ChatVoting.Twitch
{
    public readonly struct TwitchLoginCredentials : IEquatable<TwitchLoginCredentials>
    {
        public static readonly TwitchLoginCredentials Empty = new TwitchLoginCredentials(string.Empty, string.Empty);

        const string LOGIN_FILE_NAME = "twitch_login.txt";
        static readonly string _saveFilePath = Path.Combine(Main.ModDirectory, LOGIN_FILE_NAME);

        const string FILE_USERNAME_PREFIX = "username:";
        const string FILE_OAUTH_PREFIX = "oauth:";

        public readonly string Username;
        public readonly string OAuth;

        static string formatOAuthToken(string oauth)
        {
            const string OAUTH_PREFIX = "oauth:";
            if (oauth.StartsWith(OAUTH_PREFIX))
            {
                return oauth.Substring(OAUTH_PREFIX.Length);
            }
            else
            {
                return oauth;
            }
        }

        public TwitchLoginCredentials(string username, string oauth)
        {
            Username = username;
            OAuth = formatOAuthToken(oauth);
        }

        TwitchLoginCredentials(string[] fileContents)
        {
            if (fileContents.Length != 2)
            {
                throw new ArgumentException("File contents must be 2 lines");
            }

            foreach (string line in fileContents)
            {
                if (line.StartsWith(FILE_USERNAME_PREFIX))
                {
                    Username = line.Substring(FILE_USERNAME_PREFIX.Length);
                }
                else if (line.StartsWith(FILE_OAUTH_PREFIX))
                {
                    OAuth = formatOAuthToken(line.Substring(FILE_OAUTH_PREFIX.Length));
                }
            }
        }

        public static TwitchLoginCredentials TryReadFromFile()
        {
            if (File.Exists(_saveFilePath))
            {
#if DEBUG
                Log.Debug($"Reading login file {_saveFilePath}");
#endif

                try
                {
                    return new TwitchLoginCredentials(File.ReadAllLines(_saveFilePath));
                }
                catch (Exception e)
                {
                    Log.Error_NoCallerPrefix($"Unable to read {LOGIN_FILE_NAME}: {e}");
                }
            }

#if DEBUG
            Log.Debug($"No valid login file found at {_saveFilePath}");
#endif

            return Empty;
        }

        public readonly bool IsValid()
        {
            return !string.IsNullOrWhiteSpace(Username) && !string.IsNullOrWhiteSpace(OAuth);
        }

        public readonly void WriteToFile()
        {
            try
            {
                File.WriteAllLines(_saveFilePath, new string[]
                {
                    FILE_USERNAME_PREFIX + Username,
                    FILE_OAUTH_PREFIX + OAuth
                });
            }
            catch (Exception e)
            {
                Log.Error_NoCallerPrefix($"Unable to save twitch login info: {e}");
            }
        }

        public readonly ConnectionCredentials BuildConnectionCredentials()
        {
            return ConnectionCredentialsBuilder.Create()
                                               .WithTwitchUsername(Username)
                                               .WithTwitchOAuth(OAuth)
                                               .Build();
        }

        public override bool Equals(object obj)
        {
            return obj is TwitchLoginCredentials credentials && Equals(credentials);
        }

        public bool Equals(TwitchLoginCredentials other)
        {
            return Username == other.Username &&
                   OAuth == other.OAuth;
        }

        public override int GetHashCode()
        {
            int hashCode = 388668885;
            hashCode = (hashCode * -1521134295) + EqualityComparer<string>.Default.GetHashCode(Username);
            hashCode = (hashCode * -1521134295) + EqualityComparer<string>.Default.GetHashCode(OAuth);
            return hashCode;
        }

        public static bool operator ==(TwitchLoginCredentials left, TwitchLoginCredentials right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(TwitchLoginCredentials left, TwitchLoginCredentials right)
        {
            return !(left == right);
        }
    }
}
