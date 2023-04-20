using BepInEx.Configuration;
using RiskOfChaos.EffectHandling;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Data;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.Utilities;
using RiskOfChaos.Utilities.Extensions;
using RiskOfOptions.OptionConfigs;
using RiskOfOptions.Options;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectDefinitions.Character
{
    [ChaosEffect("monster_inventory_give_random_item", EffectRepetitionWeightCalculationMode = EffectActivationCountMode.PerRun, EffectWeightReductionPercentagePerActivation = 15f)]
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
        }

        [SystemInitializer]
        static void InitHooks()
        {
            On.RoR2.BasicPickupDropTable.GenerateWeightedSelection += (orig, self, run) =>
            {
                orig(self, run);

                if (!_dropTable || self != _dropTable)
                    return;

                self.AddPickupIfMissing(PickupCatalog.FindPickupIndex(RoR2Content.Items.ArtifactKey.itemIndex), _bossWeight.Value);
                self.AddPickupIfMissing(PickupCatalog.FindPickupIndex(RoR2Content.Items.CaptainDefenseMatrix.itemIndex), _tier3Weight.Value);
                self.AddPickupIfMissing(PickupCatalog.FindPickupIndex(RoR2Content.Items.Pearl.itemIndex), _bossWeight.Value);
                self.AddPickupIfMissing(PickupCatalog.FindPickupIndex(RoR2Content.Items.ShinyPearl.itemIndex), _bossWeight.Value);
                self.AddPickupIfMissing(PickupCatalog.FindPickupIndex(RoR2Content.Items.TonicAffliction.itemIndex), _lunarItemWeight.Value);

                if (run.IsExpansionEnabled(ExpansionUtils.DLC1))
                {
                }
            };
        }

        [SystemInitializer(typeof(ChaosEffectCatalog))]
        static void InitConfig()
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
        static TeamFilter _monsterTeamFilter;

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

                _monsterTeamFilter = _monsterInventory.GetComponent<TeamFilter>();
                _monsterTeamFilter.teamIndex = TeamIndex.Monster;

                NetworkServer.Spawn(_monsterInventory.gameObject);
            };

            CharacterMaster.onStartGlobal += tryGiveItems;
        }

        static bool canGiveItems(CharacterMaster master)
        {
            if (!_monsterInventory || !master || !master.inventory)
                return false;

            switch (master.teamIndex)
            {
                case TeamIndex.None:
                case TeamIndex.Neutral:
                case TeamIndex.Player:
                    return false;
            }

            return true;
        }

        static void tryGiveItems(CharacterMaster master)
        {
            if (!canGiveItems(master))
                return;

            master.inventory.AddItemsFrom(_monsterInventory);
            master.inventory.CopyEquipmentFrom(_monsterInventory);
        }

        [EffectCanActivate]
        static bool CanActivate()
        {
            return _dropTable && _monsterInventory;
        }

        public override void OnStart()
        {
            PickupDef pickupDef = PickupCatalog.GetPickupDef(_dropTable.GenerateDrop(RNG));
            _monsterInventory.TryGrant(pickupDef, true);

            uint pickupCount;
            if (pickupDef.itemIndex != ItemIndex.None)
            {
                pickupCount = (uint)_monsterInventory.GetItemCount(pickupDef.itemIndex);
            }
            else
            {
                pickupCount = 1;
            }

            Chat.SendBroadcastChat(new Chat.PlayerPickupChatMessage
            {
                baseToken = "MONSTER_INVENTORY_ADD_ITEM",
                pickupToken = pickupDef.nameToken,
                pickupColor = pickupDef.baseColor,
                pickupQuantity = pickupCount
            });

            foreach (CharacterMaster master in CharacterMaster.readOnlyInstancesList)
            {
                if (canGiveItems(master))
                {
                    PickupUtils.GrantOrDropPickupAt(pickupDef, master);
                }
            }
        }
    }
}
