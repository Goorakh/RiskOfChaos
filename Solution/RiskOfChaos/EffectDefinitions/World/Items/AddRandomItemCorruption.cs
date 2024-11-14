using HG;
using Newtonsoft.Json;
using RiskOfChaos.ChatMessages;
using RiskOfChaos.Collections.ParsedValue;
using RiskOfChaos.ConfigHandling;
using RiskOfChaos.ConfigHandling.AcceptableValues;
using RiskOfChaos.EffectHandling;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Data;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.EffectHandling.EffectComponents;
using RiskOfChaos.EffectHandling.Formatting;
using RiskOfChaos.EffectUtils.World;
using RiskOfChaos.SaveHandling;
using RiskOfChaos.Utilities.Comparers;
using RiskOfChaos.Utilities.Extensions;
using RiskOfOptions.OptionConfigs;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectDefinitions.World.Items
{
    [ChaosTimedEffect("add_random_item_corruption", TimedEffectType.Permanent)]
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
            ConfigFactory<string>.CreateConfig("Item Blacklist", "ArtifactKey")
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
                Log.Debug($"Including item {Utilities.FormatUtils.GetBestItemDisplayName(item)}");
#endif

                availableItems.Add((ItemIndex)i);
            }

            _availableItems = [.. availableItems];
        }

        static ItemIndex[] getTransformableItems(Func<ItemIndex, bool> filter = null)
        {
            List<ItemIndex> transformableItems = new List<ItemIndex>(_availableItems.Length);
            for (int i = 0; i < _availableItems.Length; i++)
            {
                ItemIndex itemIndex = _availableItems[i];
                ItemDef item = ItemCatalog.GetItemDef(itemIndex);

                if (!item.isConsumed && !item.ContainsTag(ItemTag.WorldUnique) && item != RoR2Content.Items.TonicAffliction && !Run.instance.IsItemEnabled(itemIndex))
                    continue;

                if (_itemBlacklist.Contains(itemIndex))
                    continue;

                if (filter == null || filter(itemIndex))
                {
                    transformableItems.Add(itemIndex);
                }
            }

            return transformableItems.ToArray();
        }

        static ItemIndex[] getAllTransformableFromItems()
        {
            return getTransformableItems(CustomContagiousItemManager.CanItemBeTransformedFrom);
        }

        static ItemIndex[] getAllTransformableToItems(ItemIndex from)
        {
            return getTransformableItems(to => CustomContagiousItemManager.CanItemBeTransformedInto(from, to));
        }

        [EffectCanActivate]
        static bool CanActivate()
        {
            foreach (ItemIndex from in getAllTransformableFromItems())
            {
                ItemIndex[] toItemIndices = getAllTransformableToItems(from);
                if (toItemIndices.Length > 0)
                {
                    return true;
                }
            }

            return false;
        }

        ChaosEffectComponent _effectComponent;
        ChaosEffectNameComponent _effectNameComponent;
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
            _effectNameComponent = GetComponent<ChaosEffectNameComponent>();
            _serializationComponent = GetComponent<ObjectSerializationComponent>();
        }

        public override void OnStartServer()
        {
            base.OnStartServer();

            Xoroshiro128Plus rng = new Xoroshiro128Plus(_effectComponent.Rng.nextUlong);

            List<ItemIndex> fromItemIndices = [.. getAllTransformableFromItems()];
            if (fromItemIndices.Count == 0)
            {
                Log.Error("Not enough available items");
                return;
            }

            int numPairsAdded = 0;
            while (fromItemIndices.Count > 0 && numPairsAdded < _numCorruptionsToAdd.Value)
            {
                ItemIndex fromItemIndex = fromItemIndices.GetAndRemoveRandom(rng);

                ItemIndex[] toItemIndices = getAllTransformableToItems(fromItemIndex);
                if (toItemIndices.Length > 0)
                {
                    ItemIndex toItemIndex = rng.NextElementUniform(toItemIndices);
                    _itemTransformPairs.Add(new ItemTransformationPair(fromItemIndex, toItemIndex));

                    numPairsAdded++;
                }
            }

            if (numPairsAdded == 0)
            {
                Log.Error("Not enough available items");
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

            if (NetworkServer.active)
            {
                ItemTransformationPair[] transformationPairs = new ItemTransformationPair[_itemTransformPairs.Count];
                for (int i = 0; i < transformationPairs.Length; i++)
                {
                    transformationPairs[i] = _itemTransformPairs[i];
                }

                _effectNameComponent.SetCustomNameFormatter(new NameFormatter(transformationPairs));
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
            [JsonProperty("f")]
            public ItemIndex From;

            [JsonProperty("t")]
            public ItemIndex To;

            public ItemTransformationPair(ItemIndex from, ItemIndex to)
            {
                From = from;
                To = to;
            }

            public ItemTransformationPair()
            {
            }

            public readonly bool Equals(ItemTransformationPair other)
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

        class NameFormatter : EffectNameFormatter
        {
            ItemTransformationPair[] _transformationPairs;

            public NameFormatter(ItemTransformationPair[] transformationPairs)
            {
                _transformationPairs = transformationPairs;
            }

            public NameFormatter()
            {
            }

            public override void Serialize(NetworkWriter writer)
            {
                writer.WritePackedUInt32((uint)_transformationPairs.Length);
                foreach (ItemTransformationPair pair in _transformationPairs)
                {
                    writer.Write(pair.From);
                    writer.Write(pair.To);
                }
            }

            public override void Deserialize(NetworkReader reader)
            {
                uint transformPairCount = reader.ReadPackedUInt32();
                _transformationPairs = new ItemTransformationPair[transformPairCount];
                for (int i = 0; i < transformPairCount; i++)
                {
                    ItemIndex from = reader.ReadItemIndex();
                    ItemIndex to = reader.ReadItemIndex();

                    _transformationPairs[i] = new ItemTransformationPair(from, to);
                }

                invokeFormatterDirty();
            }

            public override string GetEffectNameSubtitle(ChaosEffectInfo effectInfo)
            {
                string subtitle = base.GetEffectNameSubtitle(effectInfo);
                if (_transformationPairs.Length > 0)
                {
                    StringBuilder stringBuilder = HG.StringBuilderPool.RentStringBuilder();

                    if (!string.IsNullOrWhiteSpace(subtitle))
                    {
                        stringBuilder.AppendLine(subtitle);
                    }

                    for (int i = 0; i < _transformationPairs.Length; i++)
                    {
                        ItemTransformationPair pair = _transformationPairs[i];

                        Color fromPickupColor = PickupCatalog.invalidPickupColor;
                        string fromPickupNameToken = PickupCatalog.invalidPickupToken;
                        PickupDef fromPickup = PickupCatalog.GetPickupDef(PickupCatalog.FindPickupIndex(pair.From));
                        if (fromPickup != null)
                        {
                            fromPickupColor = fromPickup.baseColor;
                            fromPickupNameToken = fromPickup.nameToken;
                        }

                        Color toPickupColor = PickupCatalog.invalidPickupColor;
                        string toPickupNameToken = PickupCatalog.invalidPickupToken;
                        PickupDef toPickup = PickupCatalog.GetPickupDef(PickupCatalog.FindPickupIndex(pair.To));
                        if (toPickup != null)
                        {
                            toPickupColor = toPickup.baseColor;
                            toPickupNameToken = toPickup.nameToken;
                        }

                        stringBuilder.AppendColoredString(Language.GetString(fromPickupNameToken), fromPickupColor)
                                     .Append(" -> ")
                                     .AppendColoredString(Language.GetString(toPickupNameToken), toPickupColor);

                        if (i != _transformationPairs.Length - 1)
                        {
                            stringBuilder.AppendLine();
                        }
                    }

                    subtitle = stringBuilder.ToString();
                    stringBuilder = HG.StringBuilderPool.ReturnStringBuilder(stringBuilder);
                }

                return subtitle;
            }

            public override object[] GetFormatArgs()
            {
                return [];
            }

            public override bool Equals(EffectNameFormatter other)
            {
                return other is NameFormatter otherFormatter &&
                       ArrayUtils.SequenceEquals(_transformationPairs, otherFormatter._transformationPairs);
            }
        }
    }
}
