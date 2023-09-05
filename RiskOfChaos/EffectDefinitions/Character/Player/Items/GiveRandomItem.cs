using RiskOfChaos.ConfigHandling;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Data;
using RiskOfChaos.Utilities;
using RiskOfChaos.Utilities.Comparers;
using RiskOfChaos.Utilities.Extensions;
using RiskOfChaos.Utilities.ParsedValueHolders.ParsedList;
using RiskOfOptions.OptionConfigs;
using RoR2;
using UnityEngine;

namespace RiskOfChaos.EffectDefinitions.Character.Player.Items
{
    [ChaosEffect("give_random_item", EffectWeightReductionPercentagePerActivation = 0f)]
    public sealed class GiveRandomItem : BaseEffect
    {
        static bool _dropTableDirty = false;
        static BasicPickupDropTable _dropTable;

        [EffectConfig]
        static readonly ConfigHolder<string> _itemBlacklistConfig =
            ConfigFactory<string>.CreateConfig("Item Blacklist", string.Empty)
                                 .Description("A comma-separated list of items and equipment that should not be included for the effect. Both internal and English display names are accepted, with spaces and commas removed.")
                                 .OptionConfig(new InputFieldConfig
                                 {
                                     submitOn = InputFieldConfig.SubmitEnum.OnSubmit
                                 })
                                 .OnValueChanged(() => _dropTableDirty = true)
                                 .Build();

        static readonly ParsedPickupList _itemBlacklist = new ParsedPickupList(PickupIndexComparer.Instance)
        {
            ConfigHolder = _itemBlacklistConfig
        };

        static ConfigHolder<float> createItemTierWeightConfig(string name, float defaultWeight)
        {
            return ConfigFactory<float>.CreateConfig($"Weight: {name}", defaultWeight)
                                       .Description($"Controls how likely {name} are to be given\n\nA value of 0 means items from this tier will never be given")
                                       .OptionConfig(new StepSliderConfig
                                       {
                                           formatString = "{0:F2}",
                                           min = 0f,
                                           max = 1f,
                                           increment = 0.05f
                                       })
                                       .ValueConstrictor(CommonValueConstrictors.GreaterThanOrEqualTo(0f))
                                       .OnValueChanged(() => _dropTableDirty = true)
                                       .Build();
        }

        [EffectConfig] static readonly ConfigHolder<float> _tier1Weight = createItemTierWeightConfig("Common Items", 0.75f);
        [EffectConfig] static readonly ConfigHolder<float> _tier2Weight = createItemTierWeightConfig("Uncommon Items", 0.6f);
        [EffectConfig] static readonly ConfigHolder<float> _tier3Weight = createItemTierWeightConfig("Legendary Items", 0.3f);
        [EffectConfig] static readonly ConfigHolder<float> _bossWeight = createItemTierWeightConfig("Boss Items", 0.5f);
        [EffectConfig] static readonly ConfigHolder<float> _lunarEquipmentWeight = createItemTierWeightConfig("Lunar Equipments", 0.15f);
        [EffectConfig] static readonly ConfigHolder<float> _lunarItemWeight = createItemTierWeightConfig("Lunar Items", 0.35f);
        [EffectConfig] static readonly ConfigHolder<float> _equipmentWeight = createItemTierWeightConfig("Equipments", 0.25f);
        [EffectConfig] static readonly ConfigHolder<float> _voidTier1Weight = createItemTierWeightConfig("Common Void Items", 0.4f);
        [EffectConfig] static readonly ConfigHolder<float> _voidTier2Weight = createItemTierWeightConfig("Uncommon Void Items", 0.35f);
        [EffectConfig] static readonly ConfigHolder<float> _voidTier3Weight = createItemTierWeightConfig("Legendary Void Items", 0.3f);
        [EffectConfig] static readonly ConfigHolder<float> _voidBossWeight = createItemTierWeightConfig("Void Boss Items", 0.3f);

        static void regenerateDropTable()
        {
#if DEBUG
            Log.Debug("regenerating drop table...");
#endif

            if (!_dropTable)
            {
                _dropTable = ScriptableObject.CreateInstance<BasicPickupDropTable>();
                _dropTable.name = $"dt{nameof(GiveRandomItem)}";
            }

            _dropTable.tier1Weight = _tier1Weight.Value;
            _dropTable.tier2Weight = _tier2Weight.Value;
            _dropTable.tier3Weight = _tier3Weight.Value;
            _dropTable.bossWeight = _bossWeight.Value;
            _dropTable.lunarEquipmentWeight = _lunarEquipmentWeight.Value;
            _dropTable.lunarItemWeight = _lunarItemWeight.Value;
            _dropTable.lunarCombinedWeight = 0f;
            _dropTable.equipmentWeight = _equipmentWeight.Value;
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

#pragma warning disable Publicizer001 // Accessing a member that was not originally public
                WeightedSelection<PickupIndex> selector = self.selector;
#pragma warning restore Publicizer001 // Accessing a member that was not originally public

                for (int i = selector.Count - 1; i >= 0; i--)
                {
                    PickupIndex pickupIndex = selector.GetChoice(i).value;
                    if (_itemBlacklist.Contains(pickupIndex))
                    {
#if DEBUG
                        Log.Debug($"Removing {pickupIndex} from droptable: Blacklist");
#endif
                        selector.RemoveChoice(i);
                    }
                }

                void tryAddPickup(PickupIndex pickup, float weight)
                {
                    if (!_itemBlacklist.Contains(pickup))
                    {
                        self.AddPickupIfMissing(pickup, weight);
                    }
                    else
                    {
#if DEBUG
                        Log.Debug($"Not adding {pickup} to droptable: Blacklist");
#endif
                    }
                }

                tryAddPickup(PickupCatalog.FindPickupIndex(RoR2Content.Items.ArtifactKey.itemIndex), _bossWeight.Value);
                tryAddPickup(PickupCatalog.FindPickupIndex(RoR2Content.Items.CaptainDefenseMatrix.itemIndex), _tier3Weight.Value);
                tryAddPickup(PickupCatalog.FindPickupIndex(RoR2Content.Items.Pearl.itemIndex), _bossWeight.Value);
                tryAddPickup(PickupCatalog.FindPickupIndex(RoR2Content.Items.ShinyPearl.itemIndex), _bossWeight.Value);
                tryAddPickup(PickupCatalog.FindPickupIndex(RoR2Content.Items.TonicAffliction.itemIndex), _lunarItemWeight.Value);

                tryAddPickup(PickupCatalog.FindPickupIndex(RoR2Content.Equipment.QuestVolatileBattery.equipmentIndex), _equipmentWeight.Value);

                if (run.IsExpansionEnabled(ExpansionUtils.DLC1))
                {
                    tryAddPickup(PickupCatalog.FindPickupIndex(DLC1Content.Equipment.BossHunterConsumed.equipmentIndex), _equipmentWeight.Value);
                    tryAddPickup(PickupCatalog.FindPickupIndex(DLC1Content.Equipment.LunarPortalOnUse.equipmentIndex), _equipmentWeight.Value);
                }
            };
        }

        public override void OnStart()
        {
            if (!_dropTable || _dropTableDirty)
            {
                regenerateDropTable();
            }

            PickupDef pickupDef = PickupCatalog.GetPickupDef(_dropTable.GenerateDrop(RNG));

            PlayerUtils.GetAllPlayerMasters(false).TryDo(playerMaster =>
            {
                PickupUtils.GrantOrDropPickupAt(pickupDef, playerMaster);
            }, Util.GetBestMasterName);
        }
    }
}
