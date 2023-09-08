using HG;
using RiskOfChaos.ConfigHandling;
using RiskOfChaos.EffectDefinitions.World;
using RiskOfChaos.EffectHandling.Controllers;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Data;
using RiskOfChaos.Utilities;
using RiskOfChaos.Utilities.Extensions;
using RiskOfOptions.OptionConfigs;
using RoR2;
using System.Linq;
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

        static bool canLaunchDown(CharacterBody body)
        {
            if (body.teamComponent && body.teamComponent.teamIndex == TeamIndex.Player)
            {
                if ((Run.instance && Run.instance.selectedDifficulty >= DifficultyIndex.Eclipse3) ||
                    (RunArtifactManager.instance && RunArtifactManager.instance.IsArtifactEnabled(RoR2Content.Artifacts.weakAssKneesArtifactDef)) ||
                    (TimedChaosEffectHandler.Instance && TimedChaosEffectHandler.Instance.GetActiveEffectInstancesOfType<IncreaseFallDamage>().Any()))
                {
                    return false;
                }
            }

            if (body.characterMotor && body.characterMotor.isGrounded && !body.characterMotor.isFlying)
                return false;

            return true;
        }

        void launchInRandomDirection(CharacterBody body)
        {
            if (!body)
                return;

            Vector3 direction;
            if (canLaunchDown(body))
            {
                direction = RNG.PointOnUnitSphere();
            }
            else
            {
                direction = QuaternionUtils.RandomDeviation(70f, RNG) * Vector3.up;
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
