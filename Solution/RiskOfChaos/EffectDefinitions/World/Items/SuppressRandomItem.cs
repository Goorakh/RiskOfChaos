using RiskOfChaos.Components;
using RiskOfChaos.Content;
using RiskOfChaos.EffectHandling;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.EffectHandling.EffectComponents;
using RiskOfChaos.EffectHandling.EffectComponents.SubtitleProviders;
using RiskOfChaos.EffectUtils.World;
using RiskOfChaos.SaveHandling;
using RiskOfChaos.Utilities;
using RoR2;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectDefinitions.World.Items
{
    [ChaosTimedEffect("suppress_random_item", TimedEffectType.Permanent, HideFromEffectsListWhenPermanent = true)]
    [RequiredComponents(typeof(PickupListSubtitleProvider))]
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
        PickupListSubtitleProvider _pickupListSubtitleProvider;
        ObjectSerializationComponent _serializationComponent;

        [SerializedMember("i")]
        ItemIndex _itemToSuppress = ItemIndex.None;

        void Awake()
        {
            _effectComponent = GetComponent<ChaosEffectComponent>();
            _pickupListSubtitleProvider = GetComponent<PickupListSubtitleProvider>();
            _serializationComponent = GetComponent<ObjectSerializationComponent>();
        }

        public override void OnStartServer()
        {
            base.OnStartServer();

            Xoroshiro128Plus rng = new Xoroshiro128Plus(_effectComponent.Rng.nextUlong);

            List<ItemIndex> suppressableItems = [.. getAllSuppressableItems()];
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
            }

            if (_pickupListSubtitleProvider)
            {
                _pickupListSubtitleProvider.AddPickup(PickupCatalog.FindPickupIndex(_itemToSuppress));
            }
        }

        void OnDestroy()
        {
            if (_itemToSuppress != ItemIndex.None)
            {
                ItemSuppressionManager.RemoveSuppressedItem(_itemToSuppress);
            }
        }
    }
}
