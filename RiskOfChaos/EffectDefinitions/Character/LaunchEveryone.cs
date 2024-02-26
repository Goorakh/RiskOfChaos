using HG;
using RiskOfChaos.ConfigHandling;
using RiskOfChaos.ConfigHandling.AcceptableValues;
using RiskOfChaos.Content;
using RiskOfChaos.EffectDefinitions.World;
using RiskOfChaos.EffectHandling.Controllers;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Data;
using RiskOfChaos.Patches;
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
                                .AcceptableValues(new AcceptableValueMin<float>(0f))
                                .OptionConfig(new StepSliderConfig
                                {
                                    formatString = "{0:F2}x",
                                    min = 0f,
                                    max = 5f,
                                    increment = 0.05f
                                })
                                .Build();

        public override void OnStart()
        {
            // Launch all the players first so the effect is as consistent as possible with the same seed
            PlayerUtils.GetAllPlayerBodies(true).TryDo(b =>
            {
                launchInRandomDirection(b, RNG.Branch());

                // Give players a chance to avoid fall damage
                // Most relevant on characters without movement abilities (engi, captain)

                CharacterMotor characterMotor = b.characterMotor;
                Inventory inventory = b.inventory;
                if (characterMotor && inventory)
                {
                    // Ensure feather doesn't get turned into a void item if a mod adds that
                    IgnoreItemTransformations.IgnoreTransformationsFor(inventory);

                    inventory.GiveItem(RoR2Content.Items.Feather);

                    void onHitGroundServer(ref CharacterMotor.HitGroundInfo hitGroundInfo)
                    {
                        if (inventory)
                        {
                            inventory.RemoveItem(RoR2Content.Items.Feather);
                            IgnoreItemTransformations.ResumeTransformationsFor(inventory);
                        }

                        if (characterMotor)
                        {
                            characterMotor.onHitGroundServer -= onHitGroundServer;
                        }
                    }

                    characterMotor.onHitGroundServer += onHitGroundServer;
                }
            }, FormatUtils.GetBestBodyName);

            CharacterBody.readOnlyInstancesList.Where(b => !b.isPlayerControlled).TryDo(b =>
            {
                launchInRandomDirection(b, RNG);

                CharacterMotor characterMotor = b.characterMotor;
                Inventory inventory = b.inventory;
                if (characterMotor && inventory)
                {
                    if (inventory.GetItemCount(Items.InvincibleLemurianMarker) > 0)
                    {
                        inventory.GiveItem(RoR2Content.Items.TeleportWhenOob);

                        void onHitGroundServer(ref CharacterMotor.HitGroundInfo hitGroundInfo)
                        {
                            if (inventory)
                            {
                                inventory.RemoveItem(RoR2Content.Items.TeleportWhenOob);
                            }

                            if (characterMotor)
                            {
                                characterMotor.onHitGroundServer -= onHitGroundServer;
                            }
                        }

                        characterMotor.onHitGroundServer += onHitGroundServer;
                    }
                }
            }, FormatUtils.GetBestBodyName);
        }

        static bool canLaunchDown(CharacterBody body)
        {
            if (body.teamComponent.teamIndex == TeamIndex.Player)
            {
                if ((Run.instance && Run.instance.selectedDifficulty >= DifficultyIndex.Eclipse3) ||
                    (RunArtifactManager.instance && RunArtifactManager.instance.IsArtifactEnabled(RoR2Content.Artifacts.weakAssKneesArtifactDef)) ||
                    (TimedChaosEffectHandler.Instance && TimedChaosEffectHandler.Instance.GetActiveEffectInstancesOfType<IncreaseFallDamage>().Any()))
                {
                    return false;
                }
            }

            if (body.characterMotor && body.characterMotor.isGrounded)
                return false;

            return true;
        }

        static Vector3 getLaunchDirection(CharacterBody body, Xoroshiro128Plus rng)
        {
            if (canLaunchDown(body))
            {
                return rng.PointOnUnitSphere();
            }
            else
            {
                return QuaternionUtils.RandomDeviation(70f, rng) * Vector3.up;
            }
        }

        static void launchInRandomDirection(CharacterBody body, Xoroshiro128Plus rng)
        {
            Vector3 direction = getLaunchDirection(body, rng.Branch());
            applyForceToBody(body, direction * (rng.RangeFloat(50f, 150f) * _knockbackScale.Value));
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
