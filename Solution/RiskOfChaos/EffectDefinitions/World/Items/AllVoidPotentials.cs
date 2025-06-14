using Newtonsoft.Json;
using RiskOfChaos.EffectHandling;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.EffectHandling.EffectComponents;
using RiskOfChaos.EffectUtils.World.Items;
using RiskOfChaos.Patches;
using RiskOfChaos.SaveHandling;
using RoR2;
using RoR2.ContentManagement;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectDefinitions.World.Items
{
    [ChaosTimedEffect("all_void_potentials", TimedEffectType.UntilStageEnd, AllowDuplicates = false)]
    [EffectConfigBackwardsCompatibility("Effect: All Items Are Void Potentials (Lasts 1 stage)")]
    public sealed class AllVoidPotentials : NetworkBehaviour
    {
        static PickupTransmutationDropTable[] _pickupTransmutationDropTables = [];

        [SystemInitializer(typeof(PickupCatalog), typeof(PickupTransmutationManager))]
        static void Init()
        {
            Log.Debug("Creating transmutation drop tables...");

            int numCreatedTransmutationTables = 0;

            _pickupTransmutationDropTables = new PickupTransmutationDropTable[PickupCatalog.pickupCount];
            for (int i = 0; i < PickupCatalog.pickupCount; i++)
            {
                PickupIndex pickupIndex = new PickupIndex(i);

                PickupDef pickupDef = PickupCatalog.GetPickupDef(pickupIndex);
                if (pickupDef == null)
                    continue;

                if (pickupDef.itemIndex == ItemIndex.None && pickupDef.equipmentIndex == EquipmentIndex.None)
                    continue;

                PickupIndex[] availablePickups = PickupTransmutationManager.GetGroupFromPickupIndex(pickupIndex);

                bool isTransmutable = false;
                if (availablePickups != null)
                {
                    foreach (PickupIndex transmutationPickupIndex in availablePickups)
                    {
                        if (transmutationPickupIndex != pickupIndex)
                        {
                            isTransmutable = true;
                            break;
                        }
                    }
                }

                if (!isTransmutable)
                    continue;

                PickupTransmutationDropTable transmutationDropTable = ScriptableObject.CreateInstance<PickupTransmutationDropTable>();
                transmutationDropTable.name = $"dt{pickupDef.internalName}Transmutation";
                transmutationDropTable.canDropBeReplaced = false;
                transmutationDropTable.SourcePickup = pickupIndex;

                _pickupTransmutationDropTables[i] = transmutationDropTable;
                numCreatedTransmutationTables++;
            }

            Log.Debug($"Created {numCreatedTransmutationTables} transmutation drop tables");
        }

        [EffectCanActivate]
        static bool CanActivate()
        {
            return _pickupTransmutationDropTables.Length > 0 && (!RunArtifactManager.instance || !RunArtifactManager.instance.IsArtifactEnabled(RoR2Content.Artifacts.Command));
        }

        AssetOrDirectReference<GameObject> _optionPickupPrefabReference;

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

            if (NetworkServer.active)
            {
                _optionPickupPrefabReference = new AssetOrDirectReference<GameObject>
                {
                    unloadType = AsyncReferenceHandleUnloadType.AtWill,
                    address = new AssetReferenceGameObject(AddressableGuids.RoR2_DLC1_OptionPickup_OptionPickup_prefab)
                };
            }
        }

        public override void OnStartServer()
        {
            base.OnStartServer();

            Xoroshiro128Plus rng = new Xoroshiro128Plus(_effectComponent.Rng.nextUlong);

            _pickupOptionGenerators.EnsureCapacity(PickupCatalog.pickupCount);
            foreach (PickupDef pickup in PickupCatalog.allPickups)
            {
                PickupIndex pickupIndex = pickup.pickupIndex;
                if (pickupIndex.value < 0 || pickupIndex.value >= _pickupTransmutationDropTables.Length)
                    continue;

                if (!_pickupTransmutationDropTables[pickupIndex.value])
                    continue;

                PickupOptionGenerator optionGenerator = new PickupOptionGenerator(pickupIndex, new Xoroshiro128Plus(rng.nextUlong));

                if (_pickupOptionGenerators.ContainsKey(pickupIndex))
                {
                    Log.Error($"Duplicate option generators for {pickupIndex}");
                    continue;
                }

                _pickupOptionGenerators.Add(pickupIndex, optionGenerator);
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

            _optionPickupPrefabReference?.Reset();
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
                options = [];
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
            createPickupInfo.prefabOverride = _optionPickupPrefabReference.WaitForCompletion();
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
                if (!dropTable)
                    return [];

                return dropTable.GenerateUniqueDrops(maxOptionCount, new Xoroshiro128Plus(_rng.nextUlong));
            }
        }
    }
}
