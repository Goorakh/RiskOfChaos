using HG;
using RiskOfChaos.EffectHandling;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.Utilities.Extensions;
using RoR2;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectDefinitions.World
{
    [ChaosTimedEffect("all_void_potentials", TimedEffectType.UntilStageEnd, AllowDuplicates = false)]
    [EffectConfigBackwardsCompatibility("Effect: All Items Are Void Potentials (Lasts 1 stage)")]
    public sealed class AllVoidPotentials : TimedEffect
    {
        public delegate void OverrideAllowChoicesDelegate(PickupIndex originalPickup, ref bool allowChoices);
        public static event OverrideAllowChoicesDelegate OverrideAllowChoices;

        static readonly GameObject _optionPickupPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/DLC1/OptionPickup/OptionPickup.prefab").WaitForCompletion();

        [EffectCanActivate]
        static bool CanActivate()
        {
            return _optionPickupPrefab && (!RunArtifactManager.instance || !RunArtifactManager.instance.IsArtifactEnabled(RoR2Content.Artifacts.commandArtifactDef));
        }

        sealed class PickupOptionGenerator
        {
            public readonly PickupIndex SourcePickup;
            readonly Xoroshiro128Plus _rng;

            readonly PickupIndex[] _availableOptions;

            public bool HasAnyOptions => _availableOptions != null && _availableOptions.Length > 0;

            public PickupOptionGenerator(PickupIndex sourcePickup, Xoroshiro128Plus rng)
            {
                SourcePickup = sourcePickup;
                _rng = rng;

                PickupIndex[] pickupGroup = PickupTransmutationManager.GetAvailableGroupFromPickupIndex(SourcePickup);
                if (pickupGroup != null && pickupGroup.Length > 0)
                {
                    int sourcePickupIndex = Array.IndexOf(pickupGroup, SourcePickup);
                    if (sourcePickupIndex != -1)
                    {
                        pickupGroup = (PickupIndex[])pickupGroup.Clone();
                        ArrayUtils.ArrayRemoveAtAndResize(ref pickupGroup, sourcePickupIndex);
                    }

                    _availableOptions = pickupGroup;
                }
                else
                {
                    _availableOptions = Array.Empty<PickupIndex>();
                }
            }

            public PickupOptionGenerator(NetworkReader reader) : this(reader.ReadPickupIndex(), reader.ReadRNG())
            {
            }

            public void Serialize(NetworkWriter writer)
            {
                writer.Write(SourcePickup);
                writer.WriteRNG(_rng);
            }

            public PickupIndex[] GenerateOptions()
            {
                if (!HasAnyOptions)
                    return Array.Empty<PickupIndex>();

                PickupIndex[] shuffledPickupIndices = (PickupIndex[])_availableOptions.Clone();
                Util.ShuffleArray(shuffledPickupIndices, new Xoroshiro128Plus(_rng.nextUlong));
                return shuffledPickupIndices;
            }
        }

        readonly Dictionary<PickupIndex, PickupOptionGenerator> _pickupOptionGenerators = new Dictionary<PickupIndex, PickupOptionGenerator>();

        public override void OnPreStartServer()
        {
            base.OnPreStartServer();

            foreach (PickupDef pickup in PickupCatalog.allPickups)
            {
                PickupOptionGenerator optionGenerator = new PickupOptionGenerator(pickup.pickupIndex, new Xoroshiro128Plus(RNG.nextUlong));

                if (!optionGenerator.HasAnyOptions)
                    continue;

                if (_pickupOptionGenerators.ContainsKey(pickup.pickupIndex))
                {
                    Log.Error($"Duplicate option generators for {pickup.internalName}");
                    continue;
                }

                _pickupOptionGenerators.Add(pickup.pickupIndex, optionGenerator);
            }
        }

        public override void Serialize(NetworkWriter writer)
        {
            base.Serialize(writer);

            writer.WritePackedUInt32((uint)_pickupOptionGenerators.Count);
            foreach (PickupOptionGenerator optionGenerator in _pickupOptionGenerators.Values)
            {
                optionGenerator.Serialize(writer);
            }
        }

        public override void Deserialize(NetworkReader reader)
        {
            base.Deserialize(reader);

            uint generatorCount = reader.ReadPackedUInt32();
            for (uint i = 0; i < generatorCount; i++)
            {
                PickupOptionGenerator optionGenerator = new PickupOptionGenerator(reader);
                _pickupOptionGenerators[optionGenerator.SourcePickup] = optionGenerator;
            }
        }

        public override void OnStart()
        {
            PickupDropletController.onDropletHitGroundServer += onDropletHitGroundServer;
        }

        public override void OnEnd()
        {
            PickupDropletController.onDropletHitGroundServer -= onDropletHitGroundServer;
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

        void onDropletHitGroundServer(ref GenericPickupController.CreatePickupInfo createPickupInfo, ref bool shouldSpawn)
        {
            if (!shouldSpawn)
                return;

            PickupIndex pickupIndex = createPickupInfo.pickupIndex;
            if (!pickupIndex.isValid)
                return;

            PickupDef pickupDef = PickupCatalog.GetPickupDef(pickupIndex);
            if (pickupDef == null)
                return;

            // Only create options if this pickup won't already have any
            if (createPickupInfo.pickerOptions != null)
                return;

            GameObject dropletDisplay = GameObject.Instantiate(_optionPickupPrefab, createPickupInfo.position, createPickupInfo.rotation);

            if (dropletDisplay.TryGetComponent(out PickupIndexNetworker pickupIndexNetworker))
            {
                pickupIndexNetworker.NetworkpickupIndex = pickupIndex;
            }

            if (dropletDisplay.TryGetComponent(out PickupPickerController pickupPickerController))
            {
                const int NUM_ADDITIONAL_OPTIONS = 2;

                bool allowChoices = true;
                OverrideAllowChoices?.Invoke(pickupIndex, ref allowChoices);

                if (allowChoices && tryGetAvailableOptionsFor(pickupIndex, out PickupIndex[] availableOptions))
                {
                    int numExtraOptions = Math.Min(availableOptions.Length, NUM_ADDITIONAL_OPTIONS);
                    PickupPickerController.Option[] options = new PickupPickerController.Option[1 + numExtraOptions];

                    // Guarantee the original item is always an option
                    options[0] = new PickupPickerController.Option
                    {
                        available = true,
                        pickupIndex = pickupIndex
                    };

                    for (int i = 0; i < numExtraOptions; i++)
                    {
                        options[i + 1] = new PickupPickerController.Option
                        {
                            available = true,
                            pickupIndex = availableOptions[i]
                        };
                    }

                    pickupPickerController.SetOptionsServer(options);
                }
                else
                {
                    pickupPickerController.SetOptionsServer(new PickupPickerController.Option[]
                    {
                        new PickupPickerController.Option
                        {
                            available = true,
                            pickupIndex = pickupIndex
                        }
                    });
                }
            }

            NetworkServer.Spawn(dropletDisplay);

            shouldSpawn = false;
        }
    }
}
