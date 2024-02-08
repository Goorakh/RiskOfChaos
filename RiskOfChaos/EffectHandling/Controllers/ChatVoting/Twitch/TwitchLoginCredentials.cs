using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using TwitchLib.Client.Models;
using TwitchLib.Client.Models.Builders;

namespace RiskOfChaos.EffectHandling.Controllers.ChatVoting.Twitch
{
    public readonly struct TwitchLoginCredentials : IEquatable<TwitchLoginCredentials>
    {
        public static readonly TwitchLoginCredentials Empty = new TwitchLoginCredentials(string.Empty, string.Empty);

        const string LOGIN_FILE_NAME = "f100264c-5e84-4a19-a3e2-02a2e3d80469";
        static readonly string _saveFilePath = Path.Combine(Main.PersistentSaveDataDirectory, LOGIN_FILE_NAME);

        static readonly Encoding _saveFileEncoding = Encoding.ASCII;

        public readonly string Username;
        public readonly string OAuth;

        public readonly ConnectionCredentials ConnectionCredentials;

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

            ConnectionCredentials = ConnectionCredentialsBuilder.Create()
                                                                .WithTwitchUsername(Username)
                                                                .WithTwitchOAuth(OAuth)
                                                                .WithDisableUsernameCheck(true)
                                                                .Build();
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
                    string fileContents = File.ReadAllText(_saveFilePath, _saveFileEncoding);
                    byte[] rawBytes = Convert.FromBase64String(fileContents);

                    using MemoryStream fileBytesStream = new MemoryStream(rawBytes);
                    using BinaryReader reader = new BinaryReader(fileBytesStream, _saveFileEncoding);

                    string username = reader.ReadString();
                    string oauth = reader.ReadString();

                    return new TwitchLoginCredentials(username, oauth);
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
            if (!IsValid())
            {
                if (File.Exists(_saveFilePath))
                {
                    try
                    {
                        File.Delete(_saveFilePath);
                    }
                    catch (Exception ex)
                    {
                        Log.Error_NoCallerPrefix($"Unable to remove invalid login file data: {ex}");
                    }
                }

                return;
            }

            try
            {
                using MemoryStream stream = new MemoryStream(_saveFileEncoding.GetMaxByteCount(Username.Length + OAuth.Length));
                using BinaryWriter writer = new BinaryWriter(stream, _saveFileEncoding);

                writer.Write(Username);
                writer.Write(OAuth);

                string fileContents = Convert.ToBase64String(stream.ToArray());

                File.WriteAllText(_saveFilePath, fileContents, _saveFileEncoding);

#if DEBUG
                Log.Debug($"Saved login info to {_saveFilePath}");
#endif
            }
            catch (Exception e)
            {
                Log.Error_NoCallerPrefix($"Unable to save twitch login info: {e}");

                // Prevent bad/old data from being stored in file
                if (File.Exists(_saveFilePath))
                {
                    try
                    {
                        File.Delete(_saveFilePath);
                    }
                    catch (Exception ex)
                    {
                        Log.Error_NoCallerPrefix($"Unable to remove bad login file data: {ex}");
                    }
                }
            }
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
