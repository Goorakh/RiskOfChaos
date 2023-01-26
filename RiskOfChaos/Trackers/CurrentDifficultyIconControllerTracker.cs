using RiskOfChaos.Networking;
using RoR2;
using RoR2.UI;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace RiskOfChaos.Trackers
{
    public class CurrentDifficultyIconControllerTracker : MonoBehaviour
    {
        public CurrentDifficultyIconController IconController { get; private set; }

        [SystemInitializer]
        static void Init()
        {
            On.RoR2.UI.CurrentDifficultyIconController.Start += static (orig, self) =>
            {
                orig(self);

                if (!self.GetComponent<CurrentDifficultyIconControllerTracker>())
                {
                    CurrentDifficultyIconControllerTracker tracker = self.gameObject.AddComponent<CurrentDifficultyIconControllerTracker>();
                    tracker.IconController = (CurrentDifficultyIconController)self;
                }
            };
        }

        void OnEnable()
        {
            InstanceTracker.Add(this);

            RefreshDifficultyIconsMessage.OnReceived += RefreshDifficultyIconsMessage_OnReceived;
        }

        void OnDisable()
        {
            InstanceTracker.Remove(this);

            RefreshDifficultyIconsMessage.OnReceived -= RefreshDifficultyIconsMessage_OnReceived;
        }

        void RefreshDifficultyIconsMessage_OnReceived()
        {
#pragma warning disable Publicizer001 // Accessing a member that was not originally public
            // Refreshes the icon based on the current difficulty
            IconController.Start();
#pragma warning restore Publicizer001 // Accessing a member that was not originally public
        }
    }
}
