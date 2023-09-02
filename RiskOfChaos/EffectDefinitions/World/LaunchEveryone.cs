using HG;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.Utilities;
using RiskOfChaos.Utilities.Extensions;
using RoR2;
using UnityEngine;

namespace RiskOfChaos.EffectDefinitions.World
{
    [ChaosEffect("launch_everyone")]
    [IncompatibleEffects(typeof(DisableKnockback))]
    public sealed class LaunchEveryone : BaseEffect
    {
        public override void OnStart()
        {
            CharacterBody.readOnlyInstancesList.TryDo(launchInRandomDirection, FormatUtils.GetBestBodyName);
        }

        void launchInRandomDirection(CharacterBody body)
        {
            if (!body)
                return;

            CharacterMotor characterMotor = body.characterMotor;

            Vector3 direction;
            if (!characterMotor || characterMotor.isFlying || !characterMotor.isGrounded)
            {
                direction = RNG.PointOnUnitSphere();
            }
            else
            {
                const float DEVIATION = 70f;

                direction = Quaternion.Euler(RNG.RangeFloat(-DEVIATION, DEVIATION),
                                             RNG.RangeFloat(-DEVIATION, DEVIATION),
                                             RNG.RangeFloat(-DEVIATION, DEVIATION)) * Vector3.up;
            }

            applyForceToBody(body, direction * RNG.RangeFloat(50f, 150f));
        }

        static void applyForceToBody(CharacterBody body, Vector3 force)
        {
            if (body.TryGetComponent<IPhysMotor>(out IPhysMotor motor))
            {
                PhysForceInfo physForceInfo = PhysForceInfo.Create();
                physForceInfo.force = force;
                physForceInfo.disableAirControlUntilCollision = false;
                physForceInfo.ignoreGroundStick = true;
                physForceInfo.massIsOne = true;

                motor.ApplyForceImpulse(physForceInfo);
            }
            else if (body.TryGetComponent<Rigidbody>(out Rigidbody rigidbody))
            {
                rigidbody.AddForce(force, ForceMode.VelocityChange);
            }
        }
    }
}
