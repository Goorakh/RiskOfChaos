using RiskOfChaos.Trackers;
using RiskOfChaos.Utilities;
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
            if (!body.isPlayerControlled)
                return;

            if (!body.master || body.master != PlayerUtils.GetLocalUserMaster())
                return;

            foreach (ChatBoxTracker chatBoxTracker in InstanceTracker.GetInstancesList<ChatBoxTracker>())
            {
                // Fix chat scroll
                chatBoxTracker.ChatBox.Invoke(nameof(ChatBox.ScrollToBottom), 0.5f);
            }

#if DEBUG
            Log.Debug($"Fixed chat scroll on respawn for {body.GetUserName()}");
#endif
        }
    }
}
