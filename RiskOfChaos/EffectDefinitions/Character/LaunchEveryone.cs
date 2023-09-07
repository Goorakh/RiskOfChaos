using HG;
using RiskOfChaos.ConfigHandling;
using RiskOfChaos.EffectDefinitions.World;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Data;
using RiskOfChaos.Utilities;
using RiskOfChaos.Utilities.Extensions;
using RiskOfOptions.OptionConfigs;
using RoR2;
using UnityEngine;

namespace RiskOfChaos.EffectDefinitions.Character
{
    [ChaosEffect("launch_everyone")]
    [IncompatibleEffects(typeof(DisableKnockback))]
    public sealed class LaunchEveryone : BaseEffect
    {
        [EffectConfig]
        static readonly ConfigHolder<float> _knockbackScale =
            ConfigFactory<float>.CreateConfig("Force Multiplier", 1f)
                                .Description("Scale of the force applied to all characters")
                                .OptionConfig(new StepSliderConfig
                                {
                                    formatString = "{0:F2}x",
                                    min = 0f,
                                    max = 5f,
                                    increment = 0.05f
                                })
                                .ValueConstrictor(CommonValueConstrictors.GreaterThanOrEqualTo(0f))
                                .Build();

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

            applyForceToBody(body, direction * (RNG.RangeFloat(50f, 150f) * _knockbackScale.Value));
        }

        static void applyForceToBody(CharacterBody body, Vector3 force)
        {
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
