using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.Utilities;
using RiskOfChaos.Utilities.Extensions;
using RoR2;
using System;
using System.Linq;
using UnityEngine;

namespace RiskOfChaos.EffectDefinitions.World.Pickups
{
    [ChaosEffect("repeatedly_recycle_items")]
    [ChaosTimedEffect(90f, AllowDuplicates = false)]
    public sealed class RepeatedlyRecycleItems : TimedEffect
    {
        static PickupIndex[] _allAvailablePickupIndices = Array.Empty<PickupIndex>();

        [SystemInitializer(typeof(PickupCatalog))]
        static void Init()
        {
            _allAvailablePickupIndices = PickupCatalog.allPickupIndices.Where(i =>
            {
                PickupDef pickupDef = PickupCatalog.GetPickupDef(i);
                return pickupDef != null &&
                       pickupDef.displayPrefab &&
                       pickupDef.dropletDisplayPrefab &&
                       !string.IsNullOrWhiteSpace(pickupDef.nameToken) &&
                       Language.GetString(pickupDef.nameToken) != pickupDef.nameToken;
            }).ToArray();
        }

        [RequireComponent(typeof(GenericPickupController))]
        sealed class RecycleOnTimer : MonoBehaviour
        {
            GenericPickupController _pickupController;

            const float INITIAL_RECYCLE_TIME_MULTIPLIER = 0.75f;
            float _recycleTimer;

            void Awake()
            {
                InstanceTracker.Add(this);
                _pickupController = GetComponent<GenericPickupController>();
            }

            void OnEnable()
            {
                startRecycleTimer();
                _recycleTimer *= INITIAL_RECYCLE_TIME_MULTIPLIER;
            }

            void startRecycleTimer()
            {
                _recycleTimer = RoR2Application.rng.RangeFloat(0.75f, 3f);
            }

            void setToRandomItem()
            {
                PickupIndex currentPickup = _pickupController.pickupIndex;

                PickupIndex[] availablePickups = PickupTransmutationManager.GetAvailableGroupFromPickupIndex(currentPickup);
                if (availablePickups == null || availablePickups.Length == 0 || RoR2Application.rng.nextNormalizedFloat <= 0.05f)
                    availablePickups = _allAvailablePickupIndices;

                availablePickups = availablePickups.Where(p => p != currentPickup).ToArray();
                if (availablePickups.Length == 0)
                    return;

                _pickupController.NetworkpickupIndex = RoR2Application.rng.NextElementUniform(availablePickups);
                EffectManager.SimpleEffect(LegacyResourcesAPI.Load<GameObject>("Prefabs/Effects/OmniEffect/OmniRecycleEffect"), _pickupController.pickupDisplay.transform.position, Quaternion.identity, true);
            }

            void FixedUpdate()
            {
                _recycleTimer -= Time.fixedDeltaTime;
                if (_recycleTimer <= 0f)
                {
                    setToRandomItem();
                    startRecycleTimer();
                }
            }

            void OnDestroy()
            {
                InstanceTracker.Remove(this);
            }
        }

        public override void OnStart()
        {
            On.RoR2.GenericPickupController.Start += GenericPickupController_Start;
            GameObject.FindObjectsOfType<GenericPickupController>().TryDo(tryAddComponent);
        }

        static void GenericPickupController_Start(On.RoR2.GenericPickupController.orig_Start orig, GenericPickupController self)
        {
            orig(self);
            tryAddComponent(self);
        }

        static void tryAddComponent(GenericPickupController pickupController)
        {
            if (!pickupController.GetComponent<RecycleOnTimer>())
            {
                pickupController.gameObject.AddComponent<RecycleOnTimer>();
            }
        }

        public override void OnEnd()
        {
            On.RoR2.GenericPickupController.Start -= GenericPickupController_Start;

            InstanceUtils.DestroyAllTrackedInstances<RecycleOnTimer>();
        }
    }
}
