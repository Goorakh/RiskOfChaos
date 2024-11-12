using HG;
using Newtonsoft.Json;
using RiskOfChaos.EffectHandling;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.EffectHandling.EffectComponents;
using RiskOfChaos.Patches;
using RiskOfChaos.SaveHandling;
using RiskOfChaos.Utilities.Extensions;
using RoR2;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace RiskOfChaos.EffectDefinitions.World.Items
{
    [ChaosTimedEffect("all_void_potentials", TimedEffectType.UntilStageEnd, AllowDuplicates = false)]
    [EffectConfigBackwardsCompatibility("Effect: All Items Are Void Potentials (Lasts 1 stage)")]
    public sealed class AllVoidPotentials : NetworkBehaviour
    {
        public delegate void OverrideAllowChoicesDelegate(PickupIndex originalPickup, ref bool allowChoices);
        public static event OverrideAllowChoicesDelegate OverrideAllowChoices;

        static GameObject _optionPickupPrefab;

        [SystemInitializer]
        static void Init()
        {
            AsyncOperationHandle<GameObject> optionPickupLoad = Addressables.LoadAssetAsync<GameObject>("RoR2/DLC1/OptionPickup/OptionPickup.prefab");
            optionPickupLoad.OnSuccess(optionPickupPrefab =>
            {
                _optionPickupPrefab = optionPickupPrefab;
            });
        }

        [EffectCanActivate]
        static bool CanActivate()
        {
            return _optionPickupPrefab && (!RunArtifactManager.instance || !RunArtifactManager.instance.IsArtifactEnabled(RoR2Content.Artifacts.commandArtifactDef));
        }

        ChaosEffectComponent _effectComponent;

        readonly Dictionary<PickupIndex, PickupOptionGenerator> _pickupOptionGenerators = [];

        [SerializedMember("og")]
        PickupOptionGenerator[] serializedPickupOptionGenerators
        {
            get
            {
                return _pickupOptionGenerators.Values.ToArray();
            }
            set
            {
                value ??= [];

                _pickupOptionGenerators.Clear();
                _pickupOptionGenerators.EnsureCapacity(value.Length);

                foreach (PickupOptionGenerator optionGenerator in value)
                {
                    if (!optionGenerator.SourcePickup.isValid)
                        continue;

                    if (_pickupOptionGenerators.ContainsKey(optionGenerator.SourcePickup))
                    {
                        Log.Warning($"Duplicate source pickups for {optionGenerator.SourcePickup} in save data, ignoring");
                        continue;
                    }

                    _pickupOptionGenerators.Add(optionGenerator.SourcePickup, optionGenerator);
                }

                _pickupOptionGenerators.TrimExcess();
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

            _pickupOptionGenerators.EnsureCapacity(PickupCatalog.pickupCount);
            foreach (PickupDef pickup in PickupCatalog.allPickups)
            {
                PickupOptionGenerator optionGenerator = new PickupOptionGenerator(pickup.pickupIndex, new Xoroshiro128Plus(rng.nextUlong));

                if (!optionGenerator.HasAnyOptions)
                    continue;

                if (_pickupOptionGenerators.ContainsKey(pickup.pickupIndex))
                {
                    Log.Error($"Duplicate option generators for {pickup.pickupIndex}");
                    continue;
                }

                _pickupOptionGenerators.Add(pickup.pickupIndex, optionGenerator);
            }

            _pickupOptionGenerators.TrimExcess();
        }

        void Start()
        {
            if (NetworkServer.active)
            {
                PickupDropletControllerHooks.ModifyCreatePickup += modifyPickupInfo;
            }
        }

        void OnDestroy()
        {
            PickupDropletControllerHooks.ModifyCreatePickup -= modifyPickupInfo;

            _pickupOptionGenerators.Clear();
        }

        bool tryGetAvailableOptionsFor(PickupIndex sourcePickup, out PickupIndex[] options)
        {
            if (sourcePickup.isValid && _pickupOptionGenerators.TryGetValue(sourcePickup, out PickupOptionGenerator optionGenerator))
            {
                options = optionGenerator.GenerateOptions();
                return options != null && options.Length > 0;
            }
            else
            {
                options = null;
                return false;
            }
        }

        PickupPickerController.Option[] getPickableOptions(PickupIndex sourcePickup)
        {
            const int NUM_ADDITIONAL_OPTIONS = 2;

            bool allowChoices = true;
            OverrideAllowChoices?.Invoke(sourcePickup, ref allowChoices);

            // Guarantee the original item is always an option
            PickupPickerController.Option guaranteedOption = new PickupPickerController.Option
            {
                available = true,
                pickupIndex = sourcePickup
            };

            if (allowChoices && tryGetAvailableOptionsFor(sourcePickup, out PickupIndex[] availableOptions))
            {
                int numExtraOptions = Mathf.Min(availableOptions.Length, NUM_ADDITIONAL_OPTIONS);
                PickupPickerController.Option[] options = new PickupPickerController.Option[1 + numExtraOptions];

                options[0] = guaranteedOption;

                for (int i = 0; i < numExtraOptions; i++)
                {
                    options[i + 1] = new PickupPickerController.Option
                    {
                        available = true,
                        pickupIndex = availableOptions[i]
                    };
                }

                return options;
            }
            else
            {
                return [guaranteedOption];
            }
        }

        void modifyPickupInfo(ref GenericPickupController.CreatePickupInfo createPickupInfo)
        {
            if (createPickupInfo.pickerOptions != null)
                return;

            PickupIndex pickupIndex = createPickupInfo.pickupIndex;
            if (!pickupIndex.isValid)
                return;

            createPickupInfo.pickerOptions = getPickableOptions(pickupIndex);
            createPickupInfo.prefabOverride = _optionPickupPrefab;
        }

        [Serializable]
        [JsonObject(MemberSerialization.OptIn)]
        sealed class PickupOptionGenerator
        {
            PickupIndex _sourcePickup;

            [JsonProperty("s")]
            public PickupIndex SourcePickup
            {
                get
                {
                    return _sourcePickup;
                }
                set
                {
                    _sourcePickup = value;

                    PickupIndex[] pickupGroup = PickupTransmutationManager.GetAvailableGroupFromPickupIndex(SourcePickup) ?? [];
                    if (pickupGroup.Length > 0)
                    {
                        int sourcePickupIndex = Array.IndexOf(pickupGroup, SourcePickup);
                        if (sourcePickupIndex != -1)
                        {
                            pickupGroup = ArrayUtils.Clone(pickupGroup);
                            ArrayUtils.ArrayRemoveAtAndResize(ref pickupGroup, sourcePickupIndex);
                        }
                    }

                    _availableOptions = pickupGroup;
                }
            }

            [JsonProperty("rng")]
            readonly Xoroshiro128Plus _rng;

            PickupIndex[] _availableOptions;

            public bool HasAnyOptions => _availableOptions != null && _availableOptions.Length > 0;

            public PickupOptionGenerator(PickupIndex sourcePickup, Xoroshiro128Plus rng)
            {
                SourcePickup = sourcePickup;
                _rng = rng;
            }

            public PickupOptionGenerator()
            {
            }

            public PickupIndex[] GenerateOptions()
            {
                if (!HasAnyOptions)
                    return [];

                PickupIndex[] shuffledPickupIndices = ArrayUtils.Clone(_availableOptions);
                Util.ShuffleArray(shuffledPickupIndices, _rng.Branch());
                return shuffledPickupIndices;
            }
        }
    }
}
