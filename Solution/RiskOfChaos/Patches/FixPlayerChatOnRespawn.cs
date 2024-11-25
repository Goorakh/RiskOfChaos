using RiskOfChaos.Trackers;
using RoR2;
using RoR2.UI;

namespace RiskOfChaos.Patches
{
    internal static class FixPlayerChatOnRespawn
    {
        [SystemInitializer]
        static void Init()
        {
            CharacterBody.onBodyStartGlobal += CharacterBody_onBodyStartGlobal;
        }

        static void CharacterBody_onBodyStartGlobal(CharacterBody body)
        {
            CharacterMaster master = body.master;
            if (!master)
                return;

            if (!master.playerCharacterMasterController)
                return;

            bool anyChatBoxFound = false;
            foreach (ChatBoxTracker chatBoxTracker in InstanceTracker.GetInstancesList<ChatBoxTracker>())
            {
                HUD hud = chatBoxTracker.OwnerHUD;
                if (!hud)
                    continue;

                LocalUser viewer = hud.localUserViewer;
                if (viewer is null)
                    continue;

                CharacterMaster viewerMaster = viewer.cachedMaster;
                if (!viewerMaster)
                    continue;

                if (viewerMaster == master)
                {
                    // Fix chat scroll
                    chatBoxTracker.ChatBox.Invoke(nameof(ChatBox.ScrollToBottom), 0.5f);

                    anyChatBoxFound = true;
                }
            }

            if (anyChatBoxFound)
            {
                Log.Debug($"Fixed chat scroll on respawn for {body.GetUserName()}");
            }
        }
    }
}
