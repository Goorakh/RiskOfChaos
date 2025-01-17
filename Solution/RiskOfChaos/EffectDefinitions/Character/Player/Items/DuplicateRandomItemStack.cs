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
            if (!context.IsNow)
                return true;

            foreach (PlayerCharacterMasterController playerMasterController in PlayerCharacterMasterController.instances)
            {
                if (!playerMasterController.isConnected)
                    continue;

                CharacterMaster playerMaster = playerMasterController.master;
                if (!playerMaster)
                    continue;

                Inventory inventory = playerMaster.inventory;
                if (!inventory)
                    continue;

                foreach (ItemIndex item in inventory.itemAcquisitionOrder)
                {
                    if (canDuplicateItemStack(item, inventory))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        static bool canDuplicateItemStack(ItemIndex item, Inventory inventory)
        {
            ItemDef itemDef = ItemCatalog.GetItemDef(item);
            if (!itemDef || itemDef.hidden)
                return false;

            if (inventory.GetItemCount(item) <= 0)
                return false;

            if (_itemBlacklist.Contains(itemDef.itemIndex))
            {
                Log.Debug($"{itemDef} cannot be duplicated: config blacklist");
                return false;
            }

            int itemCount = inventory.GetItemCount(item);
            int maxItemStacks = _maxItemStacksConfig.Value;
            if (maxItemStacks > 0 && itemCount >= maxItemStacks)
            {
                Log.Debug($"{itemDef} cannot be duplicated: max item stacks config is {maxItemStacks}, current: {itemCount}");
                return false;
            }

            return true;
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

            _itemDuplicationOrder = new ItemIndex[ItemCatalog.itemCount];
            for (int i = 0; i < _itemDuplicationOrder.Length; i++)
            {
                _itemDuplicationOrder[i] = (ItemIndex)i;
            }

            Util.ShuffleArray(_itemDuplicationOrder, rng.Branch());

            Log.Debug($"Duplication order: [{string.Join(", ", _itemDuplicationOrder.Select(FormatUtils.GetBestItemDisplayName))}]");
        }

        void Start()
        {
            if (!NetworkServer.active)
                return;
            
            foreach (CharacterMaster master in CharacterMaster.readOnlyInstancesList)
            {
                Inventory inventory = master.inventory;
                if (!inventory)
                    continue;

                if (!master.IsPlayerOrPlayerAlly())
                    continue;

                foreach (ItemIndex item in _itemDuplicationOrder)
                {
                    if (canDuplicateItemStack(item, inventory))
                    {
                        try
                        {
                            inventory.GiveItem(item, inventory.GetItemCount(item));

                            if (master.playerCharacterMasterController)
                            {
                                PickupUtils.QueuePickupMessage(master, PickupCatalog.FindPickupIndex(item));
                            }
                        }
                        catch (Exception e)
                        {
                            Log.Error_NoCallerPrefix($"Failed to duplicate item '{FormatUtils.GetBestItemDisplayName(item)}' for '{Util.GetBestMasterName(master)}': {e}");
                        }

                        break;
                    }
                }
            }
        }
    }
}
