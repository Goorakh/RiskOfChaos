using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.Trackers;
using RoR2;
using RoR2.UI;
using UnityEngine;

namespace RiskOfChaos.EffectDefinitions.UI
{
    [ChaosTimedEffect("hide_healthbar", 60f, AllowDuplicates = false, IsNetworked = true)]
    public sealed class HideHealthbar : TimedEffect
    {
        public override void OnStart()
        {
            RoR2Application.onFixedUpdate += onFixedUpdate;
            setHealthbarsActive(false);
        }

        public override void OnEnd()
        {
            RoR2Application.onFixedUpdate -= onFixedUpdate;
            setHealthbarsActive(true);
        }

        static void onFixedUpdate()
        {
            setHealthbarsActive(false);
        }

        static void setHealthbarsActive(bool active)
        {
            foreach (HealthBarTracker healthBarTracker in InstanceTracker.GetInstancesList<HealthBarTracker>())
            {
                HealthBar healthBar = healthBarTracker.HealthBar;
                if (healthBar)
                {
                    healthBar.gameObject.SetActive(active);
                }
            }

            foreach (HUDBossHealthBarControllerTracker hudBossHealthBarControllerTracker in InstanceTracker.GetInstancesList<HUDBossHealthBarControllerTracker>())
            {
                GameObject bossHealthBarRoot = hudBossHealthBarControllerTracker.HealthBarRoot;
                if (bossHealthBarRoot)
                {
                    bossHealthBarRoot.SetActive(active);
                }
            }
        }
    }
}
