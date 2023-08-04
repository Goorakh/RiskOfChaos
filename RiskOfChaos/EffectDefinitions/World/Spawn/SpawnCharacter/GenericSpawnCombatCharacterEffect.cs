using RiskOfChaos.Utilities;
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

            public void GiveItem(ItemDef itemDef, int count = 1)
            {
                if (!itemDef)
                    throw new ArgumentNullException(nameof(itemDef));

                GiveItem(itemDef.itemIndex, count);
            }

            public void GiveItem(ItemIndex itemIndex, int count = 1)
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

            public void ApplyTo(CharacterMaster master)
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
                    switch (masterPrefab.name)
                    {
                        case "BeetleGuardMaster":
                        case "NullifierMaster":
                        case "TitanGoldMaster":
                        case "VoidJailerMaster":
                        case "VoidMegaCrabMaster":
#if DEBUG
                            Log.Debug($"excluding master {masterPrefab.name}: non-ally skin");
#endif

                            return false;
                    }
                }
                else
                {
                    switch (masterPrefab.name)
                    {
                        case "BeetleGuardAllyMaster":
                        case "NullifierAllyMaster":
                        case "TitanGoldAllyMaster":
                        case "VoidJailerAllyMaster":
                        case "VoidMegaCrabAllyMaster":
#if DEBUG
                            Log.Debug($"excluding master {masterPrefab.name}: ally skin");
#endif

                            return false;
                    }
                }

                switch (masterPrefab.name)
                {
                    case "AffixEarthHealerMaster": // Dies instantly
                    case "AncientWispMaster": // Does nothing
                    case "ArtifactShellMaster": // No model, does not attack, cannot be damaged
                    case "BeetleCrystalMaster": // Weird beetle reskin
                    case "BeetleGuardMasterCrystal": // Weird beetle buard reskin
                    case "ClaymanMaster": // No hitboxes
                    case "EngiBeamTurretMaster": // Seems to ignore the player
                    case "LemurianBruiserMasterHaunted": // Would include if it had a more distinct appearance
                    case "LemurianBruiserMasterPoison": // Would include if it had a more distinct appearance
                    case "MajorConstructMaster": // Beta Xi Construct
                    case "MinorConstructAttachableMaster": // Instantly dies
                    case "MinorConstructOnKillMaster": // Alpha construct reskin
                    case "ParentPodMaster": // Just a worse Parent spawn
                    case "ShopkeeperMaster": // Too much health, also flashbang thing when it takes enough damage
                    case "UrchinTurretMaster": // Dies shortly after spawning
                    case "VoidBarnacleMaster": // The NoCast version will be included so its spawn position can be controlled
                    case "VoidBarnacleAllyMaster":
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
                masterPrefab.masterIndex == MasterCatalog.FindMasterIndex("GrandparentMaster") ||
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
