using HG;
using RiskOfChaos.Collections.CatalogIndex;
using RiskOfChaos.Utilities;
using RiskOfChaos.Utilities.Extensions;
using RiskOfChaos.Utilities.Pickup;
using RoR2;
using RoR2.Navigation;
using System.Collections.Generic;
using UnityEngine;

namespace RiskOfChaos.EffectUtils.World.Spawn
{
    public static class CombatCharacterSpawnHelper
    {
        static readonly MasterIndexCollection _allySkinMasters = new MasterIndexCollection([
            "BeetleGuardAllyMaster",
            "NullifierAllyMaster",
            "TitanGoldAllyMaster",
            "VoidJailerAllyMaster",
            "VoidMegaCrabAllyMaster",
            "VoidBarnacleAllyMaster"
        ]);

        static readonly MasterIndexCollection _masterBlacklist = new MasterIndexCollection([
            "AffixEarthHealerMaster", // Dies instantly
            "BeetleGuardMasterCrystal", // Weird beetle guard reskin
            "DevotedLemurianMaster", // Just regular Lemurian without Devotion artifact
            "EngiBeamTurretMaster", // Seems to ignore the player
            "LemurianBruiserMasterHaunted", // Would include if it had a more distinct appearance
            "LemurianBruiserMasterPoison", // Would include if it had a more distinct appearance
            "MiniVoidRaidCrabMasterBase", // Base voidling
            "MinorConstructAttachableMaster", // Instantly dies
            "MinorConstructOnKillMaster", // Alpha construct reskin
            "ParentPodMaster", // Just a worse Parent spawn
            "ShopkeeperMaster", // Too much health, also flashbang thing when it takes enough damage
            "UrchinTurretMaster", // Dies shortly after spawning
            "VoidBarnacleNoCastMaster",
            "VoidRaidCrabMaster", // Beta voidling, half invisible
            "WispSoulMaster", // Just dies on a timer

            // This boss is buggy as hell, can just barely exist outside the boss arena
            // It also can't be an ally, since it doesn't care about teams, it just specifically targets players lol
            "FalseSonBossMaster",
            "FalseSonBossLunarShardMaster",
            "FalseSonBossLunarShardBrokenMaster",

            "InvincibleLemurianMaster",
            "InvincibleLemurianBruiserMaster",
        ]);

        public static void GetAllValidCombatCharacters(List<CharacterMaster> dest)
        {
            List<CharacterMaster> aiMasters = [.. MasterCatalog.allAiMasters];

            dest.EnsureCapacity(aiMasters.Count);

            foreach (CharacterMaster masterPrefab in aiMasters)
            {
                if (IsValidCombatCharacter(masterPrefab))
                {
                    dest.Add(masterPrefab);
                }
            }
        }

        public static bool IsValidCombatCharacter(CharacterMaster masterPrefab)
        {
            if (_masterBlacklist.Contains(masterPrefab.masterIndex))
                return false;

            if (_allySkinMasters.Contains(masterPrefab.masterIndex))
                return false;

            if (!masterPrefab.bodyPrefab || !masterPrefab.bodyPrefab.TryGetComponent(out CharacterBody bodyPrefab))
                return false;

            if (string.IsNullOrWhiteSpace(bodyPrefab.baseNameToken) || Language.IsTokenInvalid(bodyPrefab.baseNameToken))
                return false;

            if (!bodyPrefab.TryGetComponent(out ModelLocator modelLocator) || !modelLocator.modelTransform || modelLocator.modelTransform.childCount == 0)
                return false;

            if (modelLocator.modelTransform.GetComponentsInChildren<HurtBox>().Length == 0)
                return false;

            return true;
        }

        static readonly BodyIndexCollection _overrideGroundNodeSpawnBodies = new BodyIndexCollection([]);

        static readonly BodyIndexCollection _overrideAirNodeSpawnBodies = new BodyIndexCollection([
            "FlyingVerminBody"
        ]);

