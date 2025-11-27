using RiskOfChaos.Collections;
using RiskOfChaos.Collections.CatalogIndex;
using RiskOfChaos.ConfigHandling;
using RiskOfChaos.ConfigHandling.AcceptableValues;
using RiskOfChaos.Content;
using RiskOfChaos.EffectHandling;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Data;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.Patches;
using RiskOfChaos.Utilities;
using RiskOfChaos.Utilities.Extensions;
using RiskOfOptions.OptionConfigs;
using RoR2;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectDefinitions.Character
{
    [ChaosEffect("revive_dead_characters")]
    [IncompatibleEffects(typeof(DisableRevives))]
    public sealed class ReviveDeadCharacters : MonoBehaviour
    {
        [EffectConfig]
        static readonly ConfigHolder<int> _maxTrackedCharactersCount =
            ConfigFactory<int>.CreateConfig("Max Characters to Revive", 30)
                              .Description("The maximum amount of characters the effect can revive at once")
                              .AcceptableValues(new AcceptableValueMin<int>(1))
                              .OptionConfig(new IntFieldConfig { Min = 1 })
                              .OnValueChanged((s, e) =>
                              {
                                  _trackedDeadCharacters.MaxCapacity = e.NewValue;
                              })
                              .Build();

        static readonly MasterIndexCollection _reviveBlacklist = new MasterIndexCollection([
            "BrotherHauntMaster",
            "ArtifactShellMaster",
        ]);

        static readonly MaxCapacityQueue<DeadCharacterInfo> _trackedDeadCharacters = new MaxCapacityQueue<DeadCharacterInfo>(_maxTrackedCharactersCount.Value)
        {
            DisposeOnDequeue = true
        };

        static readonly List<DeadCharacterInfo> _trackedDeadPlayers = [];

        static EffectIndex _reviveEffectIndex = EffectIndex.Invalid;

        [SystemInitializer(typeof(EffectCatalogUtils))]
        static void Init()
        {
            GlobalEventManager.onCharacterDeathGlobal += onCharacterDeathGlobal;

            Run.onRunDestroyGlobal += onRunDestroyGlobal;

            Stage.onServerStageComplete += onServerStageComplete;

            _reviveEffectIndex = EffectCatalogUtils.FindEffectIndex("HippoRezEffect");
            if (_reviveEffectIndex == EffectIndex.Invalid)
            {
                Log.Error($"Failed to find revive effect index");
            }
        }

        static void onCharacterDeathGlobal(DamageReport damageReport)
        {
            if (!NetworkServer.active)
                return;

            CharacterMaster victimMaster = damageReport.victimMaster;
            if (!victimMaster)
                return;

            if (_reviveBlacklist.Contains(victimMaster.masterIndex))
                return;

            CharacterBody victimBody = damageReport.victimBody;
            bool hadBody = victimBody;

            IEnumerator waitForDeathThenTryTrack()
            {
                yield return new WaitForFixedUpdate();

                if (!victimMaster || victimMaster.IsExtraLifePendingServer())
                {
                    Log.Debug($"Not tracking death: {Util.GetBestMasterName(victimMaster)} is invalid or has extra life pending");
                    yield break;
                }

                if (hadBody)
                {
                    // victim body has been replaced, instant respawn
                    CharacterBody currentBody = victimMaster.GetBody();
                    if (currentBody && victimBody != currentBody)
                    {
                        Log.Debug($"Not tracking death: {Util.GetBestMasterName(victimMaster)} has new body, likely respawned");
                        yield break;
                    }
                }

                if (victimMaster.playerCharacterMasterController)
                {
                    _trackedDeadPlayers.Add(new DeadCharacterInfo(damageReport));
                }
                else
                {
                    _trackedDeadCharacters.Enqueue(new DeadCharacterInfo(damageReport));
                }
            }

            MonoBehaviour coroutineHost = Stage.instance;
            if (!coroutineHost)
            {
                coroutineHost = Run.instance;

                if (!coroutineHost)
                    coroutineHost = RoR2Application.instance;
            }

            coroutineHost.StartCoroutine(waitForDeathThenTryTrack());
        }

        static void onRunDestroyGlobal(Run run)
        {
            clearTrackedDeaths();
        }

        static void onServerStageComplete(Stage stage)
        {
            clearTrackedDeaths();
        }

        static void clearTrackedDeaths()
        {
            _trackedDeadCharacters.Clear();
            _trackedDeadPlayers.Clear();
        }

        [EffectCanActivate]
        static bool CanActivate(in EffectCanActivateContext context)
        {
            return !context.IsNow || _trackedDeadCharacters.Count > 0 || _trackedDeadPlayers.Count > 0;
        }

        void Start()
        {
            if (NetworkServer.active)
            {
                _trackedDeadPlayers.TryDo(doRespawn);
                _trackedDeadPlayers.Clear();

                _trackedDeadCharacters.TryDo(doRespawn);
                _trackedDeadCharacters.Clear();
            }
        }

        static void doRespawn(DeadCharacterInfo deadCharacterInfo)
        {
            if (deadCharacterInfo != null)
            {
                deadCharacterInfo.Respawn();
                deadCharacterInfo.Dispose();
            }
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

        sealed class DeadCharacterInfo : MasterSummon.IInventorySetupCallback, IDisposable
        {
            readonly CharacterMaster _master;

            readonly Vector3 _bodyPosition;
            readonly Vector3 _bodyForward;

            readonly BodyIndex _bodyIndex;
            readonly TeamIndex _teamIndex;

            readonly Loadout _loadout;

            readonly int[] _permanentItemStacks;
            readonly float[] _tempItemStackValues;

            readonly EquipmentIndex[][] _equipments;

            readonly CombatSquad _combatSquad;

            readonly DeathRewardsData _deathRewardsData;

            readonly DamageReport _deathReport;

            public DeadCharacterInfo(DamageReport deathReport)
            {
                _master = deathReport.victimMaster;

                _deathReport = deathReport;

                _bodyIndex = deathReport.victimBodyIndex;
                _teamIndex = deathReport.victimTeamIndex;

                Vector3? position = null;
                Vector3 forward = Vector3.forward;

                CharacterBody victimBody = deathReport.victimBody;
                CharacterMaster victimMaster = deathReport.victimMaster;

                if (victimMaster)
                {
                    if (victimMaster.lostBodyToDeath)
                    {
                        position = victimMaster.deathFootPosition;
                    }
                }

                if (victimBody)
                {
                    position = victimBody.footPosition;

                    forward = victimBody.transform.forward;
                    if (victimBody.characterDirection)
                    {
                        forward = victimBody.characterDirection.forward;
                    }

                    if (victimBody.TryGetComponent(out DeathRewards deathRewards))
                    {
                        _deathRewardsData = new DeathRewardsData(deathRewards);
                    }
                }

                _bodyPosition = position ?? SpawnUtils.GetBestValidRandomPlacementRule().EvaluateToPosition(RoR2Application.rng);
                _bodyForward = forward;

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

                Inventory inventory = victimMaster ? victimMaster.inventory : null;
                if (inventory)
                {
                    _permanentItemStacks = ItemCatalog.RequestItemStackArray();
                    inventory.WriteAllPermanentItemStacks(_permanentItemStacks);

                    _tempItemStackValues = new float[ItemCatalog.itemCount];
                    inventory.WriteAllTempItemRawValues(_tempItemStackValues);

                    int equipmentSlotCount = inventory.GetEquipmentSlotCount();

                    _equipments = new EquipmentIndex[equipmentSlotCount][];
                    for (uint slot = 0; slot < equipmentSlotCount; slot++)
                    {
                        int equipmentSetCount = inventory.GetEquipmentSetCount(slot);

                        _equipments[slot] = new EquipmentIndex[equipmentSetCount];

                        for (uint set = 0; set < equipmentSetCount; set++)
                        {
                            _equipments[slot][set] = inventory.GetEquipment(slot, set).equipmentIndex;
                        }
                    }
                }
            }

            public void Respawn()
            {
                bool respawned = false;

                Quaternion bodyRotation = Util.QuaternionSafeLookRotation(_bodyForward);

                CharacterMaster master = _master;
                if (master)
                {
                    // If body still exists, we've likely missed a respawn, ignore
                    CharacterBody body = master.GetBody();
                    if ((!body || !body.healthComponent || !body.healthComponent.alive) && !master.IsExtraLifePendingServer())
                    {
                        PreventMetamorphosisRespawn.PreventionEnabled = true;
                        try
                        {
                            master.Respawn(_bodyPosition, bodyRotation);
                        }
                        finally
                        {
                            PreventMetamorphosisRespawn.PreventionEnabled = false;
                        }

                        respawned = true;
                    }
                }
                else
                {
                    MasterCatalog.MasterIndex masterIndex = MasterCatalog.FindAiMasterIndexForBody(_bodyIndex);
                    if (masterIndex.isValid)
                    {
                        master = new MasterSummon()
                        {
                            masterPrefab = MasterCatalog.GetMasterPrefab(masterIndex),
                            position = _bodyPosition,
                            rotation = bodyRotation,
                            ignoreTeamMemberLimit = true,
                            teamIndexOverride = _teamIndex,
                            loadout = _loadout,
                            inventorySetupCallback = this,
                            preSpawnSetupCallback = preSpawnSetupCallback
                        }.Perform();

                        respawned = true;
                    }
                    else
                    {
                        Log.Warning($"No master index found for {BodyCatalog.GetBodyName(_bodyIndex)}");
                    }
                }

                if (!respawned)
                    return;

                CharacterBody newBody = master.GetBody();

                if (_reviveEffectIndex != EffectIndex.Invalid)
                {
                    Vector3 reviveEffectPosition = _bodyPosition;
                    if (newBody)
                    {
                        reviveEffectPosition = newBody.footPosition;
                    }

                    EffectManager.SpawnEffect(_reviveEffectIndex, new EffectData
                    {
                        origin = reviveEffectPosition,
                        rotation = bodyRotation
                    }, true);
                }

                if (newBody)
                {
                    foreach (EntityStateMachine entityStateMachine in newBody.GetComponents<EntityStateMachine>())
                    {
                        entityStateMachine.initialStateType = entityStateMachine.mainStateType;
                    }

                    if (newBody.TryGetComponent(out DeathRewards deathRewards))
                    {
                        _deathRewardsData.ApplyRewards(deathRewards);
                    }
                }
            }

            void MasterSummon.IInventorySetupCallback.SetupSummonedInventory(MasterSummon masterSummon, Inventory summonedInventory)
            {
                using (new Inventory.InventoryChangeScope(summonedInventory))
                {
                    if (_permanentItemStacks != null)
                    {
                        static bool itemCopyFilter(ItemIndex i)
                        {
                            return true;
                        }

                        summonedInventory.AddItemsFrom(_permanentItemStacks, itemCopyFilter);
                    }

                    if (_tempItemStackValues != null)
                    {
                        for (ItemIndex itemIndex = 0; (int)itemIndex < _tempItemStackValues.Length; itemIndex++)
                        {
                            summonedInventory.GiveItemTemp(itemIndex, _tempItemStackValues[(int)itemIndex]);
                        }
                    }

                    if (_equipments != null)
                    {
                        for (uint slot = 0; slot < _equipments.Length; slot++)
                        {
                            for (uint set = 0; set < _equipments[slot].Length; set++)
                            {
                                summonedInventory.SetEquipmentIndexForSlot(_equipments[slot][set], slot, set);
                            }
                        }
                    }
                }
            }

            void preSpawnSetupCallback(CharacterMaster master)
            {
                if (_combatSquad && _combatSquad.isActiveAndEnabled && !_combatSquad.defeatedServer)
                {
                    _combatSquad.AddMember(master);
                }
                else if (_deathReport.victimIsBoss)
                {
                    GameObject bossCombatSquadObj = Instantiate(RoCContent.NetworkedPrefabs.BossCombatSquadNoReward);

                    CombatSquad bossCombatSquad = bossCombatSquadObj.GetComponent<CombatSquad>();
                    bossCombatSquad.AddMember(master);

                    NetworkServer.Spawn(bossCombatSquadObj);
                }
            }

            public override string ToString()
            {
                if (_master)
                {
                    return Util.GetBestMasterName(_master);
                }
                else
                {
                    return BodyCatalog.GetBodyName(_bodyIndex);
                }
            }

            public void Dispose()
            {
                if (_loadout != null)
                {
                    Loadout.ReturnInstance(_loadout);
                }

                if (_permanentItemStacks != null)
                {
                    ItemCatalog.ReturnItemStackArray(_permanentItemStacks);
                }
            }
        }
    }
}
