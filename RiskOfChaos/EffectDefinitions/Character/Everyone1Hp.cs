using RiskOfChaos.EffectHandling;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace RiskOfChaos.EffectDefinitions.Character
{
    [ChaosEffect("Everyone1Hp")]
    public class Everyone1Hp : BaseEffect
    {
        public override void OnStart()
        {
            List<HealthComponent> healthComponents = InstanceTracker.GetInstancesList<HealthComponent>();
            for (int i = healthComponents.Count - 1; i >= 0; i--)
            {
                HealthComponent healthComponent = healthComponents[i];
                if (healthComponent && healthComponent.alive)
                {
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
                }
            }
        }
    }
}
