using RiskOfChaos.Utilities;
using RiskOfChaos.Utilities.PersistentSaveData;
using RoR2;
using RoR2.UI;
using System;
using System.IO;

namespace RiskOfChaos.Twitch
{
    static class OldTwitchLoginHandler
    {
        [SystemInitializer(typeof(TwitchAuthenticationTokenStorage))]
        static void Init()
        {
            string oauthSaveFile = PersistentSaveDataManager.GetSaveFilePath("f100264c-5e84-4a19-a3e2-02a2e3d80469");
            if (File.Exists(oauthSaveFile))
            {
                if (!TwitchAuthenticationTokenStorage.HasStoredToken)
                {
                    PopupAlertQueue.EnqueueAlert(dialogBox =>
                    {
                        dialogBox.headerToken = new SimpleDialogBox.TokenParamsPair("POPUP_TWITCH_LOGIN_OAUTH_NO_LONGER_USED_HEADER");
                        dialogBox.descriptionToken = new SimpleDialogBox.TokenParamsPair("POPUP_TWITCH_LOGIN_OAUTH_NO_LONGER_USED_DESCRIPTION");

                        dialogBox.AddCancelButton(CommonLanguageTokens.ok);
                    });
                }

                try
                {
                    File.Delete(oauthSaveFile);
                }
                catch (Exception e)
                {
                    Log.Error_NoCallerPrefix($"Failed to delete old oauth file: {e}");
                }
            }
        }
    }
}
