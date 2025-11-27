using HG;
using RoR2;
using RoR2.UI;
using System;
using UnityEngine;

namespace RiskOfChaos.Trackers
{
    public sealed class HudTracker : MonoBehaviour
    {
        [SystemInitializer]
        static void Init()
        {
            HUD.onHudTargetChangedGlobal += HUD_onHudTargetChangedGlobal;
        }

        static void HUD_onHudTargetChangedGlobal(HUD hud)
        {
            HudTracker tracker = hud.gameObject.EnsureComponent<HudTracker>();
            tracker.Hud = hud;
        }

        public static event Action<HUD> OnHudStartGlobal;

        public HUD Hud { get; private set; }

        void Start()
        {
            if (Hud)
            {
                OnHudStartGlobal?.Invoke(Hud);
            }
        }
    }
}
