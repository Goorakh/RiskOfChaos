using RiskOfChaos.ConfigHandling;
using RiskOfChaos.ConfigHandling.AcceptableValues;
using RiskOfChaos.Content;
using RiskOfChaos.EffectHandling;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Data;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.Utilities;
using RiskOfChaos.Utilities.DropTables;
using RiskOfChaos.Utilities.Extensions;
using RiskOfOptions.OptionConfigs;
using RoR2;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectDefinitions.Character
{
    [ChaosTimedEffect("monster_inventory_give_random_item", TimedEffectType.Permanent, HideFromEffectsListWhenPermanent = true)]
    public sealed class MonsterInventoryGiveRandomItem : TimedEffect
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

        PickupDef[] _grantedPickupDefs;

        public override void OnPreStartServer()
        {
            base.OnPreStartServer();

            _dropTable.RegenerateIfNeeded();

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
            Dictionary<PickupDef, uint> pickupCounts = [];

            foreach (PickupDef pickupDef in _grantedPickupDefs)
            {
                _monsterInventory.TryGrant(pickupDef, InventoryExtensions.ItemReplacementRule.DeleteExisting);

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
                        master.inventory.TryGrant(pickupDef, InventoryExtensions.ItemReplacementRule.DeleteExisting);
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
