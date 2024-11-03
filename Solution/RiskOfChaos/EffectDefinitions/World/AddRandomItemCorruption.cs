using RiskOfChaos.ChatMessages;
using RiskOfChaos.Collections.ParsedValue;
using RiskOfChaos.ConfigHandling;
using RiskOfChaos.ConfigHandling.AcceptableValues;
using RiskOfChaos.EffectHandling;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Data;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.EffectHandling.EffectComponents;
using RiskOfChaos.EffectUtils.World;
using RiskOfChaos.SaveHandling;
using RiskOfChaos.Utilities;
using RiskOfChaos.Utilities.Comparers;
using RiskOfChaos.Utilities.Extensions;
using RiskOfOptions.OptionConfigs;
using RoR2;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectDefinitions.World
{
    [ChaosTimedEffect("add_random_item_corruption", TimedEffectType.Permanent, HideFromEffectsListWhenPermanent = true)]
    public sealed class AddRandomItemCorruption : NetworkBehaviour
    {
        [EffectConfig]
        static readonly ConfigHolder<int> _numCorruptionsToAdd =
            ConfigFactory<int>.CreateConfig("Num Corruption Rules", 1)
                              .Description("The amount of item corruption rules to add per effect activation")
                              .AcceptableValues(new AcceptableValueMin<int>(1))
                              .OptionConfig(new IntFieldConfig
                              {
                                  Min = 1
                              })
                              .Build();

        [EffectConfig]
        static readonly ConfigHolder<string> _itemBlacklistConfig =
            ConfigFactory<string>.CreateConfig("Item Blacklist", string.Empty)
                                 .Description("A comma-separated list of items that should not be allowed to be used by the effect. Both internal and English display names are accepted, with spaces and commas removed.")
                                 .OptionConfig(new InputFieldConfig
                                 {
                                     submitOn = InputFieldConfig.SubmitEnum.OnExitOrSubmit,
                                     lineType = TMPro.TMP_InputField.LineType.SingleLine,
                                     richText = false
                                 })
                                 .Build();

        static readonly ParsedItemList _itemBlacklist = new ParsedItemList(ItemIndexComparer.Instance)
        {
            ConfigHolder = _itemBlacklistConfig
        };

        static ItemIndex[] _availableItems = [];

        [SystemInitializer(typeof(ItemCatalog))]
        static void Init()
        {
            List<ItemIndex> availableItems = new List<ItemIndex>(ItemCatalog.itemCount);

            for (int i = 0; i < ItemCatalog.itemCount; i++)
            {
                ItemDef item = ItemCatalog.GetItemDef((ItemIndex)i);

                if (!item || item.hidden)
                    continue;

                if (!item.canRemove && !item.isConsumed && item != RoR2Content.Items.TonicAffliction)
                    continue;

                if (Language.IsTokenInvalid(item.nameToken))
                    continue;

                if (Language.IsTokenInvalid(item.pickupToken) && Language.IsTokenInvalid(item.descriptionToken))
                    continue;

#if DEBUG
                Log.Debug($"Including item {FormatUtils.GetBestItemDisplayName(item)}");
#endif

                availableItems.Add((ItemIndex)i);
            }

            _availableItems = [.. availableItems];
        }

        static IEnumerable<ItemDef.Pair> getTransformableItemPairs()
        {
            static IEnumerable<ItemIndex> getTransformableItems(Func<ItemIndex, bool> filter = null)
            {
                for (int i = 0; i < _availableItems.Length; i++)
                {
                    ItemIndex itemIndex = _availableItems[i];
                    ItemDef item = ItemCatalog.GetItemDef(itemIndex);

                    if (!item.isConsumed && !item.ContainsTag(ItemTag.WorldUnique) && item != RoR2Content.Items.TonicAffliction && !Run.instance.IsItemEnabled(itemIndex))
                    {
#if DEBUG
                        Log.Debug($"Excluding non-enabled item {FormatUtils.GetBestItemDisplayName(item)}");
#endif
                        continue;
                    }

                    if (_itemBlacklist.Contains(itemIndex))
                        continue;

                    if (filter == null || filter(itemIndex))
                    {
                        yield return itemIndex;
                    }
                }
            }

            foreach (ItemIndex from in getTransformableItems(CustomContagiousItemManager.CanItemBeTransformedFrom))
            {
                foreach (ItemIndex to in getTransformableItems(CustomContagiousItemManager.CanItemBeTransformedInto))
                {
                    if (from == to)
                        continue;

                    yield return new ItemDef.Pair
                    {
                        itemDef1 = ItemCatalog.GetItemDef(from),
                        itemDef2 = ItemCatalog.GetItemDef(to)
                    };
                }
            }
        }

        [EffectCanActivate]
        static bool CanActivate()
        {
            return getTransformableItemPairs().Any();
        }

        ChaosEffectComponent _effectComponent;

        ObjectSerializationComponent _serializationComponent;

        readonly SyncListItemTransformationPair _itemTransformPairs = [];

        [SerializedMember("p")]
        ItemTransformationPair[] serializedItemTransformPairs
        {
            get
            {
                return [.. _itemTransformPairs];
            }
            set
            {
                _itemTransformPairs.Clear();

                if (value != null)
                {
                    foreach (ItemTransformationPair pair in value)
                    {
                        _itemTransformPairs.Add(pair);
                    }
                }
            }
        }

        void Awake()
        {
            _effectComponent = GetComponent<ChaosEffectComponent>();
            _serializationComponent = GetComponent<ObjectSerializationComponent>();
        }

        public override void OnStartServer()
        {
            base.OnStartServer();

            Xoroshiro128Plus rng = new Xoroshiro128Plus(_effectComponent.Rng.nextUlong);

            List<ItemDef.Pair> itemPairs = getTransformableItemPairs().ToList();
            if (itemPairs.Count == 0)
            {
                Log.Error("Not enough available items");
                return;
            }

            int itemTransformPairCount = Mathf.Min(_numCorruptionsToAdd.Value, itemPairs.Count);
            _itemTransformPairs.Clear();

            Util.ShuffleList(itemPairs, rng);

            for (int i = 0; i < itemTransformPairCount; i++)
            {
                ItemDef.Pair pair = itemPairs[i];
                _itemTransformPairs.Add(new ItemTransformationPair(pair.itemDef1.itemIndex, pair.itemDef2.itemIndex));
            }
        }

        void Start()
        {
            foreach (ItemTransformationPair pair in _itemTransformPairs)
            {
                CustomContagiousItemManager.AddItemTransformation(pair.From, pair.To);

                if (!_serializationComponent || !_serializationComponent.IsLoadedFromSave)
                {
                    if (NetworkServer.active)
                    {
                        Chat.SendBroadcastChat(new PickupNameFormatMessage
                        {
                            Token = "ITEM_CORRUPTION_RULE_ADD_MESSAGE",
                            PickupIndices = [PickupCatalog.FindPickupIndex(pair.From), PickupCatalog.FindPickupIndex(pair.To)]
                        });
                    }
                }
            }
        }

        void OnDestroy()
        {
            foreach (ItemTransformationPair pair in _itemTransformPairs)
            {
                CustomContagiousItemManager.RemoveItemTransformation(pair.From, pair.To);
            }
        }

        struct ItemTransformationPair : IEquatable<ItemTransformationPair>
        {
            [SerializedMember("f")]
            public ItemIndex From;

            [SerializedMember("t")]
            public ItemIndex To;

            public ItemTransformationPair(ItemIndex from, ItemIndex to)
            {
                From = from;
                To = to;
            }

            public ItemTransformationPair()
            {
            }

            public bool Equals(ItemTransformationPair other)
            {
                return From == other.From && To == other.To;
            }
        }

        class SyncListItemTransformationPair : SyncListStruct<ItemTransformationPair>
        {
            public override void SerializeItem(NetworkWriter writer, ItemTransformationPair item)
            {
                writer.Write(item.From);
                writer.Write(item.To);
            }

            public override ItemTransformationPair DeserializeItem(NetworkReader reader)
            {
                ItemIndex from = reader.ReadItemIndex();
                ItemIndex to = reader.ReadItemIndex();

                return new ItemTransformationPair(from, to);
            }
        }
    }
}
