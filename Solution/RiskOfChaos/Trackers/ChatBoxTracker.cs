using RiskOfChaos.Utilities.Extensions;
using RoR2;
using RoR2.UI;
using UnityEngine;

namespace RiskOfChaos.Trackers
{
    public class ChatBoxTracker : MonoBehaviour
    {
        [SystemInitializer]
        static void Init()
        {
            On.RoR2.UI.ChatBox.Awake += (orig, self) =>
            {
                orig(self);

                ChatBoxTracker chatBoxTracker = self.gameObject.EnsureComponent<ChatBoxTracker>();
                chatBoxTracker.ChatBox = self;
            };
        }

        public ChatBox ChatBox { get; private set; }

        public HUD OwnerHUD { get; private set; }

        void refreshOwnerHUD()
        {
            OwnerHUD = GetComponentInParent<HUD>();
        }

        void OnTransformParentChanged()
        {
            refreshOwnerHUD();
        }

        void OnEnable()
        {
            InstanceTracker.Add(this);
            refreshOwnerHUD();
        }

        void OnDisable()
        {
            InstanceTracker.Remove(this);
        }
    }
}
