using RiskOfChaos.Collections;
using RiskOfChaos.Components;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.Trackers;
using RoR2;
using RoR2.UI;
using System.Collections.Generic;
using UnityEngine;

namespace RiskOfChaos.EffectDefinitions.UI
{
    [ChaosTimedEffect("hide_healthbar", 60f, AllowDuplicates = false)]
    public sealed class HideHealthbar : MonoBehaviour
    {
        readonly ClearingObjectList<KeepDisabled> _healthBarHiderComponents = new ClearingObjectList<KeepDisabled>()
        {
            DontUseDestroyEvent = true,
            AutoClearInterval = 10f,
            ObjectIdentifier = "HealthBarHider"
        };

        void Start()
        {
            List<HealthBarTracker> healthBarTrackers = InstanceTracker.GetInstancesList<HealthBarTracker>();
            List<HUDBossHealthBarControllerTracker> bossHealthBarControllerTrackers = InstanceTracker.GetInstancesList<HUDBossHealthBarControllerTracker>();

            int currentHealthBarsCount = healthBarTrackers.Count + bossHealthBarControllerTrackers.Count;

            _healthBarHiderComponents.EnsureCapacity(currentHealthBarsCount);

            foreach (HealthBarTracker healthBarTracker in healthBarTrackers)
            {
                if (healthBarTracker)
                {
                    hideHealthBar(healthBarTracker);
                }
            }

            foreach (HUDBossHealthBarControllerTracker bossHealthBarControllerTracker in bossHealthBarControllerTrackers)
            {
                if (bossHealthBarControllerTracker)
                {
                    hideBossHealthBar(bossHealthBarControllerTracker);
                }
            }

            HealthBarTracker.OnHealthBarAwakeGlobal += hideHealthBar;
            HUDBossHealthBarControllerTracker.OnHUDBossHealthBarControllerAwakeGlobal += hideBossHealthBar;
        }

        void OnDestroy()
        {
            HealthBarTracker.OnHealthBarAwakeGlobal -= hideHealthBar;
            HUDBossHealthBarControllerTracker.OnHUDBossHealthBarControllerAwakeGlobal -= hideBossHealthBar;

            _healthBarHiderComponents.ClearAndDispose(true);
        }

        void hideHealthBar(HealthBarTracker healthBarTracker)
        {
            HealthBar healthBar = healthBarTracker.HealthBar;
            if (!healthBar)
                return;

            disableHealthBarObject(healthBar.gameObject);
        }

        void hideBossHealthBar(HUDBossHealthBarControllerTracker bossHealthBarControllerTracker)
        {
            disableHealthBarObject(bossHealthBarControllerTracker.HealthBarRoot);
        }

        void disableHealthBarObject(GameObject healthBarRoot)
        {
            if (!healthBarRoot)
                return;

            KeepDisabled healthBarHider = healthBarRoot.AddComponent<KeepDisabled>();
            _healthBarHiderComponents.Add(healthBarHider);
        }
    }
}
