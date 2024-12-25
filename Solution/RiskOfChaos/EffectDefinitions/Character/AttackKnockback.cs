using RiskOfChaos.EffectDefinitions.World.Knockback;
using RiskOfChaos.EffectHandling;
using RiskOfChaos.EffectHandling.Controllers;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Data;
using RiskOfChaos.Patches.AttackHooks;
using RiskOfChaos.Utilities.Extensions;
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

        public static bool TryKnockbackBody(in AttackInfo attackInfo)
        {
            if (attackInfo.ProcChainMask.HasAnyProc())
                return false;

            CharacterBody attackerBody = attackInfo.Attacker ? attackInfo.Attacker.GetComponent<CharacterBody>() : null;
            if (!attackerBody)
                return false;

            if (!NetworkServer.active && !attackerBody.hasEffectiveAuthority)
                return false;
            
            if (attackInfo.AttackDirection.sqrMagnitude == 0f || attackInfo.Damage <= 0f)
                return false;

            int effectStackCount = ChaosEffectTracker.Instance.GetTimedEffectStackCount(EffectInfo);
            if (effectStackCount <= 0)
                return false;

            float baseForce = attackerBody.isPlayerControlled ? 8.5f : 30f;

            float damageCoefficient = attackInfo.Damage / attackerBody.damage;
            float damageForceMultiplier = Mathf.Pow(damageCoefficient, damageCoefficient > 1f ? 1f / 1.5f : 2f);

            Vector3 knockbackDirection = -attackInfo.AttackDirection;
            Vector3 force = knockbackDirection.normalized * (baseForce * damageForceMultiplier * effectStackCount);

            if (attackerBody.TryGetComponent(out IPhysMotor motor))
            {
                PhysForceInfo physForceInfo = PhysForceInfo.Create();
                physForceInfo.force = force;
                physForceInfo.disableAirControlUntilCollision = false;
                physForceInfo.ignoreGroundStick = true;
                physForceInfo.massIsOne = true;

                motor.ApplyForceImpulse(physForceInfo);
            }
            else if (attackerBody.TryGetComponent(out Rigidbody rigidbody))
            {
                rigidbody.AddForce(force, ForceMode.VelocityChange);
            }

            return true;
        }
    }
}
