using HG;
using RiskOfChaos.EffectHandling;
using RiskOfChaos.EffectHandling.Controllers;
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

        static bool _appliedPatches = false;

        static void tryApplyPatches()
        {
            if (_appliedPatches)
                return;

            On.RoR2.PickupDropletController.CreatePickup += (orig, self) =>
            {
                foreach (AllVoidPotentials effect in TimedChaosEffectHandler.Instance.GetActiveEffectInstancesOfType<AllVoidPotentials>())
                {
                    effect.modifyPickupInfo(ref self.createPickupInfo);
                }

                orig(self);
            };

            _appliedPatches = true;
        }

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
                    _availableOptions = [];
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
                    return [];

                PickupIndex[] shuffledPickupIndices = (PickupIndex[])_availableOptions.Clone();
                Util.ShuffleArray(shuffledPickupIndices, _rng.Branch());
                return shuffledPickupIndices;
            }
        }

        readonly Dictionary<PickupIndex, PickupOptionGenerator> _pickupOptionGenerators = [];

        public override void OnPreStartServer()
        {
            base.OnPreStartServer();

            foreach (PickupDef pickup in PickupCatalog.allPickups)
            {
                PickupOptionGenerator optionGenerator = new PickupOptionGenerator(pickup.pickupIndex, RNG.Branch());

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
            tryApplyPatches();
        }

        public override void OnEnd()
        {
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

            if (allowChoices && tryGetAvailableOptionsFor(sourcePickup, out PickupIndex[] availableOptions))
            {
                int numExtraOptions = Math.Min(availableOptions.Length, NUM_ADDITIONAL_OPTIONS);
                PickupPickerController.Option[] options = new PickupPickerController.Option[1 + numExtraOptions];

                // Guarantee the original item is always an option
                options[0] = new PickupPickerController.Option
                {
                    available = true,
                    pickupIndex = sourcePickup
                };

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
                return [
                    new PickupPickerController.Option
                    {
                        available = true,
                        pickupIndex = sourcePickup
                    }
                ];
            }
        }

        void modifyPickupInfo(ref GenericPickupController.CreatePickupInfo createPickupInfo)
        {
            if (createPickupInfo.pickerOptions != null || (createPickupInfo.artifactFlag & GenericPickupController.PickupArtifactFlag.DELUSION) != 0)
                return;

            PickupIndex pickupIndex = createPickupInfo.pickupIndex;
            if (!pickupIndex.isValid)
                return;

            createPickupInfo.pickerOptions = getPickableOptions(pickupIndex);
            createPickupInfo.prefabOverride = _optionPickupPrefab;
        }
    }
}
