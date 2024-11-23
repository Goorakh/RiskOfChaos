using RiskOfChaos.Components;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.Trackers;
using RiskOfChaos.Utilities;
using RiskOfChaos.Utilities.Extensions;
using RoR2;
using System.Collections.Generic;
using UnityEngine;

namespace RiskOfChaos.EffectDefinitions.UI
{
    [ChaosTimedEffect("hide_healthbar", 60f, AllowDuplicates = false)]
    public sealed class HideHealthbar : MonoBehaviour
    {
        readonly List<KeepDisabled> _healthBarHiderComponents = [];

        float _healthBarHiderDestroyedCheckTimer = 0f;

        void Start()
        {
            List<HealthBarTracker> healthBarTrackers = InstanceTracker.GetInstancesList<HealthBarTracker>();
            List<HUDBossHealthBarControllerTracker> bossHealthBarControllerTrackers = InstanceTracker.GetInstancesList<HUDBossHealthBarControllerTracker>();

            int currentHealthBarsCount = healthBarTrackers.Count + bossHealthBarControllerTrackers.Count;

            _healthBarHiderComponents.EnsureCapacity(currentHealthBarsCount);

            foreach (HealthBarTracker healthBarTracker in healthBarTrackers)
            {
                hideHealthBar(healthBarTracker);
            }

            foreach (HUDBossHealthBarControllerTracker bossHealthBarControllerTracker in bossHealthBarControllerTrackers)
            {
                hideBossHealthBar(bossHealthBarControllerTracker);
            }

            HealthBarTracker.OnHealthBarAwakeGlobal += hideHealthBar;
            HUDBossHealthBarControllerTracker.OnHUDBossHealthBarControllerAwakeGlobal += hideBossHealthBar;
        }

        void FixedUpdate()
        {
            _healthBarHiderDestroyedCheckTimer -= Time.fixedDeltaTime;
            if (_healthBarHiderDestroyedCheckTimer <= 0f)
            {
                _healthBarHiderDestroyedCheckTimer = 5f;

                int removedHealthBarHiders = UnityObjectUtils.RemoveAllDestroyed(_healthBarHiderComponents);
                if (removedHealthBarHiders > 0)
                {
                    Log.Debug($"Cleared {removedHealthBarHiders} destroyed health bar hider(s)");
                }
            }
        }

        void OnDestroy()
        {
            HealthBarTracker.OnHealthBarAwakeGlobal -= hideHealthBar;
            HUDBossHealthBarControllerTracker.OnHUDBossHealthBarControllerAwakeGlobal -= hideBossHealthBar;

            foreach (KeepDisabled healthBarHider in _healthBarHiderComponents)
            {
                if (healthBarHider)
                {
                    Destroy(healthBarHider);
                    healthBarHider.gameObject.SetActive(true);
                }
            }

            _healthBarHiderComponents.Clear();
        }

        void hideHealthBar(HealthBarTracker healthBarTracker)
        {
            disableHealthBarObject(healthBarTracker.HealthBar.gameObject);
        }

        void hideBossHealthBar(HUDBossHealthBarControllerTracker bossHealthBarControllerTracker)
        {
            disableHealthBarObject(bossHealthBarControllerTracker.HealthBarRoot);
        }

        void disableHealthBarObject(GameObject healthBarRoot)
        {
            KeepDisabled healthBarHider = healthBarRoot.AddComponent<KeepDisabled>();
            _healthBarHiderComponents.Add(healthBarHider);
        }
    }
}
