using RiskOfChaos.ConfigHandling;
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
using RoR2;
using System.Collections.Generic;
using System.Linq;

namespace RiskOfChaos.EffectDefinitions.Character.Player.Items
{
    [ChaosEffect("duplicate_random_item_stack", EffectWeightReductionPercentagePerActivation = 0f)]
    public sealed class DuplicateRandomItemStack : BaseEffect
    {
        const int MAX_ITEM_STACKS_DEFAULT_VALUE = 1000;

        [EffectConfig]
        static readonly ConfigHolder<string> _maxItemStacksConfig =
            ConfigFactory<string>.CreateConfig("Max Item Stacks", MAX_ITEM_STACKS_DEFAULT_VALUE.ToString())
                                 .Description("The maximum amount of item stacks to allow, the effect will not duplicate an item stack if it is greater than this number. Set to a negative number to disable the limit.")
                                 .OptionConfig(new InputFieldConfig
                                 {
                                     submitOn = InputFieldConfig.SubmitEnum.OnSubmit
                                 })
                                 .Build();

        static readonly ParsedInt32 _maxItemStacks = new ParsedInt32();

        [EffectConfig]
        static readonly ConfigHolder<string> _itemBlacklistConfig =
            ConfigFactory<string>.CreateConfig("Item Blacklist", string.Empty)
                                 .Description("A comma-separated list of items that should not be allowed to be duplicated. Both internal and English display names are accepted, with spaces and commas removed.")
                                 .OptionConfig(new InputFieldConfig
                                 {
                                     submitOn = InputFieldConfig.SubmitEnum.OnSubmit
                                 })
                                 .Build();

        static readonly ParsedItemList _itemBlacklist = new ParsedItemList(ItemIndexComparer.Instance);

        [SystemInitializer(typeof(ChaosEffectCatalog))]
        static void InitConfigs()
        {
            _maxItemStacks.BindToConfig(_maxItemStacksConfig.Entry);

            _itemBlacklist.BindToConfig(_itemBlacklistConfig.Entry);
        }

        [EffectCanActivate]
        static bool CanActivate(EffectCanActivateContext context)
        {
            return !context.IsNow || PlayerUtils.GetAllPlayerMasters(false).Any(master => getAllDuplicatableItemStacks(master.inventory).Any());
        }

        readonly record struct ItemStack(ItemIndex ItemIndex, int ItemCount);

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
