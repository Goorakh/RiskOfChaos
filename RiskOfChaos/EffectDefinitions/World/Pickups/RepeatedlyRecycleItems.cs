using RiskOfChaos.ConfigHandling;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Data;
using RiskOfChaos.Utilities;
using RiskOfChaos.Utilities.Extensions;
using RiskOfOptions.OptionConfigs;
using RoR2;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;

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
                if (pickupDef == null)
                    return false;

                if (!pickupDef.displayPrefab ||
                    !pickupDef.dropletDisplayPrefab ||
                    string.IsNullOrWhiteSpace(pickupDef.nameToken) ||
                    Language.GetString(pickupDef.nameToken) == pickupDef.nameToken)
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

        const float RECYCLE_IGNORE_GROUP_CHANCE = 0.05f;

        const float MIN_RECYCLE_DURATION = 1f;

        static float generateBaseRecycleDuration(Xoroshiro128Plus rng)
        {
            return rng.RangeFloat(MIN_RECYCLE_DURATION, 3f);
        }

        readonly record struct RecycleStep(PickupIndex PickupIndex, float Duration);

        bool _isSeeded;
        Dictionary<PickupIndex, int> _recycleStepIndexLookup;
        RecycleStep[] _recycleSteps;

        public override void OnPreStartServer()
        {
            base.OnPreStartServer();

            _isSeeded = Configs.General.SeededEffectSelection.Value;
            if (_isSeeded)
            {
                List<PickupIndex> remainingPickups = _allAvailablePickupIndices.ToList();
                int cycleLength = remainingPickups.Count;

                _recycleSteps = new RecycleStep[cycleLength];
                _recycleStepIndexLookup = new Dictionary<PickupIndex, int>(cycleLength);

                void registerPickupForCycle(PickupIndex pickup, int cycleIndex)
                {
                    _recycleSteps[cycleIndex] = new RecycleStep(pickup, generateBaseRecycleDuration(RNG));
                    _recycleStepIndexLookup.Add(pickup, cycleIndex);
                }

                registerPickupForCycle(remainingPickups.GetAndRemoveRandom(RNG), 0);

                for (int i = 1; i < cycleLength; i++)
                {
                    PickupIndex previousPickup = _recycleSteps[i - 1].PickupIndex;

                    PickupIndex[] transmutationGroup = PickupTransmutationManager.GetAvailableGroupFromPickupIndex(previousPickup);
                    if (transmutationGroup != null && transmutationGroup.Length > 0)
                        transmutationGroup = transmutationGroup.Where(remainingPickups.Contains).ToArray();

                    if (transmutationGroup == null || transmutationGroup.Length == 0 || RNG.nextNormalizedFloat <= RECYCLE_IGNORE_GROUP_CHANCE)
                    {
                        registerPickupForCycle(remainingPickups.GetAndRemoveRandom(RNG), i);
                    }
                    else
                    {
                        PickupIndex pickup = RNG.NextElementUniform(transmutationGroup);
                        registerPickupForCycle(pickup, i);
                        remainingPickups.Remove(pickup);
                    }
                }

                if (remainingPickups.Count > 0)
                {
                    Log.Error($"Didn't use all pickups, missing: [{string.Join(",", remainingPickups)}]");
                }
            }
        }

        public override void Serialize(NetworkWriter writer)
        {
            base.Serialize(writer);

            writer.Write(_isSeeded);
            if (_isSeeded)
            {
                writer.WritePackedUInt32((uint)_recycleSteps.Length);
                foreach (RecycleStep step in _recycleSteps)
                {
                    writer.Write(step.PickupIndex);
                    writer.Write(step.Duration);
                }
            }
        }

        public override void Deserialize(NetworkReader reader)
        {
            base.Deserialize(reader);

            _isSeeded = reader.ReadBoolean();
            if (_isSeeded)
            {
                uint stepCount = reader.ReadPackedUInt32();
                _recycleSteps = new RecycleStep[stepCount];
                _recycleStepIndexLookup = new Dictionary<PickupIndex, int>((int)stepCount);

                for (int i = 0; i < stepCount; i++)
                {
                    PickupIndex pickup = reader.ReadPickupIndex();
                    float duration = reader.ReadSingle();

                    _recycleSteps[i] = new RecycleStep(pickup, duration);
                    _recycleStepIndexLookup.Add(pickup, i);
                }
            }
        }

        public override void OnStart()
        {
            On.RoR2.GenericPickupController.Start += GenericPickupController_Start;
            GameObject.FindObjectsOfType<GenericPickupController>().TryDo(tryAddComponent);
        }

        void GenericPickupController_Start(On.RoR2.GenericPickupController.orig_Start orig, GenericPickupController self)
        {
            orig(self);
            tryAddComponent(self);
        }

        void tryAddComponent(GenericPickupController pickupController)
        {
            if (!pickupController.GetComponent<RecycleOnTimer>())
            {
                RecycleOnTimer recycleOnTimer = pickupController.gameObject.AddComponent<RecycleOnTimer>();
                recycleOnTimer.Initialize(this);
            }
        }

        public override void OnEnd()
        {
            On.RoR2.GenericPickupController.Start -= GenericPickupController_Start;

            InstanceUtils.DestroyAllTrackedInstances<RecycleOnTimer>();
        }

        float getRecycleTimer(PickupIndex pickup)
        {
            if (_isSeeded)
            {
                if (_recycleStepIndexLookup.TryGetValue(pickup, out int stepIndex))
                {
                    return _recycleSteps[stepIndex].Duration * _recycleTimerScale.Value;
                }
                else
                {
                    Log.Error($"Pickup not accounted for: {pickup}");
                }
            }

            return generateBaseRecycleDuration(RoR2Application.rng) * _recycleTimerScale.Value;
        }

        PickupIndex getNextPickup(PickupIndex current)
        {
            if (_isSeeded)
            {
                if (_recycleStepIndexLookup.TryGetValue(current, out int stepIndex))
                {
                    return _recycleSteps[(stepIndex + 1) % _recycleSteps.Length].PickupIndex;
                }
                else
                {
                    Log.Error($"Pickup not accounted for: {current}");
                }
            }

            PickupIndex[] availablePickups = PickupTransmutationManager.GetAvailableGroupFromPickupIndex(current);
            availablePickups = availablePickups?.Where(p => p != current).ToArray();
            if (availablePickups == null || availablePickups.Length == 0 || RoR2Application.rng.nextNormalizedFloat <= RECYCLE_IGNORE_GROUP_CHANCE)
                availablePickups = _allAvailablePickupIndices;

            return RoR2Application.rng.NextElementUniform(availablePickups);
        }

        [RequireComponent(typeof(GenericPickupController))]
        sealed class RecycleOnTimer : MonoBehaviour
        {
            RepeatedlyRecycleItems _effectInstance;
            GenericPickupController _pickupController;

            const float INITIAL_RECYCLE_TIME_MULTIPLIER = 0.75f;
            float _recycleTimer;

            void Awake()
            {
                InstanceTracker.Add(this);
                _pickupController = GetComponent<GenericPickupController>();
            }

            public void Initialize(RepeatedlyRecycleItems effectInstance)
            {
                _effectInstance = effectInstance;

                startRecycleTimer();
                _recycleTimer = Mathf.Max(MIN_RECYCLE_DURATION, _recycleTimer * INITIAL_RECYCLE_TIME_MULTIPLIER);
            }

            void startRecycleTimer()
            {
                _recycleTimer = _effectInstance.getRecycleTimer(_pickupController.pickupIndex);
            }

            void setToRandomItem()
            {
                _pickupController.NetworkpickupIndex = _effectInstance.getNextPickup(_pickupController.pickupIndex);
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
    }
}
