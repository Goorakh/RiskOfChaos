using RiskOfChaos.Components;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.EffectHandling.EffectComponents;
using RiskOfChaos.EffectUtils.World;
using RiskOfChaos.SaveHandling;
using RiskOfChaos.Utilities;
using RoR2;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectDefinitions.World.Items
{
    [ChaosEffect("suppress_random_item")]
    public sealed class SuppressRandomItem : NetworkBehaviour
    {
        [EffectCanActivate]
        static bool CanActivate()
        {
            return ExpansionUtils.DLC1Enabled && ItemSuppressionManager.Instance && getAllSuppressableItems().Any();
        }

        static IEnumerable<ItemIndex> getAllSuppressableItems()
        {
            if (!Run.instance || Run.instance.availableItems == null)
                return [];

            return ItemCatalog.allItems.Where(i => Run.instance.IsItemAvailable(i) && ItemSuppressionManager.CanSuppressItem(i));
        }

        ChaosEffectComponent _effectComponent;

        [SerializedMember("i")]
        ItemIndex _itemToSuppress = ItemIndex.None;

        void Awake()
        {
            _effectComponent = GetComponent<ChaosEffectComponent>();
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

            if (_itemToSuppress != ItemIndex.None && ItemSuppressionManager.Instance && ItemSuppressionManager.Instance.SuppressItem(_itemToSuppress))
            {
                ItemDef suppressedItem = ItemCatalog.GetItemDef(_itemToSuppress);
                ItemTierDef itemTierDef = ItemTierCatalog.GetItemTierDef(suppressedItem.tier);

                Chat.SendBroadcastChat(new ColoredTokenChatMessage
                {
                    subjectAsCharacterBody = ChaosInteractor.GetBody(),
                    baseToken = "VOID_SUPPRESSOR_USE_MESSAGE",
                    paramTokens = [suppressedItem.nameToken],
                    paramColors = [ColorCatalog.GetColor(itemTierDef.colorIndex)]
                });
            }
            else
            {
                Log.Error($"Failed to suppress item: {ItemCatalog.GetItemDef(_itemToSuppress)}");
            }
        }
    }
}
