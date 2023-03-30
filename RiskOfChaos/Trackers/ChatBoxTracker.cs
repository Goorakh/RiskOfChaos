using RoR2;
using RoR2.UI;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace RiskOfChaos.Trackers
{
    public class ChatBoxTracker : MonoBehaviour
    {
        public ChatBox ChatBox { get; private set; }

        [SystemInitializer]
        static void Init()
        {
            On.RoR2.UI.ChatBox.Awake += (orig, self) =>
            {
                orig(self);

                if (!self.GetComponent<ChatBoxTracker>())
                {
                    ChatBoxTracker chatBoxTracker = self.gameObject.AddComponent<ChatBoxTracker>();
                    chatBoxTracker.ChatBox = self;
                }
            };
        }

        void OnEnable()
        {
            InstanceTracker.Add(this);
        }

        void OnDisable()
        {
            InstanceTracker.Remove(this);
        }
    }
}
