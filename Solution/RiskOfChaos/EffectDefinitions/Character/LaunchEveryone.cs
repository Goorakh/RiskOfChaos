using RiskOfChaos.ConfigHandling;
using RiskOfChaos.ConfigHandling.AcceptableValues;
using RiskOfChaos.Content;
using RiskOfChaos.EffectDefinitions.World.Knockback;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Data;
using RiskOfChaos.EffectHandling.EffectComponents;
using RiskOfChaos.Patches;
using RiskOfChaos.Utilities;
using RiskOfChaos.Utilities.Extensions;
using RiskOfChaos.Utilities.Pickup;
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

            Xoroshiro128Plus playerRng = new Xoroshiro128Plus(rng.nextUlong);
            Xoroshiro128Plus nonPlayerRng = new Xoroshiro128Plus(rng.nextUlong);

            CharacterBody.readOnlyInstancesList.TryDo(body =>
            {
                Xoroshiro128Plus rng = body.isPlayerControlled ? playerRng : nonPlayerRng;

                tryLaunchInRandomDirection(body, new Xoroshiro128Plus(rng.nextUlong));
            }, FormatUtils.GetBestBodyName);
        }

        static Vector3 getLaunchDirection(Xoroshiro128Plus rng)
        {
            return VectorUtils.Spread(Vector3.up, 70f, rng);
        }

        static void tryLaunchInRandomDirection(CharacterBody body, Xoroshiro128Plus rng)
        {
            if (body.currentVehicle != null)
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

            if (NetworkServer.active && body.inventory)
            {
                if (body.isPlayerControlled)
                {
                    // Give players a chance to avoid fall damage
                    // Most relevant on characters without movement abilities (engi, captain)

                    giveAirborneTemporaryItem(body, RoR2Content.Items.Feather, true, true);
                }

                if (body.inventory.GetItemCount(RoCContent.Items.InvincibleLemurianMarker) > 0)
                {
                    giveAirborneTemporaryItem(body, RoR2Content.Items.TeleportWhenOob, true, false);
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

        static void giveAirborneTemporaryItem(CharacterBody body, ItemDef item, bool skipIfAlreadyPresent, bool notify)
        {
            Inventory inventory = body.inventory;
            if (!inventory)
                return;

            if (skipIfAlreadyPresent && inventory.GetItemCount(item) > 0)
                return;

            CharacterMaster master = body.master;

            if (notify && !item.hidden && master.playerCharacterMasterController)
            {
                PickupUtils.QueuePickupMessage(master, PickupCatalog.FindPickupIndex(item.itemIndex), PickupNotificationFlags.DisplayPushNotificationIfNoneQueued | PickupNotificationFlags.PlaySound);
            }

            // Ensure item doesn't get turned into a void item if a mod adds that
            IgnoreItemTransformations.IgnoreTransformationsFor(inventory);
            
            inventory.GiveItem(item);

            void onHitGroundServer(CharacterBody characterBody, in CharacterMotor.HitGroundInfo hitGroundInfo)
            {
                if (!body || characterBody == body)
                {
                    if (characterBody)
                    {
                        Inventory inventory = characterBody.inventory;
                        if (inventory)
                        {
                            inventory.RemoveItem(item);
                            IgnoreItemTransformations.ResumeTransformationsFor(inventory);
                        }
                    }

                    OnCharacterHitGroundServerHook.OnCharacterHitGround -= onHitGroundServer;
                }
            }

            OnCharacterHitGroundServerHook.OnCharacterHitGround += onHitGroundServer;
        }
    }
}
