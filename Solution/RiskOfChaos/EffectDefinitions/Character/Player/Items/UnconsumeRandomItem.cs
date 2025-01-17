﻿using RiskOfChaos.Collections.ParsedValue;
using RiskOfChaos.ConfigHandling;
using RiskOfChaos.ConfigHandling.AcceptableValues;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Data;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.EffectHandling.EffectComponents;
using RiskOfChaos.EffectUtils.Character.Player.Items;
using RiskOfChaos.Utilities;
using RiskOfChaos.Utilities.Comparers;
using RiskOfChaos.Utilities.Extensions;
using RiskOfChaos.Utilities.Pickup;
using RiskOfOptions.OptionConfigs;
using RoR2;
using System.Collections.Generic;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectDefinitions.Character.Player.Items
{
    [ChaosEffect("unconsume_random_item")]
    public sealed class UnconsumeRandomItem : NetworkBehaviour
    {
        [EffectConfig]
        static readonly ConfigHolder<int> _unconsumeCountConfig =
            ConfigFactory<int>.CreateConfig("Repair Count", 1)
                              .Description("How many items can be repaired per effect activation")
                              .AcceptableValues(new AcceptableValueMin<int>(1))
                              .OptionConfig(new IntFieldConfig { Min = 1 })
                              .Build();

        [EffectConfig]
        static readonly ConfigHolder<bool> _unconsumeFullStack =
            ConfigFactory<bool>.CreateConfig("Repair Full Stack", true)
                               .Description("If full stacks of items should be repaired instead of individual items")
                               .OptionConfig(new CheckBoxConfig())
                               .Build();

        [EffectConfig]
        static readonly ConfigHolder<string> _itemBlacklistConfig =
            ConfigFactory<string>.CreateConfig("Item Blacklist", string.Empty)
                                 .Description("A comma-separated list of items and equipment that should not be allowed to be repaired. Both internal and English display names are accepted, with spaces and commas removed.")
                                 .OptionConfig(new InputFieldConfig
                                 {
                                     lineType = TMPro.TMP_InputField.LineType.SingleLine,
                                     submitOn = InputFieldConfig.SubmitEnum.OnExitOrSubmit
                                 })
                                 .Build();

        static readonly ParsedPickupList _itemBlacklist = new ParsedPickupList(PickupIndexComparer.Instance)
        {
            ConfigHolder = _itemBlacklistConfig
        };

        static bool canUnconsume(ConsumableItemUtils.ConsumableItemPair consumablePair)
        {
            return consumablePair.Item.isValid && !_itemBlacklist.Contains(consumablePair.Item) &&
                   consumablePair.ConsumedItem.isValid && !_itemBlacklist.Contains(consumablePair.ConsumedItem);
        }

        [EffectCanActivate]
        static bool CanActivate()
        {
            foreach (PlayerCharacterMasterController playerMaster in PlayerCharacterMasterController.instances)
            {
                if (!playerMaster.isConnected)
                    continue;

                CharacterMaster master = playerMaster.master;
                if (!master || master.IsDeadAndOutOfLivesServer())
                    continue;

                Inventory inventory = master.inventory;
                if (!inventory)
                    continue;

                for (int i = 0; i < ConsumableItemUtils.ConsumableItemPairs.Length; i++)
                {
                    if (canUnconsume(ConsumableItemUtils.ConsumableItemPairs[i]) && inventory.GetPickupCount(ConsumableItemUtils.ConsumableItemPairs[i].ConsumedItem) > 0)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        ChaosEffectComponent _effectComponent;

        ulong _unconsumeRngSeed;

        void Awake()
        {
            _effectComponent = GetComponent<ChaosEffectComponent>();
        }

        public override void OnStartServer()
        {
            base.OnStartServer();

            Xoroshiro128Plus rng = new Xoroshiro128Plus(_effectComponent.Rng.nextUlong);

            _unconsumeRngSeed = rng.nextUlong;
        }

        void Start()
        {
            if (!NetworkServer.active)
                return;

            foreach (CharacterMaster master in CharacterMaster.readOnlyInstancesList)
            {
                if (!master || master.IsDeadAndOutOfLivesServer())
                    continue;

                if (!master.playerCharacterMasterController && master.teamIndex != TeamIndex.Player)
                    continue;

                Inventory inventory = master.inventory;
                if (!inventory)
                    continue;

                Xoroshiro128Plus playerRng = new Xoroshiro128Plus(_unconsumeRngSeed);

                HashSet<ConsumableItemUtils.ConsumableItemPair> unconsumedPairs = new HashSet<ConsumableItemUtils.ConsumableItemPair>(ConsumableItemUtils.ConsumableItemPairs.Length);

                int consumedItems = 0;
                while (consumedItems < _unconsumeCountConfig.Value && tryUnconsumeItemPair(master, new Xoroshiro128Plus(playerRng.nextUlong), unconsumedPairs))
                {
                    consumedItems++;
                }

                if (unconsumedPairs.Count > 0 && master.playerCharacterMasterController)
                {
                    PickupIndex[] pickupIndices = new PickupIndex[unconsumedPairs.Count];
                    int pickupIndex = 0;
                    foreach (ConsumableItemUtils.ConsumableItemPair consumablePair in unconsumedPairs)
                    {
                        CharacterMasterNotificationQueueUtils.SendPickupTransformNotification(master, consumablePair.ConsumedItem, consumablePair.Item, CharacterMasterNotificationQueue.TransformationType.Default);

                        pickupIndices[pickupIndex] = consumablePair.Item;
                        pickupIndex++;
                    }

                    PickupUtils.QueuePickupsMessage(master, pickupIndices, PickupNotificationFlags.SendChatMessage);
                }
            }
        }

        bool tryUnconsumeItemPair(CharacterMaster master, Xoroshiro128Plus rng, HashSet<ConsumableItemUtils.ConsumableItemPair> unconsumedPairs)
        {
            if (!master)
                return false;

            Inventory inventory = master.inventory;
            if (!inventory)
                return false;

            ConsumableItemUtils.ConsumableItemPair[] unconsumeOrder = new ConsumableItemUtils.ConsumableItemPair[ConsumableItemUtils.ConsumableItemPairs.Length];
            ConsumableItemUtils.ConsumableItemPairs.CopyTo(unconsumeOrder, 0);

            Util.ShuffleArray(unconsumeOrder, rng);

            foreach (ConsumableItemUtils.ConsumableItemPair consumablePair in unconsumeOrder)
            {
                if (canUnconsume(consumablePair))
                {
                    int pickupCount = inventory.GetPickupCount(consumablePair.ConsumedItem);
                    if (pickupCount > 0)
                    {
                        int consumeCount = _unconsumeFullStack.Value ? pickupCount : 1;
                        if (inventory.TryRemove(consumablePair.ConsumedItem, consumeCount))
                        {
                            inventory.TryGrant(consumablePair.Item, InventoryExtensions.EquipmentReplacementRule.DropExisting, consumeCount);

                            unconsumedPairs.Add(consumablePair);
                            return true;
                        }
                    }
                }
            }

            return false;
        }
    }
}
