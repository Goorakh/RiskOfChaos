using RiskOfChaos.EffectHandling;
using RiskOfChaos.EffectHandling.Controllers;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Data;
using RoR2;
using UnityEngine;

namespace RiskOfChaos.EffectDefinitions.Character
{
    [ChaosTimedEffect("swap_health_shield", 60f, AllowDuplicates = false)]
    public sealed class SwapHealthShield : MonoBehaviour
    {
        [InitEffectInfo]
        public static readonly TimedEffectInfo EffectInfo;

        public static void TryApplyStatChanges(CharacterBody body)
        {
            if (!ChaosEffectTracker.Instance || !ChaosEffectTracker.Instance.IsTimedEffectActive(EffectInfo))
                return;

            float prevMaxHealth = body.maxHealth;
            float maxShield = Mathf.Max(0f, prevMaxHealth);

            float prevMaxShield = body.maxShield;
            float maxHealth = Mathf.Max(0f, prevMaxShield);

            if (maxHealth <= 1f)
            {
                maxShield -= Mathf.Clamp01(1f - maxHealth);
                maxHealth = 1f;
            }

            if (maxShield <= 1f)
            {
                maxHealth += maxShield;
                maxShield = 0f;
            }

            body.maxHealth = Mathf.Max(1f, maxHealth);
            body.maxShield = Mathf.Max(0f, maxShield);
        }

        void Start()
        {
            foreach (CharacterBody body in CharacterBody.readOnlyInstancesList)
            {
                body.MarkAllStatsDirty();
            }
        }

        void OnDestroy()
        {
            foreach (CharacterBody body in CharacterBody.readOnlyInstancesList)
            {
                body.MarkAllStatsDirty();
            }
        }
    }
}
