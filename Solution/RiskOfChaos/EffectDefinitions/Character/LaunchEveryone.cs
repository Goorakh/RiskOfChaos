using RiskOfChaos.Components;
using RiskOfChaos.ConfigHandling;
using RiskOfChaos.ConfigHandling.AcceptableValues;
using RiskOfChaos.Content;
using RiskOfChaos.EffectDefinitions.World.Knockback;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Data;
using RiskOfChaos.EffectHandling.EffectComponents;
using RiskOfChaos.Utilities;
using RiskOfOptions.OptionConfigs;
using RoR2;
using System;
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

            Xoroshiro128Plus playerRng = new Xoroshiro128Plus(rng.nextUlong);
            Xoroshiro128Plus nonPlayerRng = new Xoroshiro128Plus(rng.nextUlong);

            foreach (CharacterBody body in CharacterBody.readOnlyInstancesList)
            {
                if (!body)
                    continue;

                Xoroshiro128Plus launchRng = body.isPlayerControlled ? playerRng : nonPlayerRng;

                try
                {
                    tryLaunchInRandomDirection(body, new Xoroshiro128Plus(launchRng.nextUlong));
                }
                catch (Exception e)
                {
                    Log.Error_NoCallerPrefix($"Failed to launch {FormatUtils.GetBestBodyName(body)}: {e}");
                }
            }
        }

        static Vector3 getLaunchDirection(Xoroshiro128Plus rng)
        {
            return VectorUtils.Spread(Vector3.up, 70f, rng);
        }

        static void tryLaunchInRandomDirection(CharacterBody body, Xoroshiro128Plus rng)
        {
            if (body.currentVehicle)
                return;

            if (body.hasEffectiveAuthority)
            {
                Vector3 direction = getLaunchDirection(rng);

                float knockbackScale = _knockbackScale.Value;
                if (!body.isPlayerControlled)
                {
                    knockbackScale *= 2f;
                }

                applyForceToBody(body, direction * (rng.RangeFloat(30f, 70f) * knockbackScale));
            }

            if (NetworkServer.active)
            {
                Inventory inventory = body.inventory;
                if (inventory)
                {
                    if (body.isPlayerControlled)
                    {
                        // Give players a chance to avoid fall damage
                        // Most relevant on characters without movement abilities (engi, captain)

                        if (inventory.GetItemCountEffective(RoR2Content.Items.Feather) == 0)
                        {
                            TemporaryItemController.AddTemporaryItem(inventory, RoR2Content.Items.Feather, TemporaryItemController.TemporaryItemCondition.Airborne, TemporaryItemController.TemporaryItemFlags.SuppressItemTransformation);
                        }
                    }

                    if (inventory.GetItemCountPermanent(RoCContent.Items.InvincibleLemurianMarker) > 0)
                    {
                        if (inventory.GetItemCountPermanent(RoR2Content.Items.TeleportWhenOob) == 0)
                        {
                            TemporaryItemController.AddTemporaryItem(inventory, RoR2Content.Items.TeleportWhenOob, TemporaryItemController.TemporaryItemCondition.Airborne);
                        }
                    }
                }
            }
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
