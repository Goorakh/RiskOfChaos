using BepInEx.Configuration;
using RiskOfChaos.EffectHandling;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Data;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.Utilities;
using RiskOfChaos.Utilities.Comparers;
using RiskOfChaos.Utilities.Extensions;
using RiskOfChaos.Utilities.ParsedValueHolders;
using RiskOfChaos.Utilities.ParsedValueHolders.ParsedList;
using RiskOfOptions.OptionConfigs;
using RiskOfOptions.Options;
using RoR2;
using System.Collections.Generic;
using System.Linq;

namespace RiskOfChaos.EffectDefinitions.Character.Player.Items
{
    [ChaosEffect("duplicate_random_item_stack", EffectWeightReductionPercentagePerActivation = 0f)]
    public sealed class DuplicateRandomItemStack : BaseEffect
    {
        [InitEffectInfo]
        static readonly ChaosEffectInfo _effectInfo;

        static ConfigEntry<string> _maxItemStacksConfig;
        const int MAX_ITEM_STACKS_DEFAULT_VALUE = 1000;

        static readonly ParsedUInt32 _maxItemStacks = new ParsedUInt32();

        static ConfigEntry<string> _itemBlacklistConfig;
        static readonly ParsedItemList _itemBlacklist = new ParsedItemList(ItemIndexComparer.Instance);

        [SystemInitializer(typeof(ChaosEffectCatalog))]
        static void InitConfigs()
        {
            _maxItemStacksConfig = _effectInfo.BindConfig("Max Item Stacks", MAX_ITEM_STACKS_DEFAULT_VALUE.ToString(), new ConfigDescription("The maximum amount of item stacks to allow, the effect will not duplicate an item stack if it is greater than this number. Set to a negative number to disable the limit."));
            addConfigOption(new StringInputFieldOption(_maxItemStacksConfig, new InputFieldConfig
            {
                submitOn = InputFieldConfig.SubmitEnum.OnSubmit
            }));

            _maxItemStacks.BindToConfig(_maxItemStacksConfig);

            _itemBlacklistConfig = _effectInfo.BindConfig("Item Blacklist", string.Empty, new ConfigDescription("A comma-separated list of items that should not be allowed to be duplicated"));
            addConfigOption(new StringInputFieldOption(_itemBlacklistConfig, new InputFieldConfig
            {
                submitOn = InputFieldConfig.SubmitEnum.OnSubmit
            }));

            _itemBlacklist.BindToConfig(_itemBlacklistConfig);
        }

        [EffectCanActivate]
        static bool CanActivate(EffectCanActivateContext context)
        {
            return !context.IsNow || PlayerUtils.GetAllPlayerMasters(false).Any(master => getAllDuplicatableItemStacks(master.inventory).Any());
        }

        readonly struct ItemStack
        {
            public readonly ItemIndex ItemIndex;
            public readonly int ItemCount;

            public ItemStack(ItemIndex itemIndex, int itemCount)
            {
                ItemIndex = itemIndex;
                ItemCount = itemCount;
            }
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
#if DEBUG
                    Log.Debug($"{itemDef} cannot be duplicated: config blacklist");
#endif
                    continue;
                }

                int itemCount = inventory.GetItemCount(item);
                int maxItemStacks = _maxItemStacks.GetValue(MAX_ITEM_STACKS_DEFAULT_VALUE);
                if (maxItemStacks >= 0 && itemCount >= maxItemStacks)
                {
#if DEBUG
                    Log.Debug($"{itemDef} cannot be duplicated: max item stacks config is {maxItemStacks}, current: {itemCount}");
#endif
                    continue;
                }

                yield return new ItemStack(item, itemCount);
            }
        }

        public override void OnStart()
        {
            PlayerUtils.GetAllPlayerMasters(false).TryDo(playerMaster =>
            {
                Inventory inventory = playerMaster.inventory;
                if (!inventory)
                    return;

                ItemStack[] duplicatableItemStacks = getAllDuplicatableItemStacks(inventory).ToArray();
                if (duplicatableItemStacks.Length <= 0)
                    return;

                ItemStack itemStack = RNG.NextElementUniform(duplicatableItemStacks);
                inventory.GiveItem(itemStack.ItemIndex, itemStack.ItemCount);

                GenericPickupController.SendPickupMessage(playerMaster, PickupCatalog.FindPickupIndex(itemStack.ItemIndex));
            }, Util.GetBestMasterName);
        }
    }
}
