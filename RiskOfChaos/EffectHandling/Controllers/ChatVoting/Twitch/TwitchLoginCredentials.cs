using System;
using System.IO;

namespace RiskOfChaos.EffectHandling.Controllers.ChatVoting.Twitch
{
    public readonly struct TwitchLoginCredentials
    {
        public static readonly TwitchLoginCredentials Empty = new TwitchLoginCredentials(string.Empty, string.Empty);

        const string LOGIN_FILE_NAME = "twitch_login.txt";
        static readonly string _saveFilePath = Path.Combine(new FileInfo(Main.Instance.Info.Location).Directory.FullName, LOGIN_FILE_NAME);

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
                    Log.Error($"Unable to read {LOGIN_FILE_NAME}: {e}");
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
            File.WriteAllLines(_saveFilePath, new string[]
            {
                FILE_USERNAME_PREFIX + Username,
                FILE_OAUTH_PREFIX + OAuth
            });
        }
    }
}
