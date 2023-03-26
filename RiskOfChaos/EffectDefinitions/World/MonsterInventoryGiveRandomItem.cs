using BepInEx.Configuration;
using R2API;
using RiskOfChaos.EffectHandling;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Data;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.Utilities;
using RiskOfChaos.Utilities.Extensions;
using RiskOfOptions.OptionConfigs;
using RiskOfOptions.Options;
using RoR2;
using RoR2.UI;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectDefinitions.World
{
    [ChaosEffect("monster_inventory_give_random_item", EffectRepetitionWeightCalculationMode = EffectActivationCountMode.PerRun, EffectWeightReductionPercentagePerActivation = 15f, IsNetworked = true)]
    public sealed class MonsterInventoryGiveRandomItem : BaseEffect
    {
        [InitEffectInfo]
        static readonly ChaosEffectInfo _effectInfo;

        static BasicPickupDropTable _dropTable;

        static ConfigEntry<float> _tier1Weight;
        static ConfigEntry<float> _tier2Weight;
        static ConfigEntry<float> _tier3Weight;
        static ConfigEntry<float> _bossWeight;
        static ConfigEntry<float> _lunarItemWeight;
        static ConfigEntry<float> _voidTier1Weight;
        static ConfigEntry<float> _voidTier2Weight;
        static ConfigEntry<float> _voidTier3Weight;
        static ConfigEntry<float> _voidBossWeight;

        static void regenerateDropTable()
        {
#if DEBUG
            Log.Debug("regenerating drop table...");
#endif

            if (!_dropTable)
            {
                _dropTable = ScriptableObject.CreateInstance<BasicPickupDropTable>();
            }

            _dropTable.tier1Weight = _tier1Weight.Value;
            _dropTable.tier2Weight = _tier2Weight.Value;
            _dropTable.tier3Weight = _tier3Weight.Value;
            _dropTable.bossWeight = _bossWeight.Value;
            _dropTable.lunarEquipmentWeight = 0f;
            _dropTable.lunarItemWeight = _lunarItemWeight.Value;
            _dropTable.lunarCombinedWeight = 0f;
            _dropTable.equipmentWeight = 0f;
            _dropTable.voidTier1Weight = _voidTier1Weight.Value;
            _dropTable.voidTier2Weight = _voidTier2Weight.Value;
            _dropTable.voidTier3Weight = _voidTier3Weight.Value;
            _dropTable.voidBossWeight = _voidBossWeight.Value;

            // If this is done mid-run, Regenerate has to be called, since it's only done by the game on run start
            Run run = Run.instance;
            if (run)
            {
#pragma warning disable Publicizer001 // Accessing a member that was not originally public
                _dropTable.Regenerate(run);
#pragma warning restore Publicizer001 // Accessing a member that was not originally public
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static void addCustomPickupToDropTable(PickupIndex pickup, float weight)
            {
#pragma warning disable Publicizer001 // Accessing a member that was not originally public
                _dropTable.selector.AddChoice(pickup, weight);
#pragma warning restore Publicizer001 // Accessing a member that was not originally public
            }

            addCustomPickupToDropTable(PickupCatalog.FindPickupIndex(RoR2Content.Items.CaptainDefenseMatrix.itemIndex), _tier3Weight.Value);
        }

        [SystemInitializer(typeof(ChaosEffectCatalog))]
        static void Init()
        {
            ConfigEntry<float> addWeightConfig(string name, float defaultValue)
            {
                ConfigEntry<float> config = Main.Instance.Config.Bind(new ConfigDefinition(_effectInfo.ConfigSectionName, $"Weight: {name}"), defaultValue, new ConfigDescription($"Controls how likely {name} are to be given\n\nA value of 0 means items from this tier will never be given"));
                addConfigOption(new StepSliderOption(config, new StepSliderConfig
                {
                    formatString = "{0:F2}",
                    min = 0f,
                    max = 2f,
                    increment = 0.05f
                }));

                config.SettingChanged += static (sender, e) =>
                {
                    regenerateDropTable();
                };

                return config;
            }

            _tier1Weight = addWeightConfig("Common Items", 1f);
            _tier2Weight = addWeightConfig("Uncommon Items", 0.75f);
            _tier3Weight = addWeightConfig("Legendary Items", 0.3f);
            _bossWeight = addWeightConfig("Boss Items", 0.4f);
            _lunarItemWeight = addWeightConfig("Lunar Items", 0.25f);
            _voidTier1Weight = addWeightConfig("Common Void Items", 0.2f);
            _voidTier2Weight = addWeightConfig("Uncommon Void Items", 0.15f);
            _voidTier3Weight = addWeightConfig("Legendary Void Items", 0.1f);
            _voidBossWeight = addWeightConfig("Void Boss Items", 0.1f);
        }

        [SystemInitializer(typeof(ItemCatalog))]
        static void InitDropTable()
        {
            regenerateDropTable();
        }

        static Inventory _monsterInventory;

        [SystemInitializer]
        static void InitInventoryListeners()
        {
            Run.onRunStartGlobal += _ =>
            {
                if (!NetworkServer.active)
                    return;

                if (!NetPrefabs.GenericTeamInventoryPrefab)
                {
                    Log.Warning($"{nameof(NetPrefabs.GenericTeamInventoryPrefab)} is null");
                    return;
                }

                _monsterInventory = GameObject.Instantiate(NetPrefabs.GenericTeamInventoryPrefab).GetComponent<Inventory>();
                _monsterInventory.GetComponent<TeamFilter>().teamIndex = TeamIndex.Monster;

                NetworkServer.Spawn(_monsterInventory.gameObject);
            };

            Run.onRunDestroyGlobal += _ =>
            {
                if (NetworkServer.active)
                {
                    if (_monsterInventory)
                    {
                        NetworkServer.Destroy(_monsterInventory.gameObject);
                    }
                    else
                    {
                        Log.Warning("Ending run as server, but monster inventory doesn't exist, was it not spawned?");
                    }
                }

                _monsterInventory = null;
            };

            SpawnCard.onSpawnedServerGlobal += result =>
            {
                if (!NetworkServer.active || !_monsterInventory)
                    return;

                if (!result.success || !result.spawnedInstance)
                    return;

                CharacterMaster spawnedMaster = result.spawnedInstance.GetComponent<CharacterMaster>();
                if (!spawnedMaster)
                    return;

                if (spawnedMaster.teamIndex != TeamIndex.Monster)
                    return;

                spawnedMaster.inventory.AddItemsFrom(_monsterInventory);
                spawnedMaster.inventory.CopyEquipmentFrom(_monsterInventory);
            };
        }

        [EffectCanActivate]
        static bool CanActivate()
        {
            return _dropTable && _monsterInventory;
        }

        PickupDef _pickupDef;

        public override void OnPreStartServer()
        {
            base.OnPreStartServer();

            _pickupDef = PickupCatalog.GetPickupDef(_dropTable.GenerateDrop(RNG));
            _monsterInventory.TryGrant(_pickupDef, true);
        }

        public override void Serialize(NetworkWriter writer)
        {
            base.Serialize(writer);
            writer.Write(_pickupDef.pickupIndex);
        }

        public override void Deserialize(NetworkReader reader)
        {
            base.Deserialize(reader);
            _pickupDef = PickupCatalog.GetPickupDef(reader.ReadPickupIndex());
        }

        public override void OnStart()
        {
            CharacterMaster localPlayerMaster = PlayerUtils.GetLocalUserMaster();
            if (localPlayerMaster)
            {
                CharacterMasterNotificationQueue.PushPickupNotification(localPlayerMaster, _pickupDef.pickupIndex);
            }
        }
    }
}
