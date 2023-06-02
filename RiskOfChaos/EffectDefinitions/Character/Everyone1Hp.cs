using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.Trackers;
using RoR2;
using System.Collections.Generic;
using UnityEngine;

namespace RiskOfChaos.EffectDefinitions.Character
{
    [ChaosEffect("everyone_1hp")]
    public sealed class Everyone1Hp : BaseEffect
    {
        public override void OnStart()
        {
            List<HealthComponentTracker> healthComponents = InstanceTracker.GetInstancesList<HealthComponentTracker>();
            for (int i = healthComponents.Count - 1; i >= 0; i--)
            {
                HealthComponentTracker healthComponentTracker = healthComponents[i];
                if (!healthComponentTracker)
                    continue;

                HealthComponent healthComponent = healthComponentTracker.HealthComponent;
                if (!healthComponent || !healthComponent.alive)
                    continue;

                float fakeDamageDealt = healthComponent.health - 1f;
                float combinedHealthBeforeDamage = healthComponent.combinedHealth;

                healthComponent.Networkhealth = 1f;
                healthComponent.Networkshield = 0f;
                healthComponent.Networkbarrier = 0f;

#pragma warning disable Publicizer001 // Accessing a member that was not originally public
                HealthComponent.SendDamageDealt(new DamageReport(new DamageInfo
#pragma warning restore Publicizer001 // Accessing a member that was not originally public
                {
                    attacker = null,
                    canRejectForce = false,
                    crit = false,
                    damage = fakeDamageDealt,
                    damageColorIndex = DamageColorIndex.Default,
                    damageType = DamageType.Generic,
                    dotIndex = DotController.DotIndex.None,
                    force = Vector3.zero,
                    inflictor = null,
                    position = healthComponent.transform.position,
                    procChainMask = default,
                    procCoefficient = 0f
                }, healthComponent, fakeDamageDealt, combinedHealthBeforeDamage));

                // If the effect deals less than this fraction of the player's max health, they will not be given invincibility
                const float MIN_DEALT_FRACTION_TO_GRANT_INVINCIBILITY = 0.2f;
                if (healthComponent.body &&
                    healthComponent.body.isPlayerControlled &&
                    fakeDamageDealt / healthComponent.fullHealth >= MIN_DEALT_FRACTION_TO_GRANT_INVINCIBILITY)
                {
#if DEBUG
                    Log.Debug("Giving temporary invincibility to " + Util.GetBestBodyName(healthComponent.body.gameObject));
#endif
                    healthComponent.body.AddTimedBuff(RoR2Content.Buffs.HiddenInvincibility, 0.75f);
                }
            }
        }
    }
}
