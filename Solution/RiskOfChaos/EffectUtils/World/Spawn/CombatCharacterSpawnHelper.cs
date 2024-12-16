using RiskOfChaos.Collections.CatalogIndex;
using RiskOfChaos.Utilities;
using RiskOfChaos.Utilities.Extensions;
using RoR2;
using RoR2.Navigation;
using System.Collections.Generic;
using System.Linq;
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
            List<CharacterMaster> aiMasters = MasterCatalog.allAiMasters.ToList();

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

        static readonly MasterIndexCollection _overrideGroundNodeSpawnMasters = new MasterIndexCollection([
            "EngiTurretMaster",
            "GrandparentMaster",
            "SquidTurretMaster",
            "MinorConstructMaster",
            "Turret1Master",
            "VoidBarnacleNoCastMaster",
            "VoidBarnacleAllyMaster",
            "VoidBarnacleMaster"
        ]);

        static readonly MasterIndexCollection _overrideAirNodeSpawnMasters = new MasterIndexCollection([
            "FlyingVerminMaster"
        ]);

        public static MapNodeGroup.GraphType GetSpawnGraphType(CharacterMaster masterPrefab)
        {
            if (_overrideGroundNodeSpawnMasters.Contains(masterPrefab.masterIndex))
            {
                return MapNodeGroup.GraphType.Ground;
            }

            if (_overrideAirNodeSpawnMasters.Contains(masterPrefab.masterIndex))
            {
                return MapNodeGroup.GraphType.Air;
            }

            return masterPrefab.bodyPrefab.GetComponent<CharacterMotor>() ? MapNodeGroup.GraphType.Ground : MapNodeGroup.GraphType.Air;
        }

        public static void SetupSpawnedCombatCharacter(CharacterMaster master, Xoroshiro128Plus rng)
        {
            Inventory inventory = master.inventory;

            if (master.masterIndex == MasterCatalog.FindMasterIndex("EquipmentDroneMaster"))
            {
                List<EquipmentIndex> availableEquipment = new List<EquipmentIndex>(EquipmentCatalog.equipmentCount);
                foreach (EquipmentIndex equipment in EquipmentCatalog.equipmentList)
                {
                    if (Run.instance.IsEquipmentEnabled(equipment))
                    {
                        availableEquipment.Add(equipment);
                    }
                }

                if (availableEquipment.Count > 0)
                {
                    EquipmentIndex equipmentIndex = rng.NextElementUniform(availableEquipment);

                    Log.Debug($"Gave {FormatUtils.GetBestEquipmentDisplayName(equipmentIndex)} to spawned equipment drone");

                    if (inventory)
                    {
                        inventory.SetEquipmentIndex(equipmentIndex);
                    }
                }
                else
                {
                    Log.Warning("No available equipment to give to spawned equipment drone");
                }
            }
            else if (master.masterIndex == MasterCatalog.FindMasterIndex("DroneCommanderMaster"))
            {
                if (inventory)
                {
                    inventory.GiveItem(DLC1Content.Items.DroneWeaponsBoost);

                    if (UnityEngine.Random.value < 0.1f)
                    {
                        inventory.GiveItem(DLC1Content.Items.DroneWeaponsDisplay2);
                    }
                    else
                    {
                        inventory.GiveItem(DLC1Content.Items.DroneWeaponsDisplay1);
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

            EliteIndex[] eliteIndices = EliteUtils.GetRunAvailableElites(ignoreEliteTierAvailability);
            if (eliteIndices.Length == 0)
                return;

            EliteIndex eliteIndex = rng.NextElementUniform(eliteIndices);
            EliteDef eliteDef = EliteCatalog.GetEliteDef(eliteIndex);
            if (!eliteDef)
                return;

            EquipmentDef eliteEquipmentDef = eliteDef.eliteEquipmentDef;
            if (!eliteEquipmentDef)
                return;

            inventory.TryGrant(PickupCatalog.FindPickupIndex(eliteEquipmentDef.equipmentIndex), InventoryExtensions.EquipmentReplacementRule.DeleteExisting);

            if (!ignoreEliteStatBoosts)
            {
                float healthBoostCoefficient = eliteDef.healthBoostCoefficient;
                float damageBoostCoefficient = eliteDef.damageBoostCoefficient;

                inventory.GiveItem(RoR2Content.Items.BoostHp, Mathf.RoundToInt((healthBoostCoefficient - 1f) * 10f));
                inventory.GiveItem(RoR2Content.Items.BoostDamage, Mathf.RoundToInt((damageBoostCoefficient - 1f) * 10f));
            }
        }
    }
}
