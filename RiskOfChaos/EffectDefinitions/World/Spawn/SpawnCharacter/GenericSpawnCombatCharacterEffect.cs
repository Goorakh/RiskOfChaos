using RiskOfChaos.Utilities;
using RiskOfChaos.Utilities.CatalogIndexCollection;
using RoR2;
using RoR2.Navigation;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RiskOfChaos.EffectDefinitions.World.Spawn.SpawnCharacter
{
    public abstract class GenericSpawnCombatCharacterEffect : GenericSpawnEffect<CharacterMaster>
    {
        protected virtual float eliteChance => 0f;
        protected virtual bool allowDirectorUnavailableElites => false;

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

        protected record struct CharacterSpawnData(EquipmentIndex OverrideEquipment, int[] ItemStacks, Loadout Loadout) : IDisposable
        {
            public CharacterSpawnData() : this(EquipmentIndex.None, ItemCatalog.RequestItemStackArray(), null)
            {
            }

            public readonly void GiveItem(ItemDef itemDef, int count = 1)
            {
                if (!itemDef)
                    throw new ArgumentNullException(nameof(itemDef));

                GiveItem(itemDef.itemIndex, count);
            }

            public readonly void GiveItem(ItemIndex itemIndex, int count = 1)
            {
                if (ItemStacks == null)
                {
                    Log.Warning($"{nameof(ItemStacks)} not initialized");
                    return;
                }

                if (itemIndex <= ItemIndex.None || (int)itemIndex >= ItemStacks.Length)
                {
                    Log.Warning($"Invalid ItemIndex {itemIndex}");
                    return;
                }

                ItemStacks[(int)itemIndex] += count;
            }

            public readonly void ApplyTo(CharacterMaster master)
            {
                Inventory inventory = master.inventory;

                if (inventory)
                {
                    if (OverrideEquipment != EquipmentIndex.None)
                    {
                        inventory.SetEquipmentIndex(OverrideEquipment);
                    }

                    if (ItemStacks != null)
                    {
                        inventory.AddItemsFrom(ItemStacks, i => true);
                    }
                }

                if (Loadout != null)
                {
                    Loadout newLoadout = new Loadout();
                    Loadout.Copy(newLoadout);
                    master.SetLoadoutServer(newLoadout);
                }
            }

            public void Dispose()
            {
                if (ItemStacks != null)
                {
                    ItemCatalog.ReturnItemStackArray(ItemStacks);
                    ItemStacks = null;
                }
            }
        }

        CharacterSpawnData _spawnData = new CharacterSpawnData();

        ~GenericSpawnCombatCharacterEffect()
        {
            _spawnData.Dispose();
        }

        static readonly MasterIndexCollection _nonAllySkinMasters = new MasterIndexCollection(new string[]
        {
            "BeetleGuardMaster",
            "NullifierMaster",
            "TitanGoldMaster",
            "VoidJailerMaster",
            "VoidMegaCrabMaster",
        });

        static readonly MasterIndexCollection _allySkinMasters = new MasterIndexCollection(new string[]
        {
            "BeetleGuardAllyMaster",
            "NullifierAllyMaster",
            "TitanGoldAllyMaster",
            "VoidJailerAllyMaster",
            "VoidMegaCrabAllyMaster",
        });

        static readonly MasterIndexCollection _masterBlacklist = new MasterIndexCollection(new string[]
        {
            "AffixEarthHealerMaster", // Dies instantly
            "AncientWispMaster", // Does nothing
            "ArtifactShellMaster", // No model, does not attack, cannot be damaged
            "BeetleCrystalMaster", // Weird beetle reskin
            "BeetleGuardMasterCrystal", // Weird beetle buard reskin
            "ClaymanMaster", // No hitboxes
            "EngiBeamTurretMaster", // Seems to ignore the player
            "LemurianBruiserMasterHaunted", // Would include if it had a more distinct appearance
            "LemurianBruiserMasterPoison", // Would include if it had a more distinct appearance
            "MajorConstructMaster", // Beta Xi Construct
            "MinorConstructAttachableMaster", // Instantly dies
            "MinorConstructOnKillMaster", // Alpha construct reskin
            "ParentPodMaster", // Just a worse Parent spawn
            "ShopkeeperMaster", // Too much health, also flashbang thing when it takes enough damage
            "UrchinTurretMaster", // Dies shortly after spawning
            "VoidBarnacleMaster", // The NoCast version will be included so its spawn position can be controlled
            "VoidBarnacleAllyMaster",
            "VoidRaidCrabJointMaster", // Just some balls, does nothing
            "VoidRaidCrabMaster", // Beta voidling, half invisible
            "WispSoulMaster", // Just dies on a timer
        });

        protected static IEnumerable<CharacterMaster> getAllValidMasterPrefabs(bool useAllySkins)
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

                if (useAllySkins)
                {
                    if (_nonAllySkinMasters.Contains(masterPrefab.masterIndex))
                    {
#if DEBUG
                        Log.Debug($"excluding master {masterPrefab.name}: non-ally skin");
#endif

                        return false;
                    }
                }
                else
                {
                    if (_allySkinMasters.Contains(masterPrefab.masterIndex))
                    {
#if DEBUG
                        Log.Debug($"excluding master {masterPrefab.name}: ally skin");
#endif

                        return false;
                    }
                }

                if (_masterBlacklist.Contains(masterPrefab.masterIndex))
                {
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

        static readonly MasterIndexCollection _overrideGroundNodeSpawnMasters = new MasterIndexCollection(new string[]
        {
            "EngiTurretMaster",
            "GrandparentMaster",
            "SquidTurretMaster",
            "MinorConstructMaster",
            "Turret1Master",
            "VoidBarnacleNoCastMaster"
        });

        static readonly MasterIndexCollection _overrideAirNodeSpawnMasters = new MasterIndexCollection("FlyingVerminMaster");

        protected static Vector3 getProperSpawnPosition(Vector3 startSpawnPosition, CharacterMaster masterPrefab, Xoroshiro128Plus rng)
        {
            DirectorPlacementRule placementRule = new DirectorPlacementRule
            {
                position = startSpawnPosition,
                placementMode = DirectorPlacementRule.PlacementMode.NearestNode,
                preventOverhead = true
            };

            CharacterBody bodyPrefab = masterPrefab.bodyPrefab.GetComponent<CharacterBody>();

            MapNodeGroup.GraphType nodeGraphType;
            if (_overrideGroundNodeSpawnMasters.Contains(masterPrefab.masterIndex))
            {
                nodeGraphType = MapNodeGroup.GraphType.Ground;
            }
            else if (_overrideAirNodeSpawnMasters.Contains(masterPrefab.masterIndex))
            {
                nodeGraphType = MapNodeGroup.GraphType.Air;
            }
            else
            {
                nodeGraphType = bodyPrefab.GetComponent<CharacterMotor>() ? MapNodeGroup.GraphType.Ground : MapNodeGroup.GraphType.Air;
            }

            return placementRule.EvaluateToPosition(new Xoroshiro128Plus(rng.nextUlong), bodyPrefab.hullClassification, nodeGraphType, NodeFlags.None, NodeFlags.NoCharacterSpawn);
        }

        protected void setupPrefab(CharacterMaster masterPrefab)
        {
            if (masterPrefab.masterIndex == MasterCatalog.FindMasterIndex("EquipmentDroneMaster"))
            {
                List<EquipmentIndex> availableEquipment = EquipmentCatalog.equipmentList.Where(Run.instance.IsEquipmentAvailable).ToList();
                if (availableEquipment.Count > 0)
                {
                    EquipmentIndex equipmentIndex = RoR2Application.rng.NextElementUniform(availableEquipment);

#if DEBUG
                    Log.Debug($"Gave {Language.GetString(EquipmentCatalog.GetEquipmentDef(equipmentIndex).nameToken, "en")} to spawned equipment drone");
#endif

                    _spawnData.OverrideEquipment = equipmentIndex;
                }
                else
                {
                    Log.Warning("No available equipment to give to spawned equipment drone");
                }
            }
            else if (masterPrefab.masterIndex == MasterCatalog.FindMasterIndex("DroneCommanderMaster"))
            {
                _spawnData.GiveItem(DLC1Content.Items.DroneWeaponsBoost);

                if (UnityEngine.Random.value < 0.1f)
                {
                    _spawnData.GiveItem(DLC1Content.Items.DroneWeaponsDisplay2);
                }
                else
                {
                    _spawnData.GiveItem(DLC1Content.Items.DroneWeaponsDisplay1);
                }
            }

            _spawnData.Loadout = LoadoutUtils.GetRandomLoadoutFor(masterPrefab, new Xoroshiro128Plus(RNG.nextUlong));

            if (RNG.nextNormalizedFloat <= eliteChance)
            {
                _spawnData.OverrideEquipment = EliteUtils.SelectEliteEquipment(new Xoroshiro128Plus(RNG.nextUlong), allowDirectorUnavailableElites);
            }

            modifySpawnData(ref _spawnData);
        }

        protected virtual void modifySpawnData(ref CharacterSpawnData spawnData)
        {
        }

        protected virtual void onSpawned(CharacterMaster master)
        {
            _spawnData.ApplyTo(master);
        }
    }
}
