using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.Utilities;
using RoR2;
using RoR2.Navigation;
using RoR2.Skills;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RiskOfChaos.EffectDefinitions.World.Spawn.SpawnCharacter
{
    public abstract class GenericSpawnCombatCharacterEffect : GenericSpawnEffect<CharacterMaster>
    {
        protected class CharacterSpawnEntry : SpawnEntry
        {
            public CharacterSpawnEntry(CharacterMaster[] items, float weight) : base(items, weight)
            {
            }

            public CharacterSpawnEntry(CharacterMaster item, float weight) : base(item, weight)
            {
            }

            protected override bool isItemAvailable(CharacterMaster prefab)
            {
                return base.isItemAvailable(prefab) && prefab && ExpansionUtils.IsCharacterMasterExpansionAvailable(prefab.gameObject);
            }
        }

        protected static IEnumerable<CharacterMaster> getAllValidMasterPrefabs()
        {
            return MasterCatalog.allAiMasters.Where(masterPrefab =>
            {
                if (!masterPrefab)
                    return false;

                if (!masterPrefab.bodyPrefab || !masterPrefab.bodyPrefab.TryGetComponent(out CharacterBody bodyPrefab))
                {
#if DEBUG
                    Log.Debug($"Excluding master {masterPrefab}: null body");
#endif
                    return false;
                }

                if (!bodyPrefab.TryGetComponent(out ModelLocator modelLocator) || !modelLocator.modelTransform)
                {
#if DEBUG
                    Log.Debug($"Excluding master {masterPrefab}: null model");
#endif
                    return false;
                }

                if (modelLocator.modelTransform.childCount == 0)
                {
#if DEBUG
                    Log.Debug($"Excluding master {masterPrefab}: empty model");
#endif
                    return false;
                }

                switch (masterPrefab.name)
                {
                    case "AffixEarthHealerMaster": // Dies instantly
                    case "AncientWispMaster": // Does nothing
                    case "ArtifactShellMaster": // No model, does not attack, cannot be damaged
                    case "BeetleCrystalMaster": // Weird beetle reskin
                    case "BeetleGuardAllyMaster": // Ally reskin
                    case "BeetleGuardMasterCrystal": // Weird beetle buard reskin
                    case "ClaymanMaster": // No hitboxes
                    case "DroneCommanderMaster": // Doesn't have a gun unless summoner has SDP
                    case "EngiBeamTurretMaster": // Seems to ignore the player
                    case "LemurianBruiserMasterHaunted": // Would include if it had a more distinct appearance
                    case "LemurianBruiserMasterPoison": // Would include if it had a more distinct appearance
                    case "MajorConstructMaster": // Beta Xi Construct
                    case "MinorConstructAttachableMaster": // Instantly dies
                    case "MinorConstructOnKillMaster": // Alpha construct reskin
                    case "NullifierAllyMaster": // Ally reskin
                    case "ParentPodMaster": // Just a worse Parent spawn
                    case "ShopkeeperMaster": // Too much health, also flashbang thing when it takes enough damage
                    case "TitanGoldAllyMaster": // Ally reskin
                    case "UrchinTurretMaster": // Dies shortly after spawning
                    case "VoidBarnacleMaster": // The NoCast version will be included so its spawn position can be controller
                    case "VoidBarnacleAllyMaster": // Ally reskin
                    case "VoidJailerAllyMaster": // Ally reskin
                    case "VoidMegaCrabAllyMaster": // Ally reskin
                    case "VoidRaidCrabJointMaster": // Just some balls, does nothing
                    case "VoidRaidCrabMaster": // Beta voidling, half invisible
#if DEBUG
                        Log.Debug($"excluding master {masterPrefab.name}: blacklist");
#endif
                        return false;
                }

#if DEBUG
                Log.Debug($"Including master {masterPrefab}");
#endif

                return true;
            });
        }

        protected static Vector3 getProperSpawnPosition(Vector3 startSpawnPosition, CharacterMaster masterPrefab, Xoroshiro128Plus rng)
        {
            DirectorPlacementRule placementRule = new DirectorPlacementRule
            {
                position = startSpawnPosition,
                placementMode = DirectorPlacementRule.PlacementMode.NearestNode,
                preventOverhead = true
            };

            CharacterBody bodyPrefab = masterPrefab.bodyPrefab.GetComponent<CharacterBody>();

            bool isFlying;
            if (masterPrefab.masterIndex == MasterCatalog.FindMasterIndex("EngiTurretMaster") ||
                masterPrefab.masterIndex == MasterCatalog.FindMasterIndex("SquidTurretMaster") ||
                masterPrefab.masterIndex == MasterCatalog.FindMasterIndex("MinorConstructMaster") ||
                masterPrefab.masterIndex == MasterCatalog.FindMasterIndex("Turret1Master") ||
                masterPrefab.masterIndex == MasterCatalog.FindMasterIndex("VoidBarnacleNoCastMaster"))
            {
                isFlying = false;
            }
            else if (masterPrefab.masterIndex == MasterCatalog.FindMasterIndex("FlyingVerminMaster"))
            {
                isFlying = true;
            }
            else
            {
                isFlying = !bodyPrefab.GetComponent<CharacterMotor>();
            }

            MapNodeGroup.GraphType nodeGraphType = isFlying ? MapNodeGroup.GraphType.Air : MapNodeGroup.GraphType.Ground;

            return placementRule.EvaluateToPosition(new Xoroshiro128Plus(rng.nextUlong), bodyPrefab.hullClassification, nodeGraphType);
        }

        protected virtual void onSpawned(CharacterMaster master)
        {
            if (master.masterIndex == MasterCatalog.FindMasterIndex("EquipmentDroneMaster"))
            {
                Inventory inventory = master.inventory;
                if (inventory)
                {
                    EquipmentIndex equipmentIndex = RNG.NextElementUniform(EquipmentCatalog.equipmentList.Where(Run.instance.IsEquipmentAvailable).ToList());

#if DEBUG
                    Log.Debug($"Gave {Language.GetString(EquipmentCatalog.GetEquipmentDef(equipmentIndex).nameToken, "en")} to spawned equipment drone");
#endif

                    inventory.SetEquipmentIndex(equipmentIndex);
                }
            }

            if (master.bodyPrefab && master.bodyPrefab.TryGetComponent(out CharacterBody body))
            {
                BodyIndex bodyIndex = body.bodyIndex;

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
}
