using RiskOfChaos.ConfigHandling;
using RiskOfChaos.EffectHandling;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Data;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.Utilities;
using RiskOfChaos.Utilities.Comparers;
using RiskOfChaos.Utilities.Extensions;
using RiskOfChaos.Utilities.ParsedValueHolders.ParsedList;
using RiskOfOptions.OptionConfigs;
using RoR2;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectDefinitions.Character
{
    [ChaosTimedEffect("monster_inventory_give_random_item", TimedEffectType.Permanent, EffectRepetitionWeightCalculationMode = EffectActivationCountMode.PerRun, EffectWeightReductionPercentagePerActivation = 15f, HideFromEffectsListWhenPermanent = true)]
    public sealed class MonsterInventoryGiveRandomItem : TimedEffect
    {
        static bool _dropTableDirty = false;
        static BasicPickupDropTable _dropTable;

        static void markDropTableDirty()
        {
            _dropTableDirty = true;
        }

        [EffectConfig]
        static readonly ConfigHolder<int> _itemCount =
            ConfigFactory<int>.CreateConfig("Item Count", 1)
                              .Description("The amount of items to give per effect activation")
                              .OptionConfig(new IntSliderConfig
                              {
                                  min = 1,
                                  max = 10
                              })
                              .ValueConstrictor(CommonValueConstrictors.GreaterThanOrEqualTo(1))
                              .Build();

        [EffectConfig]
        static readonly ConfigHolder<string> _itemBlacklistConfig =
            ConfigFactory<string>.CreateConfig("Item Blacklist", string.Empty)
                                 .Description("A comma-separated list of items and equipment that should not be included for the effect. Both internal and English display names are accepted, with spaces and commas removed.")
                                 .OptionConfig(new InputFieldConfig
                                 {
                                     submitOn = InputFieldConfig.SubmitEnum.OnSubmit
                                 })
                                 .OnValueChanged(markDropTableDirty)
                                 .Build();

        static readonly ParsedPickupList _itemBlacklist = new ParsedPickupList(PickupIndexComparer.Instance)
        {
            ConfigHolder = _itemBlacklistConfig
        };

        [EffectConfig]
        static readonly ConfigHolder<bool> _applyAIBlacklist =
            ConfigFactory<bool>.CreateConfig("Apply AI Blacklist", true)
                               .Description("If the effect should apply enemy item blacklist rules to the items it gives")
                               .OptionConfig(new CheckBoxConfig())
                               .OnValueChanged(markDropTableDirty)
                               .Build();

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
                                       .OnValueChanged(markDropTableDirty)
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
                _dropTable.canDropBeReplaced = false;
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

            List<ItemTag> bannedItemTags = new List<ItemTag>();

            if (_applyAIBlacklist.Value)
            {
                bannedItemTags.AddRange(new ItemTag[]
                {
                    ItemTag.AIBlacklist,
                    ItemTag.Scrap,
                    ItemTag.CannotCopy,
                    ItemTag.PriorityScrap
                });
            }

            _dropTable.bannedItemTags = bannedItemTags.Distinct().ToArray();

#if DEBUG
            IEnumerable<ItemIndex> blacklistedItems = bannedItemTags.SelectMany(t => ItemCatalog.GetItemsWithTag(t))
                                                                    .Concat(ItemCatalog.allItems.Where(i => isBlacklisted(PickupCatalog.FindPickupIndex(i))))
                                                                    .Distinct()
                                                                    .OrderBy(i => i);

            Log.Debug($"Excluded items: [{string.Join(", ", blacklistedItems.Select(FormatUtils.GetBestItemDisplayName))}]");
#endif

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

        static bool isBlacklisted(PickupIndex pickupIndex)
        {
            if (_itemBlacklist.Contains(pickupIndex))
                return true;

            if (_applyAIBlacklist.Value)
            {
                PickupDef pickupDef = PickupCatalog.GetPickupDef(pickupIndex);
                if (pickupDef is not null)
                {
                    ItemIndex itemIndex = pickupDef.itemIndex;

                    // Eulogy Zero
                    if (itemIndex == DLC1Content.Items.RandomlyLunar.itemIndex)
                        return true;
                }
            }

            return false;
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
                    if (isBlacklisted(pickupIndex))
                    {
#if DEBUG
                        Log.Debug($"Removing {pickupIndex} from droptable: Blacklist");
#endif
                        selector.RemoveChoice(i);
                    }
                }

                void tryAddPickup(PickupIndex pickup, float weight)
                {
                    if (!isBlacklisted(pickup))
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

        PickupDef[] _grantedPickupDefs;

        public override void OnPreStartServer()
        {
            base.OnPreStartServer();

            if (!_dropTable || _dropTableDirty)
            {
                regenerateDropTable();
            }

            _grantedPickupDefs = new PickupDef[_itemCount.Value];
            for (int i = 0; i < _grantedPickupDefs.Length; i++)
            {
                _grantedPickupDefs[i] = PickupCatalog.GetPickupDef(_dropTable.GenerateDrop(RNG));
            }
        }

        public override void Serialize(NetworkWriter writer)
        {
            base.Serialize(writer);

            writer.WritePackedUInt32((uint)_grantedPickupDefs.Length);
            foreach (PickupDef pickup in _grantedPickupDefs)
            {
                writer.Write(pickup.pickupIndex);
            }
        }

        public override void Deserialize(NetworkReader reader)
        {
            base.Deserialize(reader);

            _grantedPickupDefs = new PickupDef[reader.ReadPackedUInt32()];
            for (int i = 0; i < _grantedPickupDefs.Length; i++)
            {
                _grantedPickupDefs[i] = PickupCatalog.GetPickupDef(reader.ReadPickupIndex());
            }
        }

        public override void OnStart()
        {
            Dictionary<PickupDef, uint> pickupCounts = new Dictionary<PickupDef, uint>();

            foreach (PickupDef pickupDef in _grantedPickupDefs)
            {
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

                pickupCounts[pickupDef] = pickupCount;
            }

            foreach (KeyValuePair<PickupDef, uint> pickupCountPair in pickupCounts)
            {
                Chat.SendBroadcastChat(new Chat.PlayerPickupChatMessage
                {
                    baseToken = "MONSTER_INVENTORY_ADD_ITEM",
                    pickupToken = pickupCountPair.Key.nameToken,
                    pickupColor = pickupCountPair.Key.baseColor,
                    pickupQuantity = pickupCountPair.Value
                });
            }

            CharacterMaster.readOnlyInstancesList.TryDo(master =>
            {
                if (canGiveItems(master))
                {
                    foreach (PickupDef pickupDef in _grantedPickupDefs)
                    {
                        master.inventory.TryGrant(pickupDef, true);
                    }
                }
            }, Util.GetBestMasterName);
        }

        public override void OnEnd()
        {
            if (_monsterInventory)
            {
                foreach (PickupDef pickupDef in _grantedPickupDefs)
                {
                    _monsterInventory.TryRemove(pickupDef);
                }
            }

            CharacterMaster.readOnlyInstancesList.TryDo(master =>
            {
                if (canGiveItems(master))
                {
                    foreach (PickupDef pickupDef in _grantedPickupDefs)
                    {
                        master.inventory.TryRemove(pickupDef);
                    }
                }
            }, Util.GetBestMasterName);
        }
    }
}
