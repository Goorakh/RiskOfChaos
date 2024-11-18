using HG;
using RiskOfChaos.ConfigHandling;
using RiskOfChaos.ConfigHandling.AcceptableValues;
using RiskOfChaos.Content;
using RiskOfChaos.EffectHandling;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Data;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.EffectHandling.EffectComponents;
using RiskOfChaos.EffectHandling.Formatting;
using RiskOfChaos.SaveHandling;
using RiskOfChaos.Utilities;
using RiskOfChaos.Utilities.DropTables;
using RiskOfChaos.Utilities.Extensions;
using RiskOfChaos.Utilities.Pickup;
using RiskOfOptions.OptionConfigs;
using RoR2;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectDefinitions.Character
{
    [ChaosTimedEffect("monster_inventory_give_random_item", TimedEffectType.Permanent, HideFromEffectsListWhenPermanent = true)]
    public sealed class MonsterInventoryGiveRandomItem : NetworkBehaviour
    {
        [EffectConfig]
        static readonly ConfigHolder<int> _itemCount =
            ConfigFactory<int>.CreateConfig("Item Count", 1)
                              .Description("The amount of items to give per effect activation")
                              .AcceptableValues(new AcceptableValueMin<int>(1))
                              .OptionConfig(new IntFieldConfig { Min = 1})
                              .Build();

        [EffectConfig]
        static readonly ConfigurableDropTable _dropTable;

        [EffectConfig]
        static readonly ConfigHolder<bool> _applyAIBlacklist =
            ConfigFactory<bool>.CreateConfig("Apply AI Blacklist", true)
                               .Description("If the effect should apply enemy item blacklist rules to the items it gives")
                               .OptionConfig(new CheckBoxConfig())
                               .OnValueChanged(markDropTableDirty)
                               .Build();

        static void markDropTableDirty()
        {
            _dropTable.MarkDirty();
        }

        static MonsterInventoryGiveRandomItem()
        {
            _dropTable = ScriptableObject.CreateInstance<ConfigurableDropTable>();
            _dropTable.name = $"dt{nameof(MonsterInventoryGiveRandomItem)}";
            _dropTable.canDropBeReplaced = false;

            _dropTable.RegisterDrop(DropType.Tier1, 1f);
            _dropTable.RegisterDrop(DropType.Tier2, 0.75f);
            _dropTable.RegisterDrop(DropType.Tier3, 0.3f);
            _dropTable.RegisterDrop(DropType.Boss, 0.4f);
            _dropTable.RegisterDrop(DropType.LunarItem, 0.25f);
            _dropTable.RegisterDrop(DropType.VoidTier1, 0.2f);
            _dropTable.RegisterDrop(DropType.VoidTier2, 0.15f);
            _dropTable.RegisterDrop(DropType.VoidTier3, 0.1f);
            _dropTable.RegisterDrop(DropType.VoidBoss, 0.1f);

            _dropTable.CreateItemBlacklistConfig("Item Blacklist", "A comma-separated list of items and equipment that should not be included for the effect. Both internal and English display names are accepted, with spaces and commas removed.");

            _dropTable.OnPreGenerate += () =>
            {
                List<ItemTag> bannedItemTags = [];
                if (_applyAIBlacklist.Value)
                {
                    bannedItemTags.AddRange([
                        ItemTag.AIBlacklist,
                        ItemTag.Scrap,
                        ItemTag.CannotCopy,
                        ItemTag.PriorityScrap
                    ]);
                }

                _dropTable.bannedItemTags = bannedItemTags.Distinct().ToArray();

#if DEBUG
                IEnumerable<ItemIndex> blacklistedItems = bannedItemTags.SelectMany(t => ItemCatalog.GetItemsWithTag(t))
                                                                        .Distinct()
                                                                        .OrderBy(i => i);

                Log.Debug($"Excluded items: [{string.Join(", ", blacklistedItems.Select(FormatUtils.GetBestItemDisplayName))}]");
#endif
            };

            _dropTable.AddDrops += (List<ExplicitDrop> drops) =>
            {
                drops.Add(new ExplicitDrop(RoR2Content.Items.ArtifactKey.itemIndex, DropType.Boss, null));
                drops.Add(new ExplicitDrop(RoR2Content.Items.CaptainDefenseMatrix.itemIndex, DropType.Tier3, null));
                drops.Add(new ExplicitDrop(RoR2Content.Items.Pearl.itemIndex, DropType.Boss, null));
                drops.Add(new ExplicitDrop(RoR2Content.Items.ShinyPearl.itemIndex, DropType.Boss, null));
                drops.Add(new ExplicitDrop(RoR2Content.Items.TonicAffliction.itemIndex, DropType.LunarItem, null));
            };

            _dropTable.RemoveDrops += (List<PickupIndex> removeDrops) =>
            {
                if (_applyAIBlacklist.Value)
                {
                    removeDrops.Add(PickupCatalog.FindPickupIndex(DLC1Content.Items.RandomlyLunar.itemIndex));
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

                if (!RoCContent.NetworkedPrefabs.GenericTeamInventory)
                {
                    Log.Warning($"{nameof(RoCContent.NetworkedPrefabs.GenericTeamInventory)} is null");
                    return;
                }

                GameObject monsterInventoryObj = GameObject.Instantiate(RoCContent.NetworkedPrefabs.GenericTeamInventory);
                _monsterInventory = monsterInventoryObj.GetComponent<Inventory>();

                _monsterTeamFilter = _monsterInventory.GetComponent<TeamFilter>();
                _monsterTeamFilter.teamIndex = TeamIndex.Monster;

                NetworkServer.Spawn(monsterInventoryObj);
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

        ChaosEffectComponent _effectComponent;
        ChaosEffectNameComponent _effectNameComponent;
        ObjectSerializationComponent _serializationComponent;

        [SerializedMember("p")]
        PickupIndex[] _pickupIndicesToGrant = [];

        void Awake()
        {
            _effectComponent = GetComponent<ChaosEffectComponent>();
            _effectNameComponent = GetComponent<ChaosEffectNameComponent>();
            _serializationComponent = GetComponent<ObjectSerializationComponent>();
        }

        public override void OnStartServer()
        {
            base.OnStartServer();

            Xoroshiro128Plus rng = new Xoroshiro128Plus(_effectComponent.Rng.nextUlong);

            _dropTable.RegenerateIfNeeded();

            _pickupIndicesToGrant = new PickupIndex[_itemCount.Value];
            for (int i = 0; i < _pickupIndicesToGrant.Length; i++)
            {
                _pickupIndicesToGrant[i] = _dropTable.GenerateDrop(rng);
            }
        }

        void Start()
        {
            if (!NetworkServer.active)
                return;

            HashSet<PickupIndex> grantedPickups = new HashSet<PickupIndex>(_pickupIndicesToGrant.Length);

            foreach (PickupIndex pickupIndex in _pickupIndicesToGrant)
            {
                PickupDef pickupDef = PickupCatalog.GetPickupDef(pickupIndex);
                if (pickupDef == null)
                    continue;

                if (_monsterInventory.TryGrant(pickupDef, InventoryExtensions.ItemReplacementRule.DeleteExisting))
                {
                    grantedPickups.Add(pickupIndex);
                }
            }

            if (grantedPickups.Count > 0)
            {
                PickupIndex[] pickupIndices = [.. grantedPickups];
                uint[] pickupQuantities = new uint[pickupIndices.Length];

                for (int i = 0; i < pickupIndices.Length; i++)
                {
                    pickupQuantities[i] = (uint)_monsterInventory.GetPickupCount(PickupCatalog.GetPickupDef(pickupIndices[i]));
                }

                if (!_serializationComponent || !_serializationComponent.IsLoadedFromSave)
                {
                    PickupUtils.QueuePickupsMessage("MONSTER_INVENTORY_ADD_ITEM", pickupIndices, pickupQuantities, PickupNotificationFlags.SendChatMessage);
                }

                CharacterMaster.readOnlyInstancesList.TryDo(master =>
                {
                    if (canGiveItems(master))
                    {
                        foreach (PickupIndex pickupIndex in grantedPickups)
                        {
                            master.inventory.TryGrant(PickupCatalog.GetPickupDef(pickupIndex), InventoryExtensions.ItemReplacementRule.DeleteExisting);
                        }
                    }
                }, Util.GetBestMasterName);

                if (_effectNameComponent)
                {
                    _effectNameComponent.SetCustomNameFormatter(new NameFormatter(pickupIndices));
                }
            }
        }

        void OnDestroy()
        {
            if (_monsterInventory)
            {
                foreach (PickupIndex pickupIndex in _pickupIndicesToGrant)
                {
                    _monsterInventory.TryRemove(PickupCatalog.GetPickupDef(pickupIndex));
                }
            }

            CharacterMaster.readOnlyInstancesList.TryDo(master =>
            {
                if (canGiveItems(master))
                {
                    foreach (PickupIndex pickupIndex in _pickupIndicesToGrant)
                    {
                        master.inventory.TryRemove(PickupCatalog.GetPickupDef(pickupIndex));
                    }
                }
            }, Util.GetBestMasterName);
        }

        class NameFormatter : EffectNameFormatter
        {
            PickupIndex[] _grantedPickups;

            public NameFormatter(PickupIndex[] grantedPickups)
            {
                _grantedPickups = grantedPickups;
            }

            public NameFormatter()
            {
            }

            public override string GetEffectNameSubtitle(ChaosEffectInfo effectInfo)
            {
                string subtitle = base.GetEffectNameSubtitle(effectInfo);

                if (_grantedPickups.Length > 0)
                {
                    StringBuilder stringBuilder = HG.StringBuilderPool.RentStringBuilder();
                    if (!string.IsNullOrWhiteSpace(subtitle))
                    {
                        stringBuilder.AppendLine(subtitle);
                    }

                    stringBuilder.Append("(");

                    for (int i = 0; i < _grantedPickups.Length; i++)
                    {
                        string pickupNameToken = PickupCatalog.invalidPickupToken;
                        Color pickupColor = PickupCatalog.invalidPickupColor;

                        PickupDef pickupDef = PickupCatalog.GetPickupDef(_grantedPickups[i]);
                        if (pickupDef != null)
                        {
                            pickupNameToken = pickupDef.nameToken;
                            pickupColor = pickupDef.baseColor;
                        }

                        stringBuilder.AppendColoredString(Language.GetString(pickupNameToken), pickupColor);

                        if (i != _grantedPickups.Length - 1)
                        {
                            stringBuilder.Append(", ");
                        }
                    }

                    stringBuilder.Append(")");

                    subtitle = stringBuilder.ToString();
                    stringBuilder = HG.StringBuilderPool.ReturnStringBuilder(stringBuilder);
                }

                return subtitle;
            }

            public override object[] GetFormatArgs()
            {
                return [];
            }

            public override void Serialize(NetworkWriter writer)
            {
                writer.WritePackedUInt32((uint)_grantedPickups.Length);
                foreach (PickupIndex pickup in _grantedPickups)
                {
                    writer.Write(pickup);
                }
            }

            public override void Deserialize(NetworkReader reader)
            {
                uint pickupCount = reader.ReadPackedUInt32();

                _grantedPickups = new PickupIndex[pickupCount];

                for (int i = 0; i < pickupCount; i++)
                {
                    _grantedPickups[i] = reader.ReadPickupIndex();
                }

                invokeFormatterDirty();
            }

            public override bool Equals(EffectNameFormatter other)
            {
                return other is NameFormatter otherFormatter &&
                       ArrayUtils.SequenceEquals(_grantedPickups, otherFormatter._grantedPickups);
            }
        }
    }
}
