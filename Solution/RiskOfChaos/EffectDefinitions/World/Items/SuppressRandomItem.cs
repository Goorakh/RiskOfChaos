using RiskOfChaos.Components;
using RiskOfChaos.EffectHandling;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.EffectHandling.EffectComponents;
using RiskOfChaos.EffectHandling.Formatting;
using RiskOfChaos.EffectUtils.World;
using RiskOfChaos.SaveHandling;
using RiskOfChaos.Utilities;
using RiskOfChaos.Utilities.Extensions;
using RoR2;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectDefinitions.World.Items
{
    [ChaosTimedEffect("suppress_random_item", TimedEffectType.Permanent, HideFromEffectsListWhenPermanent = true)]
    public sealed class SuppressRandomItem : NetworkBehaviour
    {
        [EffectCanActivate]
        static bool CanActivate()
        {
            return ExpansionUtils.DLC1Enabled && getAllSuppressableItems().Any();
        }

        static IEnumerable<ItemIndex> getAllSuppressableItems()
        {
            if (!Run.instance || Run.instance.availableItems == null)
                return [];

            return ItemCatalog.allItems.Where(i => Run.instance.IsItemAvailable(i) && ItemSuppressionManager.CanSuppressItem(i));
        }

        ChaosEffectComponent _effectComponent;
        ChaosEffectNameComponent _effectNameComponent;
        ObjectSerializationComponent _serializationComponent;

        [SerializedMember("i")]
        ItemIndex _itemToSuppress = ItemIndex.None;

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

            List<ItemIndex> suppressableItems = getAllSuppressableItems().ToList();
            _itemToSuppress = rng.NextElementUniform(suppressableItems);
        }

        void Start()
        {
            if (!NetworkServer.active)
                return;

            if (_itemToSuppress == ItemIndex.None)
            {
                Log.Error($"No item to suppress");
                return;
            }

            if (ItemSuppressionManager.SuppressItem(_itemToSuppress))
            {
                ItemDef suppressedItem = ItemCatalog.GetItemDef(_itemToSuppress);
                ItemTierDef itemTierDef = ItemTierCatalog.GetItemTierDef(suppressedItem.tier);

                if (!_serializationComponent.IsLoadedFromSave)
                {
                    Chat.SendBroadcastChat(new ColoredTokenChatMessage
                    {
                        subjectAsCharacterBody = ChaosInteractor.GetBody(),
                        baseToken = "VOID_SUPPRESSOR_USE_MESSAGE",
                        paramTokens = [suppressedItem.nameToken],
                        paramColors = [ColorCatalog.GetColor(itemTierDef.colorIndex)]
                    });
                }

                if (_effectNameComponent)
                {
                    _effectNameComponent.SetCustomNameFormatter(new NameFormatter(_itemToSuppress));
                }
            }
        }

        void OnDestroy()
        {
            if (_itemToSuppress != ItemIndex.None)
            {
                ItemSuppressionManager.RemoveSuppressedItem(_itemToSuppress);
            }
        }

        class NameFormatter : EffectNameFormatter
        {
            ItemIndex _suppressedItemIndex;

            public NameFormatter(ItemIndex suppressedItemIndex)
            {
                _suppressedItemIndex = suppressedItemIndex;
            }

            public NameFormatter()
            {
            }

            public override string GetEffectNameSubtitle(ChaosEffectInfo effectInfo)
            {
                string subtitle = base.GetEffectNameSubtitle(effectInfo);

                PickupDef suppressedItem = PickupCatalog.GetPickupDef(PickupCatalog.FindPickupIndex(_suppressedItemIndex));
                if (suppressedItem != null)
                {
                    StringBuilder stringBuilder = HG.StringBuilderPool.RentStringBuilder();

                    if (!string.IsNullOrWhiteSpace(subtitle))
                    {
                        stringBuilder.AppendLine(subtitle);
                    }

                    stringBuilder.Append("\n(");

                    stringBuilder.AppendColoredString(Language.GetString(suppressedItem.nameToken), suppressedItem.baseColor);

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
                writer.Write(_suppressedItemIndex);
            }

            public override void Deserialize(NetworkReader reader)
            {
                _suppressedItemIndex = reader.ReadItemIndex();
                invokeFormatterDirty();
            }

            public override bool Equals(EffectNameFormatter other)
            {
                return other is NameFormatter otherFormatter &&
                       _suppressedItemIndex == otherFormatter._suppressedItemIndex;
            }
        }
    }
}
