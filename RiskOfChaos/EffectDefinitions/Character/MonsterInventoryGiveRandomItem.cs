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
using UnityEngine.Networking;

namespace RiskOfChaos.EffectDefinitions.Character
{
    [ChaosEffect("monster_inventory_give_random_item", EffectRepetitionWeightCalculationMode = EffectActivationCountMode.PerRun, EffectWeightReductionPercentagePerActivation = 15f)]
    public sealed class MonsterInventoryGiveRandomItem : BaseEffect
    {
        static bool _dropTableDirty = false;
        static BasicPickupDropTable _dropTable;

        static ConfigHolder<float> createWeightConfig(string name, float defaultValue)
        {
            return ConfigFactory<float>.CreateConfig($"Weight: {name}", defaultValue)
                                       .Description($"Controls how likely {name} are to be given\n\nA value of 0 means items from this tier will never be given")
                                       .OptionConfig(new StepSliderConfig
                                       {
                                           formatString = "{0:F2}",
                                           min = 0f,
                                           max = 2f,
                                           increment = 0.05f
                                       })
                                       .ValueConstrictor(CommonValueConstrictors.GreaterThanOrEqualTo(0f))
                                       .OnValueChanged(() => _dropTableDirty = true)
                                       .Build();
        }

        [EffectConfig] static readonly ConfigHolder<float> _tier1Weight = createWeightConfig("Common Items", 1f);
        [EffectConfig] static readonly ConfigHolder<float> _tier2Weight = createWeightConfig("Uncommon Items", 0.75f);
        [EffectConfig] static readonly ConfigHolder<float> _tier3Weight = createWeightConfig("Legendary Items", 0.3f);
        [EffectConfig] static readonly ConfigHolder<float> _bossWeight = createWeightConfig("Boss Items", 0.4f);
        [EffectConfig] static readonly ConfigHolder<float> _lunarItemWeight = createWeightConfig("Lunar Items", 0.25f);
        [EffectConfig] static readonly ConfigHolder<float> _voidTier1Weight = createWeightConfig("Common Void Items", 0.2f);
        [EffectConfig] static readonly ConfigHolder<float> _voidTier2Weight = createWeightConfig("Uncommon Void Items", 0.15f);
        [EffectConfig] static readonly ConfigHolder<float> _voidTier3Weight = createWeightConfig("Legendary Void Items", 0.1f);
        [EffectConfig] static readonly ConfigHolder<float> _voidBossWeight = createWeightConfig("Void Boss Items", 0.1f);

        static void regenerateDropTable()
        {
#if DEBUG
            Log.Debug("regenerating drop table...");
#endif

            if (!_dropTable)
            {
                _dropTable = ScriptableObject.CreateInstance<BasicPickupDropTable>();
                _dropTable.name = $"dt{nameof(MonsterInventoryGiveRandomItem)}";
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

            _dropTableDirty = false;
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
            return _monsterInventory;
        }

        public override void OnStart()
        {
            if (!_dropTable || _dropTableDirty)
            {
                regenerateDropTable();
            }

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

            CharacterMaster.readOnlyInstancesList.TryDo(master =>
            {
                if (canGiveItems(master))
                {
                    master.inventory.TryGrant(pickupDef, true);
                }
            }, Util.GetBestMasterName);
        }
    }
}
