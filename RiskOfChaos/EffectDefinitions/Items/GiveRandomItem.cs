using BepInEx.Configuration;
using RiskOfChaos.EffectHandling;
using RiskOfChaos.Utilities;
using RiskOfOptions.OptionConfigs;
using RiskOfOptions.Options;
using RoR2;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace RiskOfChaos.EffectDefinitions.Items
{
    [ChaosEffect(EFFECT_ID, EffectRepetitionWeightExponent = 0f)]
    public class GiveRandomItem : BaseEffect
    {
        const string EFFECT_ID = "GiveRandomItem";

        static string _configSectionName;

        static BasicPickupDropTable _dropTable;

        static ConfigEntry<float> _tier1Weight;
        static ConfigEntry<float> _tier2Weight;
        static ConfigEntry<float> _tier3Weight;
        static ConfigEntry<float> _bossWeight;
        static ConfigEntry<float> _lunarEquipmentWeight;
        static ConfigEntry<float> _lunarItemWeight;
        static ConfigEntry<float> _equipmentWeight;
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

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static void addCustomPickupToDropTable(PickupIndex pickup, float weight)
            {
#pragma warning disable Publicizer001 // Accessing a member that was not originally public
                _dropTable.selector.AddChoice(pickup, weight);
#pragma warning restore Publicizer001 // Accessing a member that was not originally public
            }

            addCustomPickupToDropTable(PickupCatalog.FindPickupIndex(RoR2Content.Items.CaptainDefenseMatrix.itemIndex), _tier3Weight.Value);

            addCustomPickupToDropTable(PickupCatalog.FindPickupIndex(DLC1Content.Equipment.BossHunterConsumed.equipmentIndex), _equipmentWeight.Value);
            addCustomPickupToDropTable(PickupCatalog.FindPickupIndex(RoR2Content.Equipment.QuestVolatileBattery.equipmentIndex), _equipmentWeight.Value);
            addCustomPickupToDropTable(PickupCatalog.FindPickupIndex(DLC1Content.Equipment.LunarPortalOnUse.equipmentIndex), _equipmentWeight.Value);
        }

        [SystemInitializer(typeof(ChaosEffectCatalog))]
        static void Init()
        {
            _configSectionName = ChaosEffectCatalog.GetConfigSectionName(EFFECT_ID);

            static ConfigEntry<float> addWeightConfig(string name, float defaultValue)
            {
                ConfigEntry<float> config = Main.Instance.Config.Bind(new ConfigDefinition(_configSectionName, $"Weight: {name}"), defaultValue, new ConfigDescription($"Controls how likely {name} are to be given\n\nA value of 0 means items from this tier will never be given"));
                ChaosEffectCatalog.AddEffectConfigOption(new StepSliderOption(config, new StepSliderConfig { formatString = "{0:F2}", min = 0f, max = 1f, increment = 0.05f }));

                config.SettingChanged += static (sender, e) =>
                {
                    regenerateDropTable();
                };

                return config;
            }

            _tier1Weight = addWeightConfig("Common Items", 0.75f);
            _tier2Weight = addWeightConfig("Uncommon Items", 0.6f);
            _tier3Weight = addWeightConfig("Legendary Items", 0.3f);
            _bossWeight = addWeightConfig("Boss Items", 0.5f);
            _lunarEquipmentWeight = addWeightConfig("Lunar Equipments", 0.15f);
            _lunarItemWeight = addWeightConfig("Lunar Items", 0.35f);
            _equipmentWeight = addWeightConfig("Equipments", 0.25f);
            _voidTier1Weight = addWeightConfig("Common Void Items", 0.4f);
            _voidTier2Weight = addWeightConfig("Uncommon Void Items", 0.35f);
            _voidTier3Weight = addWeightConfig("Legendary Void Items", 0.3f);
            _voidBossWeight = addWeightConfig("Void Boss Items", 0.3f);
        }

        [SystemInitializer(typeof(ItemCatalog), typeof(EquipmentCatalog))]
        static void InitDropTable()
        {
            regenerateDropTable();
        }

        public override void OnStart()
        {
            const string LOG_PREFIX = $"{nameof(GiveRandomItem)}.{nameof(OnStart)} ";

            PickupDef pickupDef = PickupCatalog.GetPickupDef(_dropTable.GenerateDrop(RNG));
            foreach (CharacterMaster playerMaster in PlayerUtils.GetAllPlayerMasters(false))
            {
                PickupUtils.GrantOrDropPickupAt(pickupDef, playerMaster);
            }
        }
    }
}
