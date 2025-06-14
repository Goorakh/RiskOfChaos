using HG;
using RiskOfChaos.Patches;
using RoR2;
using RoR2.UI;
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

                CurrentDifficultyIconControllerTracker tracker = self.gameObject.EnsureComponent<CurrentDifficultyIconControllerTracker>();
                tracker.IconController = self;
            };
        }

        void OnEnable()
        {
            InstanceTracker.Add(this);

            DifficultyChangedHook.OnRunDifficultyChanged += OnRunDifficultyChanged;
        }

        void OnDisable()
        {
            InstanceTracker.Remove(this);

            DifficultyChangedHook.OnRunDifficultyChanged -= OnRunDifficultyChanged;
        }

        void OnRunDifficultyChanged()
        {
            // Refreshes the icon based on the current difficulty
            IconController.Start();
        }
    }
}
