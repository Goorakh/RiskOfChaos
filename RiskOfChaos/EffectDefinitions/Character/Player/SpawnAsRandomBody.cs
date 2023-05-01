using RiskOfChaos.EffectDefinitions.World.Spawn;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.Patches;
using RiskOfChaos.Utilities;
using RiskOfChaos.Utilities.Extensions;
using RoR2;
using RoR2.Skills;
using System.Linq;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace RiskOfChaos.EffectDefinitions.Character.Player
{
    [ChaosEffect("spawn_as_random_body", DefaultSelectionWeight = 0.2f)]
    public sealed class SpawnAsRandomBody : GenericSpawnEffect<GameObject>
    {
        class BodyPrefabEntry : SpawnEntry
        {
            public BodyPrefabEntry(GameObject[] items, float weight) : base(items, weight)
            {
            }

            public BodyPrefabEntry(GameObject item, float weight) : base(item, weight)
            {
            }

            protected override bool isItemAvailable(GameObject item)
            {
                return base.isItemAvailable(item) && item && ExpansionUtils.IsObjectExpansionAvailable(item);
            }
        }

        static BodyPrefabEntry[] _availableBodyPrefabs;

        [SystemInitializer(typeof(BodyCatalog))]
        static void Init()
        {
            _availableBodyPrefabs = BodyCatalog.allBodyPrefabs.Where(bodyPrefabObj =>
            {
                if (!bodyPrefabObj || !bodyPrefabObj.TryGetComponent(out CharacterBody bodyPrefab))
                    return false;

                if (bodyPrefab.baseMoveSpeed == 0f)
                {
#if DEBUG
                    Log.Debug($"Excluding body {bodyPrefab}: Immobile");
#endif
                    return false;
                }

                if ((bodyPrefab.bodyFlags & CharacterBody.BodyFlags.Masterless) != 0)
                {
#if DEBUG
                    Log.Debug($"Excluding body {bodyPrefab}: Masterless");
#endif
                    return false;
                }

                if (!bodyPrefab.GetComponent<Interactor>())
                {
#if DEBUG
                    Log.Debug($"Excluding body {bodyPrefab}: Missing Interactor component");
#endif
                    return false;
                }

                if (!bodyPrefab.TryGetComponent(out ModelLocator modelLocator) || !modelLocator.modelTransform)
                {
#if DEBUG
                    Log.Debug($"Excluding body {bodyPrefab}: null model");
#endif
                    return false;
                }

                if (modelLocator.modelTransform.childCount == 0)
                {
#if DEBUG
                    Log.Debug($"Excluding body {bodyPrefab}: empty model");
#endif
                    return false;
                }

                // Required for certain effects to work properly (ex Doppelganger & Mitosis)
                if (MasterCatalog.FindAiMasterIndexForBody(bodyPrefab.bodyIndex) == MasterCatalog.MasterIndex.none)
                {
#if DEBUG
                    Log.Debug($"Excluding body {bodyPrefab}: Missing AI controller");
#endif
                    return false;
                }

                switch (bodyPrefab.name)
                {
                    case "AncientWispBody": // Unfinished
                    case "ArchWispBody": // Unfinished
                    case "Assassin2Body": // Unfinished
                    case "BeetleBody": // Secondary will trap you forever
                    case "BeetleCrystalBody": // Secondary will trap you forever
                    case "BeetleGuardAllyBody": // Beetle guard reskin
                    case "BeetleGuardCrystalBody": // Beetle guard reskin
                    case "BeetleQueen2Body": // Way too annoying to move, also interactor range is terrible
                    case "Drone2Body": // Healing drone, no attacks
                    case "DroneCommanderBody": // No attacks without item
                    case "EngiWalkerTurretBody": // Can't be assigned client authority
                    case "JellyfishBody": // Dies on attacking
                    case "NullifierAllyBody": // Ally reskin
                    case "VoidJailerAllyBody": // Ally reskin
                    case "VoidMegaCrabAllyBody": // Ally reskin
                    case "WispSoulBody": // Dies on a timer
#if DEBUG
                        Log.Debug($"Excluding body {bodyPrefab}: blacklist");
#endif
                        return false;
                }

#if DEBUG
                Log.Debug($"Including body {bodyPrefab}");
#endif

                if (!bodyPrefab.GetComponent<EquipmentSlot>())
                {
#if DEBUG
                    Log.Debug($"Added EquipmentSlot component to body prefab {bodyPrefab}");
#endif

                    bodyPrefab.gameObject.AddComponent<EquipmentSlot>();
                }

                return true;
            }).Select(bodyPrefabObj =>
            {
                CharacterBody bodyPrefab = bodyPrefabObj.GetComponent<CharacterBody>();

                float weight;
                if (bodyPrefab.isChampion)
                {
                    weight = 0.3f;
                }
                else
                {
                    weight = 1f;
                }

                return new BodyPrefabEntry(bodyPrefabObj, weight);
            }).ToArray();

#if DEBUG
            Log.Debug($"Including {_availableBodyPrefabs.Length} character bodies");
#endif

            static void setBodyInteractorDistance(string assetPath, float distance)
            {
                GameObject bodyPrefab = Addressables.LoadAssetAsync<GameObject>(assetPath).WaitForCompletion();
                if (!bodyPrefab)
                {
                    Log.Warning($"Null prefab at path {assetPath}");
                    return;
                }

                if (!bodyPrefab.GetComponent<CharacterBody>())
                {
                    Log.Warning($"{bodyPrefab} ({assetPath}) has no body component");
                    return;
                }

                Interactor interactor = bodyPrefab.GetComponent<Interactor>();
                if (!interactor)
                {
                    Log.Warning($"{bodyPrefab} ({assetPath}) has no interactor component");
                    return;
                }

                interactor.maxInteractionDistance = distance;

#if DEBUG
                Log.Debug($"Set interaction distance for {bodyPrefab} to {distance}");
#endif
            }

            setBodyInteractorDistance("RoR2/Base/Brother/BrotherBody.prefab", 10f);
            setBodyInteractorDistance("RoR2/Junk/BrotherGlass/BrotherGlassBody.prefab", 10f);
            setBodyInteractorDistance("RoR2/Base/Brother/BrotherHurtBody.prefab", 10f);
            setBodyInteractorDistance("RoR2/Base/Golem/GolemBody.prefab", 7f);
            setBodyInteractorDistance("RoR2/Base/Gravekeeper/GravekeeperBody.prefab", 14f);
            setBodyInteractorDistance("RoR2/DLC1/Gup/GupBody.prefab", 8f);
            setBodyInteractorDistance("RoR2/Base/ImpBoss/ImpBossBody.prefab", 12f);
            setBodyInteractorDistance("RoR2/Base/LemurianBruiser/LemurianBruiserBody.prefab", 10f);
            setBodyInteractorDistance("RoR2/Base/LunarGolem/LunarGolemBody.prefab", 10f);
            setBodyInteractorDistance("RoR2/Base/LunarWisp/LunarWispBody.prefab", 7f);
            setBodyInteractorDistance("RoR2/Base/Drones/MegaDroneBody.prefab", 10f);
            setBodyInteractorDistance("RoR2/Base/Nullifier/NullifierBody.prefab", 9f);
            setBodyInteractorDistance("RoR2/Base/Parent/ParentBody.prefab", 13f);
            setBodyInteractorDistance("RoR2/Base/Scav/ScavBody.prefab", 12f);
            setBodyInteractorDistance("RoR2/Base/ScavLunar/ScavLunar1Body.prefab", 12f);
            setBodyInteractorDistance("RoR2/Base/ScavLunar/ScavLunar2Body.prefab", 12f);
            setBodyInteractorDistance("RoR2/Base/ScavLunar/ScavLunar3Body.prefab", 12f);
            setBodyInteractorDistance("RoR2/Base/ScavLunar/ScavLunar4Body.prefab", 12f);
            setBodyInteractorDistance("RoR2/Base/Titan/TitanBody.prefab", 18f);
            setBodyInteractorDistance("RoR2/Base/Titan/TitanGoldBody.prefab", 18f);
            setBodyInteractorDistance("RoR2/DLC1/VoidJailer/VoidJailerBody.prefab", 10f);
            setBodyInteractorDistance("RoR2/DLC1/VoidMegaCrab/VoidMegaCrabBody.prefab", 13f);
        }

        [EffectCanActivate]
        static bool CanActivate()
        {
            return areAnyAvailable(_availableBodyPrefabs);
        }

        public override void OnStart()
        {
            foreach (CharacterMaster playerMaster in PlayerUtils.GetAllPlayerMasters(false))
            {
                randomizePlayerBody(playerMaster, new Xoroshiro128Plus(RNG.nextUlong));
            }
        }

        void randomizePlayerBody(CharacterMaster master, Xoroshiro128Plus rng)
        {
            WeightedSelection<BodyPrefabEntry> weightedSelection = getWeightedEntrySelection(_availableBodyPrefabs);
            for (int i = 0; i < weightedSelection.choices.Length; i++)
            {
                if (weightedSelection.choices[i].value.ContainsItem(master.bodyPrefab))
                {
                    weightedSelection.RemoveChoice(i);
                    break;
                }
            }

            if (weightedSelection.Count == 0)
            {
                Log.Warning($"No valid body prefab found for {Util.GetBestMasterName(master)}");
                return;
            }

            master.bodyPrefab = weightedSelection.Evaluate(rng.nextNormalizedFloat).GetItem(rng);

#if DEBUG
            Log.Debug($"Override body prefab for {Util.GetBestMasterName(master)}: {master.bodyPrefab}");
#endif

            if (!master.IsDeadAndOutOfLivesServer())
            {
                CharacterBody body = master.GetBody();
                if (body)
                {
                    bool metamorphosisEnabled = RunArtifactManager.instance && RunArtifactManager.instance.IsArtifactEnabled(RoR2Content.Artifacts.randomSurvivorOnRespawnArtifactDef);

                    VehicleSeat oldVehicleSeat = body.currentVehicle;
                    if (oldVehicleSeat)
                    {
                        oldVehicleSeat.EjectPassenger();
                    }

                    PreventMetamorphosisRespawn.PreventionEnabled = metamorphosisEnabled;

                    CharacterBody newBody = master.Respawn(body.footPosition, body.GetRotation());

                    PreventMetamorphosisRespawn.PreventionEnabled = false;

                    if (oldVehicleSeat)
                    {
                        oldVehicleSeat.AssignPassenger(newBody.gameObject);
                    }
                }
            }

            BodyIndex bodyIndex = master.bodyPrefab.GetComponent<CharacterBody>().bodyIndex;

#pragma warning disable Publicizer001 // Accessing a member that was not originally public
            Loadout.BodyLoadoutManager.BodyInfo bodyInfo = Loadout.BodyLoadoutManager.allBodyInfos[(int)bodyIndex];
#pragma warning restore Publicizer001 // Accessing a member that was not originally public

            Loadout loadout = new Loadout();
            for (int i = 0; i < bodyInfo.skillSlotCount; i++)
            {
                SkillFamily.Variant[] skillVariants = bodyInfo.prefabSkillSlots[i].skillFamily.variants;

                if (skillVariants.Length > 0)
                {
                    loadout.bodyLoadoutManager.SetSkillVariant(bodyIndex, i, (uint)RNG.RangeInt(0, skillVariants.Length));
                }
            }

            int bodySkinCount = BodyCatalog.GetBodySkins(bodyIndex).Length;
            if (bodySkinCount > 0)
            {
                loadout.bodyLoadoutManager.SetSkinIndex(bodyIndex, (uint)RNG.RangeInt(0, bodySkinCount));
            }

            master.SetLoadoutServer(loadout);
        }
    }
}