        public static MapNodeGroup.GraphType GetSpawnGraphType(CharacterMaster masterPrefab)
        {
            GameObject bodyPrefab = masterPrefab ? masterPrefab.bodyPrefab : null;
            return GetSpawnGraphType(bodyPrefab ? bodyPrefab.GetComponent<CharacterBody>() : null);
        }

        public static MapNodeGroup.GraphType GetSpawnGraphType(CharacterBody bodyPrefab)
        {
            BodyIndex bodyIndex = BodyCatalog.FindBodyIndex(bodyPrefab);

            if (_overrideGroundNodeSpawnBodies.Contains(bodyIndex))
                return MapNodeGroup.GraphType.Ground;

            if (_overrideAirNodeSpawnBodies.Contains(bodyIndex))
                return MapNodeGroup.GraphType.Air;

            IPhysMotor physMotor = bodyPrefab ? bodyPrefab.GetComponent<IPhysMotor>() : null;

            bool isFlying = false;
            bool isStatic = false;

            switch (physMotor)
            {
                case null:
                    isStatic = true;
                    break;
                case CharacterMotor characterMotor:
                    isFlying = characterMotor.flightParameters.CheckShouldUseFlight() || !characterMotor.gravityParameters.CheckShouldUseGravity();
                    break;
                case RigidbodyMotor rigidbodyMotor:
                    Rigidbody rigidbody = rigidbodyMotor.rigid;
                    isFlying = rigidbody && !rigidbody.useGravity;
                    isStatic = !rigidbody || rigidbody.isKinematic;
                    break;
                case PseudoCharacterMotor:
                    isStatic = true;
                    break;
                default:
                    Log.Warning($"Unhandled PhysMotor type: {physMotor?.GetType()?.FullName} ({physMotor})");
                    isStatic = true;
                    break;
            }

            return isStatic || !isFlying ? MapNodeGroup.GraphType.Ground : MapNodeGroup.GraphType.Air;
        }

        public static void SetupSpawnedCombatCharacter(CharacterMaster master, Xoroshiro128Plus rng, InventoryExtensions.PickupReplacementRule equipmentReplacementRule = InventoryExtensions.PickupReplacementRule.DeleteExisting)
        {
            Inventory inventory = master.inventory;

            if (master.masterIndex == MasterCatalog.FindMasterIndex("EquipmentDroneMaster"))
            {
                using (ListPool<EquipmentIndex>.RentCollection(out List<EquipmentIndex> availableEquipment))
                {
                    availableEquipment.EnsureCapacity(EquipmentCatalog.equipmentCount);

                    foreach (EquipmentIndex equipmentIndex in EquipmentCatalog.equipmentList)
                    {
                        if (Run.instance.IsEquipmentEnabled(equipmentIndex))
                        {
                            availableEquipment.Add(equipmentIndex);
                        }
                    }

                    if (availableEquipment.Count > 0)
                    {
                        EquipmentIndex equipmentIndex = rng.NextElementUniform(availableEquipment);

                        InventoryExtensions.PickupGrantParameters equipmentGrantParameters = new InventoryExtensions.PickupGrantParameters
                        {
                            PickupToGrant = new PickupStack(PickupCatalog.FindPickupIndex(equipmentIndex), new Inventory.ItemStackValues { permanentStacks = 1}),
                            ReplacementRule = equipmentReplacementRule,
                            NotificationFlags = PickupUtils.DefaultNotificationFlags
                        };

                        if (equipmentGrantParameters.AttemptGrant(inventory))
                        {
                            Log.Debug($"Gave {FormatUtils.GetBestEquipmentDisplayName(equipmentIndex)} to spawned equipment drone");
                        }
                    }
                    else
                    {
                        Log.Warning("No available equipment to give to spawned equipment drone");
                    }
                }
            }
            else if (master.masterIndex == MasterCatalog.FindMasterIndex("DroneCommanderMaster"))
            {
                if (inventory && inventory.GetItemCountPermanent(DLC1Content.Items.DroneWeaponsBoost) == 0)
                {
                    inventory.GiveItemPermanent(DLC1Content.Items.DroneWeaponsBoost);

                    if (UnityEngine.Random.value < 0.1f)
                    {
                        inventory.GiveItemPermanent(DLC1Content.Items.DroneWeaponsDisplay2);
                    }
                    else
                    {
                        inventory.GiveItemPermanent(DLC1Content.Items.DroneWeaponsDisplay1);
                    }
                }
            }
            else if (master.masterIndex == MasterCatalog.FindMasterIndex("DroneBomberMaster"))
            {
                if (inventory && inventory.GetItemCountPermanent(DLC3Content.Items.DroneDynamiteDisplay) == 0)
                {
                    inventory.GiveItemPermanent(DLC3Content.Items.DroneDynamiteDisplay);
                }
            }

            CharacterBody body = master.GetBody();
            if (body)
            {
                DroneIndex droneIndex = DroneCatalog.GetDroneIndexFromBodyIndex(body.bodyIndex);
                if (droneIndex != DroneIndex.None)
                {
                    if (ExpansionUtils.DLC3Enabled)
                    {
                        if (inventory)
                        {
                            int droneTier = 0;
                            while (rng.nextNormalizedFloat <= 0.3f)
                            {
                                droneTier++;
                            }

                            if (droneTier > 0)
                            {
                                inventory.GiveItemPermanent(DLC3Content.Items.DroneUpgradeHidden, droneTier);
                            }
                        }
                    }
                }
            }

            Loadout loadout = LoadoutUtils.GetRandomLoadoutFor(master, rng);
            master.SetLoadoutServer(loadout);
        }

