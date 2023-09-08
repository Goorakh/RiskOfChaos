using RiskOfChaos.ConfigHandling;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Data;
using RiskOfChaos.Utilities;
using RiskOfChaos.Utilities.Extensions;
using RiskOfOptions.OptionConfigs;
using RoR2;
using System;
using System.Linq;
using UnityEngine;

namespace RiskOfChaos.EffectDefinitions.World.Pickups
{
    [ChaosTimedEffect("repeatedly_recycle_items", 90f, AllowDuplicates = false)]
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

        [EffectConfig]
        static readonly ConfigHolder<float> _recycleTimerScale =
            ConfigFactory<float>.CreateConfig("Recycle Timer Scale", 1f)
                                .Description("The multiplier to apply to the recycle timer (duration between items)")
                                .OptionConfig(new StepSliderConfig
                                {
                                    formatString = "{0:F1}",
                                    min = 0.1f,
                                    max = 5f,
                                    increment = 0.1f
                                })
                                .ValueConstrictor(CommonValueConstrictors.GreaterThanOrEqualTo(0.1f))
                                .Build();

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
                _recycleTimer = RoR2Application.rng.RangeFloat(0.75f, 3f) * _recycleTimerScale.Value;
            }

            void setToRandomItem()
            {
                PickupIndex currentPickup = _pickupController.pickupIndex;

                PickupIndex[] availablePickups = PickupTransmutationManager.GetAvailableGroupFromPickupIndex(currentPickup);
                availablePickups = availablePickups?.Where(p => p != currentPickup).ToArray();
                if (availablePickups == null || availablePickups.Length == 0 || RoR2Application.rng.nextNormalizedFloat <= 0.05f)
                    availablePickups = _allAvailablePickupIndices;

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
