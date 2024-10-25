using RiskOfChaos.EffectDefinitions.World;
using RiskOfChaos.EffectHandling;
using RiskOfChaos.EffectHandling.Controllers;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Data;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectDefinitions.Character
{
    [ChaosTimedEffect("attack_knockback", 90f)]
    [IncompatibleEffects(typeof(DisableKnockback))]
    [EffectConfigBackwardsCompatibility("Effect: Extreme Recoil")]
    public sealed class AttackKnockback : MonoBehaviour
    {
        [InitEffectInfo]
        public static readonly TimedEffectInfo EffectInfo;

        public static void TryKnockbackBody(CharacterBody body, Vector3 knockbackDirection, float damage)
        {
            if (NetworkServer.active || body.hasEffectiveAuthority)
            {
                if (!body || knockbackDirection.sqrMagnitude == 0f || damage <= 0f)
                    return;

                int effectStackCount = ChaosEffectTracker.Instance.GetTimedEffectStackCount(EffectInfo);
                if (effectStackCount <= 0)
                    return;

                float baseForce = body.isPlayerControlled ? 8.5f : 30f;

                float damageCoefficient = damage / body.damage;
                float damageForceMultiplier = Mathf.Pow(damageCoefficient, damageCoefficient > 1f ? 1f / 1.5f : 2f);

                Vector3 force = knockbackDirection.normalized * (baseForce * damageForceMultiplier * effectStackCount);

                if (body.TryGetComponent(out IPhysMotor motor))
                {
                    PhysForceInfo physForceInfo = PhysForceInfo.Create();
                    physForceInfo.force = force;
                    physForceInfo.disableAirControlUntilCollision = false;
                    physForceInfo.ignoreGroundStick = true;
                    physForceInfo.massIsOne = true;

                    motor.ApplyForceImpulse(physForceInfo);
                }
                else if (body.TryGetComponent(out Rigidbody rigidbody))
                {
                    rigidbody.AddForce(force, ForceMode.VelocityChange);
                }
            }
        }
    }
}