        public static void GrantRandomEliteAspect(CharacterMaster master, Xoroshiro128Plus rng, bool ignoreEliteTierAvailability, bool ignoreEliteStatBoosts = false)
        {
            if (!master)
                return;

            Inventory inventory = master.inventory;
            if (!inventory)
                return;

            IReadOnlyList<EliteIndex> elites = EliteUtils.GetRunAvailableElites(ignoreEliteTierAvailability);
            if (elites.Count == 0)
                return;

            EliteIndex eliteIndex = rng.NextElementUniform(elites);
            EliteDef eliteDef = EliteCatalog.GetEliteDef(eliteIndex);
            if (!eliteDef)
                return;

            EquipmentDef eliteEquipmentDef = eliteDef.eliteEquipmentDef;
            if (!eliteEquipmentDef)
                return;

            InventoryExtensions.PickupGrantParameters eliteEquipmentGrantParameters = new InventoryExtensions.PickupGrantParameters
            {
                PickupToGrant = new PickupStack(PickupCatalog.FindPickupIndex(eliteEquipmentDef.equipmentIndex), new Inventory.ItemStackValues { totalStacks = 1 }),
                ReplacementRule = InventoryExtensions.PickupReplacementRule.DeleteExisting,
                NotificationFlags = PickupUtils.DefaultNotificationFlags
            };

            if (eliteEquipmentGrantParameters.AttemptGrant(inventory))
            {
                if (!ignoreEliteStatBoosts)
                {
                    float healthBoostCoefficient = eliteDef.healthBoostCoefficient;
                    float damageBoostCoefficient = eliteDef.damageBoostCoefficient;

                    inventory.GiveItemPermanent(RoR2Content.Items.BoostHp, Mathf.RoundToInt((healthBoostCoefficient - 1f) * 10f));
                    inventory.GiveItemPermanent(RoR2Content.Items.BoostDamage, Mathf.RoundToInt((damageBoostCoefficient - 1f) * 10f));
                }
            }
        }

#if DEBUG
        [ConCommand(commandName = "roc_test_body_spawn_nodegraphs")]
        static void CCTestSpawnNodeGraphs(ConCommandArgs args)
        {
            foreach (CharacterBody body in BodyCatalog.allBodyPrefabBodyBodyComponents)
            {
                MapNodeGroup.GraphType spawnGraphType = GetSpawnGraphType(body);
                Log.Info($"{body.name}: {spawnGraphType}");
            }
        }
#endif
    }
}
