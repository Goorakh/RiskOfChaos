using RiskOfChaos.Utilities.PersistentSaveData;
using RoR2;
using System;
using System.IO;
using System.Text;

namespace RiskOfChaos.Twitch
{
    static class TwitchAuthenticationTokenStorage
    {
        const uint CURRENT_TOKEN_STORE_FORMAT_VERSION = 0;

        const string TOKEN_STORE_FILE_NAME = "twitch.uat";

        static readonly string _tokenStoreFilePath = PersistentSaveDataManager.GetSaveFilePath(TOKEN_STORE_FILE_NAME);

        public static bool HasStoredToken { get; private set; }

        [SystemInitializer]
        static void Init()
        {
            HasStoredToken = tryLoadAccessToken(out TwitchUserAccessToken storedAccessToken);
            if (HasStoredToken)
            {
                Log.Debug("Loaded access token from file");

                TwitchAuthenticationManager.SetTokenFromFile(storedAccessToken);
            }

            TwitchAuthenticationManager.OnAccessTokenChanged += TwitchAuthenticationManager_OnAccessTokenChanged;
        }

        static void TwitchAuthenticationManager_OnAccessTokenChanged()
        {
            storeAccessToken(TwitchAuthenticationManager.CurrentAccessToken);
        }

        static bool tryLoadAccessToken(out TwitchUserAccessToken accessToken)
        {
            if (!File.Exists(_tokenStoreFilePath))
            {
                accessToken = TwitchUserAccessToken.Empty;
                return false;
            }

            using FileStream fs = new FileStream(_tokenStoreFilePath, FileMode.Open, FileAccess.Read);
            using BinaryReader reader = new BinaryReader(fs, Encoding.ASCII);

            string scopes;
            string token;
            try
            {
                uint version = reader.ReadUInt32();

                scopes = reader.ReadString();
                token = reader.ReadString();
            }
            catch (Exception e)
            {
                Log.Error_NoCallerPrefix($"Failed to read access token from file: {e}");

                accessToken = TwitchUserAccessToken.Empty;
                return false;
            }

            accessToken = new TwitchUserAccessToken(scopes, token);
            return true;
        }

        static void storeAccessToken(TwitchUserAccessToken accessToken)
        {
            if (accessToken.IsEmpty)
            {
                if (File.Exists(_tokenStoreFilePath))
                    File.Delete(_tokenStoreFilePath);

                HasStoredToken = false;

                return;
            }

            using (FileStream fs = File.Open(_tokenStoreFilePath, FileMode.Create, FileAccess.Write))
            {
                using BinaryWriter writer = new BinaryWriter(fs, Encoding.ASCII);

                writer.Write(CURRENT_TOKEN_STORE_FORMAT_VERSION);

                writer.Write(accessToken.Scopes);
                writer.Write(accessToken.Token);
            }

            HasStoredToken = true;
        }
    }
}
