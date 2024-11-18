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
                tryLaunchInRandomDirection(body, body.isPlayerControlled ? playerRng : nonPlayerRng);
            }, FormatUtils.GetBestBodyName);
        }

        static Vector3 getLaunchDirection(Xoroshiro128Plus rng)
        {
            return QuaternionUtils.RandomDeviation(70f, rng) * Vector3.up;
        }

        static void tryLaunchInRandomDirection(CharacterBody body, Xoroshiro128Plus rng)
        {
            if (!body.hasEffectiveAuthority)
                return;

            Vector3 direction = getLaunchDirection(rng.Branch());
            applyForceToBody(body, direction * (rng.RangeFloat(50f, 150f) * _knockbackScale.Value));

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
