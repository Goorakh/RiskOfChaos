using RiskOfChaos.Collections.ParsedValue;
using RiskOfChaos.ConfigHandling;
using RiskOfChaos.ConfigHandling.AcceptableValues;
using RiskOfChaos.Utilities.Comparers;
using RiskOfChaos.Utilities.Extensions;
using RiskOfOptions.OptionConfigs;
using RoR2;
using RoR2.ExpansionManagement;
using System;
using System.Collections.Generic;

namespace RiskOfChaos.Utilities.DropTables
{
    public class ConfigurableDropTable : BasicPickupDropTable, IConfigProvider
    {
        readonly Dictionary<DropType, ConfigHolder<float>> _dropTypeToWeightConfig = [];

        bool _dropTableDirty = false;

        ConfigHolder<string> _itemBlacklistConfig;
        ParsedPickupList _itemBlacklist;

        public event Action<List<ExplicitDrop>> AddDrops;
        public event Action<List<PickupIndex>> RemoveDrops;

        public event Action OnPreGenerate;

        public void RegisterDrop(DropType type, float defaultWeight)
        {
            if (_dropTypeToWeightConfig.ContainsKey(type))
            {
                Log.Error($"Drop type {type} already registered");
                return;
            }

            string name = type switch
            {
                DropType.Tier1 => "Common Items",
                DropType.Tier2 => "Uncommon Items",
                DropType.Tier3 => "Legendary Items",
                DropType.Boss => "Boss Items",
                DropType.LunarEquipment => "Lunar Equipments",
                DropType.LunarItem => "Lunar Items",
                DropType.Equipment => "Equipments",
                DropType.VoidTier1 => "Common Void Items",
                DropType.VoidTier2 => "Uncommon Void Items",
                DropType.VoidTier3 => "Legendary Void Items",
                DropType.VoidBoss => "Void Boss Items",
                _ => throw new NotImplementedException($"Drop type {type} is not implemented")
            };

            ConfigHolder<float> config =
                ConfigFactory<float>.CreateConfig($"Weight: {name}", defaultWeight)
                                    .Description($"""
                                     Controls how likely {name} are to be given
                                     
                                     A value of 0 means items from this tier will never be given
                                     """)
                                    .AcceptableValues(new AcceptableValueMin<float>(0f))
                                    .OptionConfig(new FloatFieldConfig { Min = 0f })
                                    .OnValueChanged(MarkDirty)
                                    .Build();

            _dropTypeToWeightConfig.Add(type, config);
        }

        public void CreateItemBlacklistConfig(string key, string description)
        {
            _itemBlacklistConfig =
                ConfigFactory<string>.CreateConfig(key, string.Empty)
                                     .Description(description)
                                     .OptionConfig(new InputFieldConfig
                                     {
                                         lineType = TMPro.TMP_InputField.LineType.SingleLine,
                                         submitOn = InputFieldConfig.SubmitEnum.OnExitOrSubmit
                                     })
                                     .OnValueChanged(MarkDirty)
                                     .Build();

            _itemBlacklist = new ParsedPickupList(PickupIndexComparer.Instance)
            {
                ConfigHolder = _itemBlacklistConfig
            };
        }

        public float GetWeight(DropType type)
        {
            if (_dropTypeToWeightConfig.TryGetValue(type, out ConfigHolder<float> config))
            {
                return config.Value;
            }
            else
            {
                return 0f;
            }
        }

        public IEnumerable<ConfigHolderBase> GetConfigs()
        {
            if (_itemBlacklistConfig != null)
            {
                yield return _itemBlacklistConfig;
            }

            foreach (ConfigHolder<float> weightConfig in _dropTypeToWeightConfig.Values)
            {
                yield return weightConfig;
            }
        }

        public void MarkDirty()
        {
            _dropTableDirty = true;
        }

        public void RegenerateIfNeeded()
        {
            if (_dropTableDirty)
            {
                Run run = Run.instance;
                if (run)
                {
                    Log.Debug($"Regenerating drop table '{name}'");

                    Regenerate(run);
                    _dropTableDirty = false;
                }
            }
        }

        public override void Regenerate(Run run)
        {
            tier1Weight = GetWeight(DropType.Tier1);
            tier2Weight = GetWeight(DropType.Tier2);
            tier3Weight = GetWeight(DropType.Tier3);
            bossWeight = GetWeight(DropType.Boss);
            lunarEquipmentWeight = GetWeight(DropType.LunarEquipment);
            lunarItemWeight = GetWeight(DropType.LunarItem);
            lunarCombinedWeight = 0f;
            equipmentWeight = GetWeight(DropType.Equipment);
            voidTier1Weight = GetWeight(DropType.VoidTier1);
            voidTier2Weight = GetWeight(DropType.VoidTier2);
            voidTier3Weight = GetWeight(DropType.VoidTier3);
            voidBossWeight = GetWeight(DropType.VoidBoss);

            OnPreGenerate?.Invoke();

            base.Regenerate(run);

            List<ExplicitDrop> additionalDrops = [];
            AddDrops?.Invoke(additionalDrops);
            foreach (ExplicitDrop drop in additionalDrops)
            {
                if ((!IsFilterRequired() || PassesFilter(drop.PickupIndex)) &&
                    (drop.RequiredExpansion == ExpansionIndex.None || ExpansionUtils.IsExpansionEnabled(drop.RequiredExpansion)))
                {
                    selector.AddOrModifyWeight(drop.PickupIndex, GetWeight(drop.DropType));
                }
            }

            List<PickupIndex> removePickups = [];
            RemoveDrops?.Invoke(removePickups);

            if (_itemBlacklist != null || removePickups.Count > 0)
            {
                for (int i = selector.Count - 1; i >= 0; i--)
                {
                    PickupIndex pickupIndex = selector.GetChoice(i).value;

                    bool remove = false;
                    if (_itemBlacklist != null && _itemBlacklist.Contains(pickupIndex))
                    {
                        Log.Debug($"Removing {pickupIndex} from droptable {name}: Blacklist");
                        remove = true;
                    }
                    else if (removePickups.Contains(pickupIndex))
                    {
                        Log.Debug($"Removing {pickupIndex} from droptable {name}: In remove list");
                        remove = true;
                    }

                    if (remove)
                    {
                        selector.RemoveChoice(i);
                    }
                }
            }

            if (selector.Count == 0)
            {
                Log.Warning($"Drop table {name} has no options, adding fallback");
                selector.AddChoice(PickupCatalog.FindPickupIndex(RoR2Content.Items.ArtifactKey.itemIndex), 1f);
            }
        }
    }
}
