using RiskOfChaos.Components;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectComponents;
using RiskOfChaos.Trackers;
using RoR2;
using RoR2.UI;
using UnityEngine;

namespace RiskOfChaos.EffectDefinitions.UI
{
    [ChaosTimedEffect("hide_healthbar", 60f, AllowDuplicates = false)]
    public sealed class HideHealthbar : MonoBehaviour
    {
        ChaosEffectComponent _effectComponent;

        void Awake()
        {
            _effectComponent = GetComponent<ChaosEffectComponent>();
        }

        void Start()
        {
            foreach (HealthBarTracker healthBarTracker in InstanceTracker.GetInstancesList<HealthBarTracker>())
            {
                if (healthBarTracker)
                {
                    hideHealthBar(healthBarTracker);
                }
            }

            foreach (HUDBossHealthBarControllerTracker bossHealthBarControllerTracker in InstanceTracker.GetInstancesList<HUDBossHealthBarControllerTracker>())
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
            healthBarHider.OwnerEffectComponent = _effectComponent;
        }
    }
}
