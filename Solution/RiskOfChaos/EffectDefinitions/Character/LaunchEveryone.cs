using HG;
using RiskOfChaos.ConfigHandling;
using RiskOfChaos.ConfigHandling.AcceptableValues;
using RiskOfChaos.Content;
using RiskOfChaos.EffectDefinitions.World.Knockback;
using RiskOfChaos.EffectHandling.Controllers;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Data;
using RiskOfChaos.EffectHandling.EffectComponents;
using RiskOfChaos.Patches;
using RiskOfChaos.Utilities;
using RiskOfChaos.Utilities.Extensions;
using RiskOfOptions.OptionConfigs;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectDefinitions.Character
{
    [ChaosEffect("launch_everyone")]
    [IncompatibleEffects(typeof(DisableKnockback))]
    public sealed class LaunchEveryone : NetworkBehaviour
    {
        [EffectConfig]
        static readonly ConfigHolder<float> _knockbackScale =
            ConfigFactory<float>.CreateConfig("Force Multiplier", 1f)
                                .Description("Scale of the force applied to all characters")
                                .AcceptableValues(new AcceptableValueMin<float>(0f))
                                .OptionConfig(new FloatFieldConfig { Min = 0f, FormatString = "{0}x" })
                                .Build();

        ChaosEffectComponent _effectComponent;

        [SyncVar]
        ulong _rngSeed;

        void Awake()
        {
            _effectComponent = GetComponent<ChaosEffectComponent>();
        }

        public override void OnStartServer()
        {
            base.OnStartServer();

            _rngSeed = _effectComponent.Rng.nextUlong;
        }

        void Start()
        {
            Xoroshiro128Plus rng = new Xoroshiro128Plus(_rngSeed);

            // Launch all the players first so the effect is as consistent as possible with the same seed
            PlayerUtils.GetAllPlayerBodies(true).TryDo(body =>
            {
                tryLaunchInRandomDirection(body, rng.Branch());

                if (NetworkServer.active)
                {
                    // Give players a chance to avoid fall damage
                    // Most relevant on characters without movement abilities (engi, captain)

                    giveAirborneTemporaryItem(body, RoR2Content.Items.Feather);
                }
            }, FormatUtils.GetBestBodyName);

            CharacterBody.readOnlyInstancesList.TryDo(body =>
            {
                if (body.isPlayerControlled)
                    return;

                tryLaunchInRandomDirection(body, rng);

                if (NetworkServer.active)
                {
                    if (body.inventory && body.inventory.GetItemCount(RoCContent.Items.InvincibleLemurianMarker) > 0)
                    {
                        giveAirborneTemporaryItem(body, RoR2Content.Items.TeleportWhenOob);
                    }
                }
            }, FormatUtils.GetBestBodyName);
        }

        static bool canLaunchDown(CharacterBody body)
        {
            if (body.teamComponent.teamIndex == TeamIndex.Player)
            {
                if ((Run.instance && Run.instance.selectedDifficulty >= DifficultyIndex.Eclipse3) ||
                    (RunArtifactManager.instance && RunArtifactManager.instance.IsArtifactEnabled(RoR2Content.Artifacts.WeakAssKnees)) ||
                    (ChaosEffectTracker.Instance && ChaosEffectTracker.Instance.IsTimedEffectActive(IncreaseFallDamage.EffectInfo)))
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

        static void tryLaunchInRandomDirection(CharacterBody body, Xoroshiro128Plus rng)
        {
            if (!body.hasEffectiveAuthority)
                return;

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

        static void giveAirborneTemporaryItem(CharacterBody body, ItemDef item)
        {
            Inventory inventory = body.inventory;
            if (!inventory)
                return;
            
            // Ensure item doesn't get turned into a void item if a mod adds that
            IgnoreItemTransformations.IgnoreTransformationsFor(inventory);

            inventory.GiveItem(item);

            void onHitGroundServer(CharacterBody characterBody, in CharacterMotor.HitGroundInfo hitGroundInfo)
            {
                if (characterBody != body)
                    return;

                if (inventory)
                {
                    inventory.RemoveItem(item);
                    IgnoreItemTransformations.ResumeTransformationsFor(inventory);
                }

                OnCharacterHitGroundServerHook.OnCharacterHitGround -= onHitGroundServer;
            }

            OnCharacterHitGroundServerHook.OnCharacterHitGround += onHitGroundServer;
        }
    }
}
