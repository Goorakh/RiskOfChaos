using RiskOfChaos.Collections.ParsedValue;
using RiskOfChaos.ConfigHandling;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Data;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.EffectHandling.EffectComponents;
using RiskOfChaos.Utilities;
using RiskOfChaos.Utilities.Comparers;
using RiskOfChaos.Utilities.Extensions;
using RiskOfOptions.OptionConfigs;
using RoR2;
using RoR2.Items;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectDefinitions.Character.Player.Items
{
    [ChaosEffect("corrupt_random_item", DefaultSelectionWeight = 0.6f)]
    public sealed class CorruptRandomItem : NetworkBehaviour
    {
        [EffectConfig]
        static readonly ConfigHolder<string> _itemBlacklistConfig =
            ConfigFactory<string>.CreateConfig("Item Corruption Blacklist", string.Empty)
                                 .Description("A comma-separated list of items that should not be allowed to be corrupted. Both internal and English display names are accepted, with spaces and commas removed.")
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
        static bool CanActivate()
        {
            return ExpansionUtils.DLC1Enabled && PlayerUtils.GetAllPlayerMasters(false).Any(m => getAllCorruptableItems(m.inventory).Any());
        }

        static IEnumerable<ItemIndex> getAllCorruptableItems()
        {
            Run run = Run.instance;
            if (!run)
                yield break;

            foreach (ItemIndex item in ItemCatalog.allItems)
            {
                ItemIndex transformedItem = ContagiousItemManager.GetTransformedItemIndex(item);
                if (!run.IsItemEnabled(transformedItem))
                    continue;

                if (_itemBlacklist.Contains(item) || _itemBlacklist.Contains(transformedItem))
                {
#if DEBUG
                    Log.Debug($"Excluding {ItemCatalog.GetItemDef(item)} ({ItemCatalog.GetItemDef(transformedItem)}): Corruption blacklist");
#endif
                    continue;
                }

                yield return item;
            }
        }

        static IEnumerable<ItemIndex> getAllCorruptableItems(Inventory inventory)
        {
            if (!inventory)
                return [];

            return getAllCorruptableItems().Where(i => inventory.GetItemCount(i) > 0);
        }

        ChaosEffectComponent _effectComponent;

        ItemIndex[] _itemCorruptOrder;

        void Awake()
        {
            _effectComponent = GetComponent<ChaosEffectComponent>();
        }

        public override void OnStartServer()
        {
            base.OnStartServer();

            Xoroshiro128Plus rng = new Xoroshiro128Plus(_effectComponent.Rng.nextUlong);

            _itemCorruptOrder = getAllCorruptableItems().ToArray();
            Util.ShuffleArray(_itemCorruptOrder, rng.Branch());

#if DEBUG
            Log.Debug($"Corrupt order: [{string.Join(", ", _itemCorruptOrder.Select(FormatUtils.GetBestItemDisplayName))}]");
#endif
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

                    int itemToCorruptIndex = Array.FindIndex(_itemCorruptOrder, i => inventory.GetItemCount(i) > 0);
                    if (itemToCorruptIndex == -1) // This inventory has none of the corruptable items
                        return;

                    ContagiousItemManager.TryForceReplacement(inventory, _itemCorruptOrder[itemToCorruptIndex]);
                }, Util.GetBestMasterName);
            }
        }
    }
}
