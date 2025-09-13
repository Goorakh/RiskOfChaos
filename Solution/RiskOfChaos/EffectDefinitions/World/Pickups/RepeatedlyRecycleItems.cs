using RiskOfChaos.Collections;
using RiskOfChaos.ConfigHandling;
using RiskOfChaos.ConfigHandling.AcceptableValues;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Data;
using RiskOfChaos.EffectHandling.EffectComponents;
using RiskOfChaos.Patches;
using RiskOfChaos.SaveHandling;
using RiskOfChaos.Utilities;
using RiskOfChaos.Utilities.Extensions;
using RiskOfOptions.OptionConfigs;
using RoR2;
using RoR2.ContentManagement;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace RiskOfChaos.EffectDefinitions.World.Pickups
{
    [ChaosTimedEffect("repeatedly_recycle_items", 90f, AllowDuplicates = false)]
    public sealed class RepeatedlyRecycleItems : NetworkBehaviour
    {
        static PickupIndex[] _allAvailablePickupIndices = [];

        static EffectIndex _recycleEffectIndex = EffectIndex.Invalid;

        [SystemInitializer(typeof(PickupCatalog), typeof(EffectCatalog))]
        static IEnumerator Init()
        {
            _allAvailablePickupIndices = PickupCatalog.allPickupIndices.Where(i =>
            {
                PickupDef pickupDef = PickupCatalog.GetPickupDef(i);
                if (pickupDef == null)
                    return false;

                if (!pickupDef.displayPrefab ||
                    !pickupDef.dropletDisplayPrefab ||
                    string.IsNullOrWhiteSpace(pickupDef.nameToken) ||
                    Language.IsTokenInvalid(pickupDef.nameToken))
                {
                    return false;
                }

                switch (pickupDef.internalName)
                {
                    case "EquipmentIndex.EliteSecretSpeedEquipment":
                    case "EquipmentIndex.EliteGoldEquipment":
                    case "EquipmentIndex.GhostGun":
                    case "EquipmentIndex.IrradiatingLaser":
                    case "EquipmentIndex.LunarPortalOnUse":
                    case "EquipmentIndex.SoulJar":
                    case "MiscPickupIndex.VoidCoin":
                        return false;
                }

                return true;
            }).ToArray();

            AsyncOperationHandle<GameObject> recycleEffectLoad = AddressableUtil.LoadAssetAsync<GameObject>(AddressableGuids.RoR2_Base_Recycle_OmniRecycleEffect_prefab, AsyncReferenceHandleUnloadType.Preload);
            recycleEffectLoad.OnSuccess(recycleEffectPrefab =>
            {
                _recycleEffectIndex = EffectCatalog.FindEffectIndexFromPrefab(recycleEffectPrefab);
                if (_recycleEffectIndex == EffectIndex.Invalid)
                {
                    Log.Error($"Failed to find recycle effect index from {recycleEffectPrefab}");
                }
            });

            return recycleEffectLoad;
        }

        [EffectConfig]
        static readonly ConfigHolder<float> _recycleTimerScale =
            ConfigFactory<float>.CreateConfig("Recycle Timer Scale", 1f)
                                .Description("The multiplier to apply to the recycle timer (duration between items)")
                                .AcceptableValues(new AcceptableValueMin<float>(0f))
                                .OptionConfig(new FloatFieldConfig { Min = 0f })
                                .Build();

        static bool isPickupAvailable(PickupIndex pickup)
        {
            if (Run.instance.IsPickupEnabled(pickup))
                return true;

            PickupDef pickupDef = PickupCatalog.GetPickupDef(pickup);
            if (pickupDef.itemIndex != ItemIndex.None)
            {
                ItemDef item = ItemCatalog.GetItemDef(pickupDef.itemIndex);
                if (item.ContainsTag(ItemTag.WorldUnique))
                {
                    Log.Debug($"Including worldunique pickup: {pickup}");
                    return true;
                }
            }

            return false;
        }

        const float RECYCLE_IGNORE_GROUP_CHANCE = 0.05f;

        ChaosEffectComponent _effectComponent;

        readonly ClearingObjectList<RecycleOnTimer> _recyclerComponents = [];

        PickupIndex[] _recycleSteps = [];

        [SerializedMember("s")]
        PickupIndex[] serializedRecycleSteps
        {
            get
            {
                return _recycleSteps;
            }
            set
            {
                List<PickupIndex> recycleSteps = new List<PickupIndex>(value ?? []);

                for (int i = recycleSteps.Count - 1; i >= 0; i--)
                {
                    if (!isPickupAvailable(recycleSteps[i]))
                    {
                        recycleSteps.RemoveAt(i);
                    }
                }

                _recycleSteps = recycleSteps.ToArray();
            }
        }

        void Awake()
        {
            _effectComponent = GetComponent<ChaosEffectComponent>();
        }

        public override void OnStartServer()
        {
            base.OnStartServer();

            Xoroshiro128Plus rng = new Xoroshiro128Plus(_effectComponent.Rng.nextUlong);

            List<PickupIndex> remainingPickups = new List<PickupIndex>(_allAvailablePickupIndices.Length);
            foreach (PickupIndex pickupIndex in _allAvailablePickupIndices)
            {
                if (isPickupAvailable(pickupIndex))
                {
                    remainingPickups.Add(pickupIndex);
                }
            }

            Log.Debug($"Non-included pickups: [{string.Join(", ", _allAvailablePickupIndices.Except(remainingPickups))}]");

            int cycleLength = Mathf.CeilToInt(remainingPickups.Count * rng.RangeFloat(1.25f, 2f));

            remainingPickups.EnsureCapacity(cycleLength);
            while (remainingPickups.Count < cycleLength)
            {
                remainingPickups.Add(rng.NextElementUniform(remainingPickups));
            }

            _recycleSteps = new PickupIndex[cycleLength];

            IList<PickupIndex> availableNextStepOptions = Array.Empty<PickupIndex>();

            for (int i = 0; i < cycleLength; i++)
            {
                bool ignoreGroup = rng.nextNormalizedFloat <= RECYCLE_IGNORE_GROUP_CHANCE;

                PickupIndex stepPickup;
                if (availableNextStepOptions.Count > 0 && !ignoreGroup)
                {
                    stepPickup = rng.NextElementUniform(availableNextStepOptions);
                    remainingPickups.Remove(stepPickup);
                }
                else
                {
                    stepPickup = remainingPickups.GetAndRemoveRandom(rng);
                }

                _recycleSteps[i] = stepPickup;

                IList<PickupIndex> nextStepOptions = PickupTransmutationManager.GetGroupFromPickupIndex(stepPickup) ?? [];
                if (nextStepOptions.Count > 0)
                {
                    List<PickupIndex> availableDestinationPickups = new List<PickupIndex>(nextStepOptions.Count);

                    foreach (PickupIndex pickup in nextStepOptions)
                    {
                        if (pickup != stepPickup && isPickupAvailable(pickup) && remainingPickups.Contains(pickup))
                        {
                            availableDestinationPickups.Add(pickup);
                        }
                    }

                    nextStepOptions = availableDestinationPickups;
                }

                availableNextStepOptions = nextStepOptions;
            }
        }

        void Start()
        {
            if (NetworkServer.active)
            {
                List<GenericPickupController> genericPickupControllers = InstanceTracker.GetInstancesList<GenericPickupController>();

                _recyclerComponents.EnsureCapacity(genericPickupControllers.Count);

                genericPickupControllers.TryDo(handlePickupController);
                GenericPickupControllerHooks.OnGenericPickupControllerStartGlobal += handlePickupController;
            }
        }

        void OnDestroy()
        {
            GenericPickupControllerHooks.OnGenericPickupControllerStartGlobal -= handlePickupController;

            _recyclerComponents.ClearAndDispose(true);
        }

        [Server]
        void handlePickupController(GenericPickupController pickupController)
        {
            RecycleOnTimer recycleOnTimer = pickupController.gameObject.AddComponent<RecycleOnTimer>();
            recycleOnTimer.EffectInstance = this;

            _recyclerComponents.Add(recycleOnTimer);
        }

        int findRecycleStepIndex(PickupIndex pickupIndex)
        {
            for (int i = 0; i < _recycleSteps.Length; i++)
            {
                if (_recycleSteps[i] == pickupIndex)
                {
                    return i;
                }
            }

            return 0;
        }

        PickupIndex getRecycleStep(int index)
        {
            return _recycleSteps[index % _recycleSteps.Length];
        }

        sealed class RecycleOnTimer : MonoBehaviour
        {
            static readonly AnimationCurve _recycleTimeCurve = new AnimationCurve([
                new Keyframe(0f, 1f, -1f, -1f),
                new Keyframe(0.5f, 0.3f, -0.5f, -0.5f),
                new Keyframe(0.8f, 0.15f, -1f, -1f),
                new Keyframe(1f, 0f, 0f, 0f),
            ]);

            public RepeatedlyRecycleItems EffectInstance;

            public GenericPickupController PickupController { get; private set; }

            const int MAX_RECYCLES = 30;

            int _numTimesRecycled;
            bool _isDoneRecycling;

            float _recycleTimer;

            int _currentRecycleStepIndex;

            PickupIndex currentRecycleStep => EffectInstance.getRecycleStep(_currentRecycleStepIndex);

            void Awake()
            {
                PickupController = GetComponent<GenericPickupController>();
            }

            void Start()
            {
                PickupController.NetworkRecycled = true;

                _currentRecycleStepIndex = EffectInstance.findRecycleStepIndex(PickupController.pickupIndex);

                startRecycleTimer();
            }

            void startRecycleTimer()
            {
                _recycleTimer = 0.2f + (1.3f * _recycleTimeCurve.Evaluate(_numTimesRecycled / (float)MAX_RECYCLES));
                _recycleTimer *= _recycleTimerScale.Value;
            }

            void stepRecycle()
            {
                PickupIndex nextPickup = currentRecycleStep;
                if (!nextPickup.isValid || nextPickup == PickupController.pickupIndex)
                    return;

                PickupController.NetworkpickupIndex = nextPickup;

                if (_recycleEffectIndex != EffectIndex.Invalid)
                {
                    EffectManager.SpawnEffect(_recycleEffectIndex, new EffectData
                    {
                        origin = PickupController.pickupDisplay.transform.position
                    }, true);
                }

                _numTimesRecycled++;
                
                if (_numTimesRecycled >= MAX_RECYCLES)
                {
                    PickupController.NetworkRecycled = true;
                    _isDoneRecycling = true;
                }
            }

            void FixedUpdate()
            {
                if (_isDoneRecycling)
                    return;

                _recycleTimer -= Time.fixedDeltaTime;
                if (_recycleTimer <= 0f)
                {
                    if (PickupController.pickupIndex != currentRecycleStep)
                    {
                        _currentRecycleStepIndex = EffectInstance.findRecycleStepIndex(PickupController.pickupIndex);
                    }

                    _currentRecycleStepIndex++;

                    stepRecycle();
                    startRecycleTimer();
                }
            }
        }
    }
}
