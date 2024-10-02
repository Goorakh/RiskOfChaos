using BepInEx.Configuration;
using RiskOfChaos.Collections.CatalogIndex;
using RiskOfChaos.ConfigHandling;
using RiskOfChaos.Content;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Data;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.Utilities;
using RiskOfChaos.Utilities.Extensions;
using RiskOfOptions.OptionConfigs;
using RoR2;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RiskOfChaos.EffectDefinitions.World.Spawn.SpawnCharacter
{
    [ChaosEffect("spawn_random_ally", DefaultSelectionWeight = 0.9f)]
    public sealed class SpawnRandomAlly : GenericSpawnCombatCharacterEffect, MasterSummon.IInventorySetupCallback
    {
        static CharacterMaster _devotedLemurianPrefab;

        static CharacterSpawnEntry[] _spawnEntries;

        [SystemInitializer(typeof(MasterCatalog))]
        static void Init()
        {
            _spawnEntries = getAllValidMasterPrefabs(true).Select(master =>
            {
                CharacterBody bodyPrefab = master.bodyPrefab.GetComponent<CharacterBody>();

                float weight;
                if (bodyPrefab.isChampion)
                {
                    weight = 0.5f;
                }
                else
                {
                    weight = 1f;
                }

                return new CharacterSpawnEntry(master, weight);
            }).ToArray();

            _devotedLemurianPrefab = MasterCatalog.FindMasterPrefab("DevotedLemurianMaster")?.GetComponent<CharacterMaster>();
        }

        [EffectConfig]
        static readonly ConfigHolder<float> _eliteChance =
            ConfigFactory<float>.CreateConfig("Elite Chance", 0.4f)
                                .Description("The likelyhood for the spawned ally to be an elite")
                                .AcceptableValues(new AcceptableValueRange<float>(0f, 1f))
                                .OptionConfig(new StepSliderConfig
                                {
                                    FormatString = "{0:P0}",
                                    min = 0f,
                                    max = 1f,
                                    increment = 0.01f
                                })
                                .Build();

        [EffectConfig]
        static readonly ConfigHolder<bool> _allowDirectorUnavailableElites =
            ConfigFactory<bool>.CreateConfig("Ignore Elite Selection Rules", true)
                               .Description("If the effect should ignore normal elite selection rules. If enabled, any elite type can be selected, if disabled, only the elite types that can currently be spawned on the stage can be selected")
                               .OptionConfig(new CheckBoxConfig())
                               .Build();

        static readonly MasterIndexCollection _mastersToConvertToDevotedLemurian = new MasterIndexCollection([
            "LemurianBruiserMaster",
            "LemurianBruiserMasterFire",
            "LemurianBruiserMasterHaunted",
            "LemurianBruiserMasterIce",
            "LemurianBruiserMasterPoison",
            "LemurianMaster"
        ]);

        protected override float eliteChance => _eliteChance.Value;

        protected override bool allowDirectorUnavailableElites => _allowDirectorUnavailableElites.Value;

        [EffectCanActivate]
        static bool CanActivate()
        {
            return areAnyAvailable(_spawnEntries);
        }

        bool _isElite;

        public override void OnStart()
        {
            CharacterMaster allySpawnPrefab = getItemToSpawn(_spawnEntries, RNG);

            bool isDevotedLemurian = _devotedLemurianPrefab &&
                                     RunArtifactManager.instance.IsArtifactEnabled(CU8Content.Artifacts.Devotion) &&
                                     _mastersToConvertToDevotedLemurian.Contains(allySpawnPrefab.masterIndex);

            if (isDevotedLemurian)
            {
#if DEBUG
                Log.Debug($"Replacing {allySpawnPrefab} with {_devotedLemurianPrefab}");
#endif

                allySpawnPrefab = _devotedLemurianPrefab;
            }

            setupPrefab(allySpawnPrefab);

            HashSet<DevotionInventoryController> devotionInventories = [];

            foreach (CharacterBody playerBody in PlayerUtils.GetAllPlayerBodies(true))
            {
                DevotionInventoryController devotionInventoryController = null;
                if (isDevotedLemurian && playerBody.TryGetComponent(out Interactor playerInteractor))
                {
                    devotionInventoryController = DevotionInventoryController.GetOrCreateDevotionInventoryController(playerInteractor);
                }

                if (devotionInventoryController)
                {
                    devotionInventories.Add(devotionInventoryController);
                }

                void onSpawnedCallback(CharacterMaster spawnedMaster)
                {
                    if (spawnedMaster.TryGetComponent(out DevotedLemurianController devotedLemurianController))
                    {
                        if (devotionInventoryController)
                        {
                            devotedLemurianController.InitializeDevotedLemurian(ItemIndex.None, devotionInventoryController);
                        }

                        if (_isElite)
                        {
                            // Skip first level so that elite aspect doesn't get overriden
                            devotedLemurianController.DevotedEvolutionLevel = 1;
                        }
                    }

                    onSpawned(spawnedMaster);
                }

                new MasterSummon()
                {
                    summonerBodyObject = playerBody.gameObject,
                    masterPrefab = allySpawnPrefab.gameObject,
                    position = getProperSpawnPosition(playerBody.footPosition, allySpawnPrefab, RNG),
                    rotation = Quaternion.identity,
                    ignoreTeamMemberLimit = true,
                    useAmbientLevel = true,
                    preSpawnSetupCallback = onSpawnedCallback,
                    inventorySetupCallback = this
                }.Perform();
            }

            foreach (DevotionInventoryController devotionInventoryController in devotionInventories)
            {
                devotionInventoryController.UpdateAllMinions();
            }
        }

        protected override void modifySpawnData(ref CharacterSpawnData spawnData)
        {
            base.modifySpawnData(ref spawnData);

            _isElite = EliteUtils.IsEliteEquipment(spawnData.OverrideEquipment);
        }

        protected override void onSpawned(CharacterMaster master)
        {
            base.onSpawned(master);

            master.gameObject.SetDontDestroyOnLoad(true);
        }

        public void SetupSummonedInventory(MasterSummon masterSummon, Inventory summonedInventory)
        {
            summonedInventory.GiveItem(Items.MinAllyRegen);
        }
    }
}
