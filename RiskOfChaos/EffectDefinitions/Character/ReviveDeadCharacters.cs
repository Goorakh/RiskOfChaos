using RiskOfChaos.ConfigHandling;
using RiskOfChaos.EffectHandling;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Data;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.Utilities;
using RiskOfChaos.Utilities.Extensions;
using RiskOfOptions.OptionConfigs;
using RoR2;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectDefinitions.Character
{
    [ChaosEffect("revive_dead_characters")]
    public sealed class ReviveDeadCharacters : BaseEffect
    {
        static readonly GameObject _bossCombatSquadPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Core/BossCombatSquad.prefab").WaitForCompletion();

        [EffectConfig]
        static readonly ConfigHolder<int> _maxTrackedCharactersCount = 
            ConfigFactory<int>.CreateConfig("Max Characters to Revive", 50)
                              .Description("The maximum amount of characters the effect can revive at once")
                              .OptionConfig(new IntSliderConfig
                              {
                                  min = 1,
                                  max = 100
                              })
                              .ValueConstrictor(ValueConstrictors.GreaterThanOrEqualTo(1))
                              .OnValueChanged((s, e) =>
                              {
                                  _trackedDeadCharacters.MaxCapacity = e.NewValue;
                              })
                              .Build();

        static readonly MaxCapacityQueue<DeadCharacterInfo> _trackedDeadCharacters = new MaxCapacityQueue<DeadCharacterInfo>(_maxTrackedCharactersCount.Value);

        [SystemInitializer]
        static void InitListeners()
        {
            GlobalEventManager.onCharacterDeathGlobal += damageReport =>
            {
                if (!NetworkServer.active)
                    return;

                CharacterMaster victimMaster = damageReport.victimMaster;
                if (!victimMaster || victimMaster.IsExtraLifePendingServer())
                    return;

                _trackedDeadCharacters.Enqueue(new DeadCharacterInfo(damageReport));
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
        static bool CanActivate(EffectCanActivateContext context)
        {
            return !context.IsNow || _trackedDeadCharacters.Count > 0;
        }

        public override void OnStart()
        {
            _trackedDeadCharacters.TryDo(character => character.Respawn());
            _trackedDeadCharacters.Clear();
        }

        readonly record struct DeathRewardsData(uint GoldReward, uint ExpReward, int SpawnValue)
        {
            public DeathRewardsData(DeathRewards deathRewards) : this(deathRewards.goldReward, deathRewards.expReward, deathRewards.spawnValue)
            {
            }

            public readonly void ApplyRewards(DeathRewards deathRewards)
            {
                deathRewards.goldReward = GoldReward;
                deathRewards.expReward = ExpReward;
                deathRewards.spawnValue = SpawnValue;
            }
        }

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

            readonly DeathRewardsData _deathRewardsData;

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

                    if (victimBody.TryGetComponent(out DeathRewards deathRewards))
                    {
                        _deathRewardsData = new DeathRewardsData(deathRewards);
                    }
                }
                else
                {
                    _bodyPosition = SpawnUtils.GetBestValidRandomPlacementRule().EvaluateToPosition(RoR2Application.rng);
                    _bodyRotation = Quaternion.identity;
                }

                CharacterMaster victimMaster = deathReport.victimMaster;
                if (victimMaster)
                {
                    if (victimMaster.loadout != null)
                    {
                        _loadout = Loadout.RequestInstance();
                        victimMaster.loadout.Copy(_loadout);
                    }

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
                    Log.Warning($"No master index found for {BodyCatalog.GetBodyName(_bodyIndex)}");
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

                    if (bodyObj.TryGetComponent(out DeathRewards deathRewards))
                    {
                        _deathRewardsData.ApplyRewards(deathRewards);
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
                if (_combatSquad && _combatSquad.isActiveAndEnabled && !_combatSquad.defeatedServer)
                {
                    _combatSquad.AddMember(master);
                }
                else if (_deathReport.victimIsBoss && _bossCombatSquadPrefab)
                {
                    GameObject bossCombatSquadObj = GameObject.Instantiate(_bossCombatSquadPrefab);

                    BossGroup bossGroup = bossCombatSquadObj.GetComponent<BossGroup>();
                    bossGroup.dropPosition = null; // Don't drop an item

                    CombatSquad bossCombatSquad = bossCombatSquadObj.GetComponent<CombatSquad>();
                    bossCombatSquad.AddMember(master);

                    NetworkServer.Spawn(bossCombatSquadObj);
                }
            }

            public override readonly string ToString()
            {
                return $"{BodyCatalog.GetBodyName(_bodyIndex)}";
            }
        }
    }
}
