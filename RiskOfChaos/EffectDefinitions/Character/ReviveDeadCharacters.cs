using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.Utilities.Extensions;
using RoR2;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectDefinitions.Character
{
    [ChaosEffect("revive_dead_characters")]
    public sealed class ReviveDeadCharacters : BaseEffect
    {
        static readonly GameObject _bossCombatSquadPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Core/BossCombatSquad.prefab").WaitForCompletion();

        readonly struct DeadCharacterInfo : MasterSummon.IInventorySetupCallback
        {
            readonly Vector3 _bodyPosition;
            readonly Quaternion _bodyRotation;

            readonly BodyIndex _bodyIndex;
            readonly TeamIndex _teamIndex;

            readonly Loadout _loadout;

            readonly int[] _itemStacks;
            readonly EquipmentIndex[] _equipmentSlots;

            readonly CombatSquad _combatSquad;

            readonly DamageReport _deathReport;

            public DeadCharacterInfo(DamageReport deathReport)
            {
                _deathReport = deathReport;

                _bodyIndex = deathReport.victimBodyIndex;
                _teamIndex = deathReport.victimTeamIndex;

                CharacterBody victimBody = deathReport.victimBody;
                if (victimBody)
                {
                    _bodyPosition = victimBody.footPosition;
                    _bodyRotation = victimBody.GetRotation();
                }

                CharacterMaster victimMaster = deathReport.victimMaster;
                if (victimMaster)
                {
                    _loadout = Loadout.RequestInstance();
                    victimMaster.loadout.Copy(_loadout);

                    foreach (CombatSquad squad in InstanceTracker.GetInstancesList<CombatSquad>())
                    {
                        if (squad.ContainsMember(victimMaster))
                        {
                            _combatSquad = squad;
                            break;
                        }
                    }
                }

                Inventory inventory = victimMaster.inventory;
                if (inventory)
                {
                    _itemStacks = ItemCatalog.RequestItemStackArray();
                    inventory.WriteItemStacks(_itemStacks);

                    int equipmentSlotCount = inventory.GetEquipmentSlotCount();
                    _equipmentSlots = new EquipmentIndex[equipmentSlotCount];
                    for (uint i = 0; i < equipmentSlotCount; i++)
                    {
                        _equipmentSlots[i] = inventory.GetEquipment(i).equipmentIndex;
                    }
                }
            }

            public readonly void Respawn()
            {
                MasterCatalog.MasterIndex masterIndex = MasterCatalog.FindAiMasterIndexForBody(_bodyIndex);
                if (!masterIndex.isValid)
                {
                    Log.Warning($"No master index found for body {BodyCatalog.GetBodyName(_bodyIndex)}");
                    return;
                }

                CharacterMaster master = new MasterSummon()
                {
                    masterPrefab = MasterCatalog.GetMasterPrefab(masterIndex),
                    position = _bodyPosition,
                    rotation = _bodyRotation,
                    ignoreTeamMemberLimit = true,
                    teamIndexOverride = _teamIndex,
                    loadout = _loadout,
                    inventorySetupCallback = this,
                    preSpawnSetupCallback = preSpawnSetupCallback
                }.Perform();

                if (_loadout != null)
                {
                    Loadout.ReturnInstance(_loadout);
                }

                GameObject reviveEffect = LegacyResourcesAPI.Load<GameObject>("Prefabs/Effects/HippoRezEffect");
                if (reviveEffect)
                {
                    EffectManager.SpawnEffect(reviveEffect, new EffectData
                    {
                        origin = _bodyPosition,
                        rotation = _bodyRotation
                    }, true);
                }

                GameObject bodyObj = master.GetBodyObject();
                if (bodyObj)
                {
                    foreach (EntityStateMachine entityStateMachine in bodyObj.GetComponents<EntityStateMachine>())
                    {
                        entityStateMachine.initialStateType = entityStateMachine.mainStateType;
                    }
                }
            }

            public readonly void SetupSummonedInventory(MasterSummon masterSummon, Inventory summonedInventory)
            {
                if (_itemStacks != null)
                {
                    summonedInventory.AddItemsFrom(_itemStacks, Inventory.defaultItemCopyFilterDelegate);
                    ItemCatalog.ReturnItemStackArray(_itemStacks);
                }

                if (_equipmentSlots != null)
                {
                    for (uint i = 0; i < _equipmentSlots.Length; i++)
                    {
                        summonedInventory.SetEquipmentIndexForSlot(_equipmentSlots[i], i);
                    }
                }
            }

            readonly void preSpawnSetupCallback(CharacterMaster master)
            {
                if (_combatSquad && _combatSquad.isActiveAndEnabled)
                {
                    _combatSquad.AddMember(master);
                }
                else if (_deathReport.victimIsBoss && _bossCombatSquadPrefab)
                {
                    GameObject bossCombatSquadObj = GameObject.Instantiate(_bossCombatSquadPrefab);

                    BossGroup bossGroup = bossCombatSquadObj.GetComponent<BossGroup>();
                    bossGroup.dropPosition = null; // Don't drop an squad

                    CombatSquad bossCombatSquad = bossCombatSquadObj.GetComponent<CombatSquad>();
                    bossCombatSquad.AddMember(master);

                    NetworkServer.Spawn(bossCombatSquadObj);
                }
            }
        }

        const int MAX_TRACKED_CHARACTER_COUNT = 50;
        static readonly Queue<DeadCharacterInfo> _trackedDeadCharacters = new Queue<DeadCharacterInfo>(MAX_TRACKED_CHARACTER_COUNT);

        [SystemInitializer]
        static void Init()
        {
            GlobalEventManager.onCharacterDeathGlobal += damageReport =>
            {
                if (!NetworkServer.active)
                    return;

                CharacterMaster victimMaster = damageReport.victimMaster;
                if (!victimMaster || !victimMaster.IsDeadAndOutOfLivesServer())
                    return;

                _trackedDeadCharacters.Enqueue(new DeadCharacterInfo(damageReport));
                while (_trackedDeadCharacters.Count > MAX_TRACKED_CHARACTER_COUNT)
                {
                    _trackedDeadCharacters.Dequeue();
                }
            };

            Run.onRunDestroyGlobal += _ =>
            {
                _trackedDeadCharacters.Clear();
            };

            Stage.onServerStageComplete += _ =>
            {
                _trackedDeadCharacters.Clear();
            };
        }

        [EffectCanActivate]
        static bool CanActivate()
        {
            return _trackedDeadCharacters.Count > 0;
        }

        public override void OnStart()
        {
            while (_trackedDeadCharacters.Count > 0)
            {
                DeadCharacterInfo characterInfo = _trackedDeadCharacters.Dequeue();
                characterInfo.Respawn();
            }
        }
    }
}
