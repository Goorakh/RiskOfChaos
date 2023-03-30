using RiskOfChaos.Trackers;
using RiskOfChaos.Utilities;
using RoR2;
using RoR2.UI;
using System.Linq;
using UnityEngine;

namespace RiskOfChaos.Patches
{
    internal static class FixPlayerChatOnRespawn
    {
        internal static void Apply()
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
                chatBoxTracker.ChatBox.Invoke(nameof(ChatBox.ScrollToBottom), 0.1f);
            }

#if DEBUG
            Log.Debug($"Fixed chat scroll on respawn for {body.GetUserName()}");
#endif
        }
    }
}
