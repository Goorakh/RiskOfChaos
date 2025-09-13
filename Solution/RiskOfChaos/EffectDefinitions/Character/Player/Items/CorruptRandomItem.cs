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
using UnityEngine.Networking;

namespace RiskOfChaos.EffectDefinitions.Character.Player.Items
{
    [ChaosEffect("corrupt_random_item", DefaultSelectionWeight = 0.5f)]
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
            List<ContagiousItemManager.TransformationInfo> itemCorruptions = getAllAvailableItemCorruptions();
            if (itemCorruptions.Count > 0)
            {
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
                    
                    foreach (ContagiousItemManager.TransformationInfo itemCorruptionInfo in itemCorruptions)
                    {
                        if (inventory.GetItemCount(itemCorruptionInfo.originalItem) > 0)
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        static List<ContagiousItemManager.TransformationInfo> getAllAvailableItemCorruptions()
        {
            Run run = Run.instance;
            if (!run)
                return [];

            List<ContagiousItemManager.TransformationInfo> itemCorruptions = new List<ContagiousItemManager.TransformationInfo>(ContagiousItemManager.transformationInfos.Length);

            for (int i = 0; i < ContagiousItemManager.transformationInfos.Length; i++)
            {
                ContagiousItemManager.TransformationInfo transformationInfo = ContagiousItemManager.transformationInfos[i];

                if (run.IsItemExpansionLocked(transformationInfo.originalItem) || run.IsItemExpansionLocked(transformationInfo.transformedItem))
                {
                    continue;
                }

                if (_itemBlacklist.Contains(transformationInfo.originalItem) || _itemBlacklist.Contains(transformationInfo.transformedItem))
                {
                    Log.Debug($"Excluding {ItemCatalog.GetItemDef(transformationInfo.originalItem)} ({ItemCatalog.GetItemDef(transformationInfo.transformedItem)}): Corruption blacklist");
                    continue;
                }

                itemCorruptions.Add(transformationInfo);
            }

            return itemCorruptions;
        }

        ChaosEffectComponent _effectComponent;

        ContagiousItemManager.TransformationInfo[] _itemCorruptionOrder = [];

        void Awake()
        {
            _effectComponent = GetComponent<ChaosEffectComponent>();
        }

        public override void OnStartServer()
        {
            base.OnStartServer();

            Xoroshiro128Plus rng = new Xoroshiro128Plus(_effectComponent.Rng.nextUlong);

            _itemCorruptionOrder = [.. getAllAvailableItemCorruptions()];
            Util.ShuffleArray(_itemCorruptionOrder, rng.Branch());

            Log.Debug($"Corrupt order: [{string.Join(", ", _itemCorruptionOrder.Select(t => FormatUtils.GetBestItemDisplayName(t.originalItem)))}]");
        }

        void Start()
        {
            if (!NetworkServer.active)
                return;
            
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

                int itemToCorruptIndex = Array.FindIndex(_itemCorruptionOrder, i => inventory.GetItemCount(i.originalItem) > 0);
                if (itemToCorruptIndex == -1) // This inventory has none of the corruptable items
                    return;

                ContagiousItemManager.TryForceReplacement(inventory, _itemCorruptionOrder[itemToCorruptIndex].originalItem);
            }
        }
    }
}
