using Newtonsoft.Json;
using RiskOfChaos.EffectHandling;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.EffectHandling.EffectComponents;
using RiskOfChaos.EffectUtils.World.Items;
using RiskOfChaos.Patches;
using RiskOfChaos.SaveHandling;
using RiskOfChaos.Utilities.Extensions;
using RoR2;
using System;
using System.Collections;
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
        static GameObject _optionPickupPrefab;

        static PickupTransmutationDropTable[] _pickupTransmutationDropTables = [];

        [SystemInitializer(typeof(PickupCatalog), typeof(PickupTransmutationManager))]
        static IEnumerator Init()
        {
            AsyncOperationHandle<GameObject> optionPickupLoad = Addressables.LoadAssetAsync<GameObject>("RoR2/DLC1/OptionPickup/OptionPickup.prefab");
            optionPickupLoad.OnSuccess(optionPickupPrefab =>
            {
                _optionPickupPrefab = optionPickupPrefab;
            });

            Log.Debug("Creating transmutation drop tables...");

            _pickupTransmutationDropTables = new PickupTransmutationDropTable[PickupCatalog.pickupCount];
            for (int i = 0; i < PickupCatalog.pickupCount; i++)
            {
                PickupIndex pickupIndex = new PickupIndex(i);

                PickupTransmutationDropTable transmutationDropTable = ScriptableObject.CreateInstance<PickupTransmutationDropTable>();
                transmutationDropTable.name = $"dt{PickupCatalog.GetPickupDef(pickupIndex).internalName}Transmutation";
                transmutationDropTable.canDropBeReplaced = false;
                transmutationDropTable.SourcePickup = pickupIndex;

                _pickupTransmutationDropTables[i] = transmutationDropTable;

                yield return null;
            }

            Log.Debug($"Created {_pickupTransmutationDropTables.Length} transmutation drop tables");
        }

        [EffectCanActivate]
        static bool CanActivate()
        {
            return _optionPickupPrefab && _pickupTransmutationDropTables.Length > 0 && (!RunArtifactManager.instance || !RunArtifactManager.instance.IsArtifactEnabled(RoR2Content.Artifacts.Command));
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

                _pickupOptionGenerators.EnsureCapacity(_pickupOptionGenerators.Count + value.Length);

                foreach (PickupOptionGenerator optionGenerator in value)
                {
                    if (!optionGenerator.SourcePickup.isValid)
                        continue;

                    _pickupOptionGenerators[optionGenerator.SourcePickup] = optionGenerator;
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

        bool tryGetAvailableOptionsFor(PickupIndex sourcePickup, int maxOptionCount, out PickupIndex[] options)
        {
            if (sourcePickup.isValid && _pickupOptionGenerators.TryGetValue(sourcePickup, out PickupOptionGenerator optionGenerator))
            {
                options = optionGenerator.GenerateOptions(maxOptionCount);
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

            // Guarantee the original item is always an option
            PickupPickerController.Option guaranteedOption = new PickupPickerController.Option
            {
                pickupIndex = sourcePickup,
                available = true
            };

            PickupPickerController.Option[] additionalOptions = [];
            if (tryGetAvailableOptionsFor(sourcePickup, NUM_ADDITIONAL_OPTIONS, out PickupIndex[] availablePickupOptions))
            {
                int numExtraOptions = availablePickupOptions.Length;
                additionalOptions = new PickupPickerController.Option[numExtraOptions];

                for (int i = 0; i < numExtraOptions; i++)
                {
                    PickupIndex pickupIndex = sourcePickup;
                    if (allowChoices)
                    {
                        pickupIndex = availablePickupOptions[i];
                    }

                    additionalOptions[i] = new PickupPickerController.Option
                    {
                        pickupIndex = pickupIndex,
                        available = true
                    };
                }
            }

            return [guaranteedOption, .. additionalOptions];
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
                }
            }

            [JsonProperty("rng")]
            readonly Xoroshiro128Plus _rng;

            public PickupOptionGenerator(PickupIndex sourcePickup, Xoroshiro128Plus rng)
            {
                SourcePickup = sourcePickup;
                _rng = rng;
            }

            public PickupOptionGenerator()
            {
            }

            public PickupIndex[] GenerateOptions(int maxOptionCount)
            {
                int index = _sourcePickup.value;
                if (index < 0 || index >= _pickupTransmutationDropTables.Length)
                    return [];

                PickupTransmutationDropTable dropTable = _pickupTransmutationDropTables[index];
                return dropTable.GenerateUniqueDrops(maxOptionCount, new Xoroshiro128Plus(_rng.nextUlong));
            }
        }
    }
}
