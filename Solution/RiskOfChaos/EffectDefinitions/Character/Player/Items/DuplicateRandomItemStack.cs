using RiskOfChaos.Collections.ParsedValue;
using RiskOfChaos.ConfigHandling;
using RiskOfChaos.EffectHandling;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Data;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.EffectHandling.EffectComponents;
using RiskOfChaos.Utilities;
using RiskOfChaos.Utilities.Comparers;
using RiskOfChaos.Utilities.Extensions;
using RiskOfChaos.Utilities.Pickup;
using RiskOfOptions.OptionConfigs;
using RoR2;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectDefinitions.Character.Player.Items
{
    [ChaosEffect("duplicate_random_item_stack")]
    public sealed class DuplicateRandomItemStack : NetworkBehaviour
    {
        [EffectConfig]
        static readonly ConfigHolder<int> _maxItemStacksConfig =
            ConfigFactory<int>.CreateConfig("Max Item Stacks", 0)
                              .Description("The maximum amount of item stacks to allow, the effect will not duplicate an item stack if it is greater than this number. Set to 0 to disable the limit.")
                              .OptionConfig(new IntFieldConfig { Min = 0 })
                              .Build();

        [EffectConfig]
        static readonly ConfigHolder<string> _itemBlacklistConfig =
            ConfigFactory<string>.CreateConfig("Item Blacklist", string.Empty)
                                 .Description("A comma-separated list of items that should not be allowed to be duplicated. Both internal and English display names are accepted, with spaces and commas removed.")
                                 .OptionConfig(new InputFieldConfig
                                 {
                                     lineType = TMPro.TMP_InputField.LineType.SingleLine,
                                     submitOn = InputFieldConfig.SubmitEnum.OnExitOrSubmit
                                 })
                                 .Build();

        static readonly ParsedItemList _itemBlacklist = new ParsedItemList(ItemIndexComparer.Instance)
        {
            ConfigHolder = _itemBlacklistConfig
        };

        [EffectCanActivate]
        static bool CanActivate(in EffectCanActivateContext context)
        {
            return !context.IsNow || PlayerUtils.GetAllPlayerMasters(false).Any(master => getAllDuplicatableItemStacks(master.inventory).Any());
        }

        static IEnumerable<ItemStack> getAllDuplicatableItemStacks(Inventory inventory)
        {
            if (!inventory)
                yield break;

            foreach (ItemIndex item in inventory.itemAcquisitionOrder)
            {
                ItemDef itemDef = ItemCatalog.GetItemDef(item);
                if (!itemDef || itemDef.hidden)
                    continue;

                if (_itemBlacklist.Contains(itemDef.itemIndex))
                {
                    Log.Debug($"{itemDef} cannot be duplicated: config blacklist");
                    continue;
                }

                int itemCount = inventory.GetItemCount(item);
                int maxItemStacks = _maxItemStacksConfig.Value;
                if (maxItemStacks > 0 && itemCount >= maxItemStacks)
                {
                    Log.Debug($"{itemDef} cannot be duplicated: max item stacks config is {maxItemStacks}, current: {itemCount}");
                    continue;
                }

                yield return new ItemStack(item, itemCount);
            }
        }

        ChaosEffectComponent _effectComponent;

        ItemIndex[] _itemDuplicationOrder;

        void Awake()
        {
            _effectComponent = GetComponent<ChaosEffectComponent>();
        }

        public override void OnStartServer()
        {
            base.OnStartServer();

            Xoroshiro128Plus rng = new Xoroshiro128Plus(_effectComponent.Rng.nextUlong);

            _itemDuplicationOrder = [.. ItemCatalog.allItems];
            Util.ShuffleArray(_itemDuplicationOrder, rng.Branch());

            Log.Debug($"Duplication order: [{string.Join(", ", _itemDuplicationOrder.Select(FormatUtils.GetBestItemDisplayName))}]");
        }

        bool tryGetStackToDuplicate(ItemStack[] availableItemStacks, out ItemStack result)
        {
            for (int i = 0; i < _itemDuplicationOrder.Length; i++)
            {
                int itemStackIndex = Array.FindIndex(availableItemStacks, s => s.ItemIndex == _itemDuplicationOrder[i]);
                if (itemStackIndex != -1)
                {
                    result = availableItemStacks[itemStackIndex];
                    return true;
                }
            }

            result = default;
            return false;
        }

        void Start()
        {
            if (NetworkServer.active)
            {
                PlayerUtils.GetAllPlayerMasters(false).TryDo(playerMaster =>
                {
                    Inventory inventory = playerMaster.inventory;
                    if (!inventory)
                        return;

                    ItemStack[] duplicatableItemStacks = getAllDuplicatableItemStacks(inventory).ToArray();
                    if (duplicatableItemStacks.Length <= 0)
                        return;

                    if (tryGetStackToDuplicate(duplicatableItemStacks, out ItemStack itemStack))
                    {
                        inventory.GiveItem(itemStack.ItemIndex, itemStack.ItemCount);

                        PickupUtils.QueuePickupMessage(playerMaster, PickupCatalog.FindPickupIndex(itemStack.ItemIndex));
                    }
                }, Util.GetBestMasterName);
            }
        }
    }
}
