using RiskOfChaos.Collections.ParsedValue;
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
    [ChaosEffect("consume_random_item", DefaultSelectionWeight = 0.6f)]
    public sealed class ConsumeRandomItem : NetworkBehaviour
    {
        [EffectConfig]
        static readonly ConfigHolder<int> _consumeCountConfig =
            ConfigFactory<int>.CreateConfig("Consume Count", 1)
                              .Description("How many items can be consumed per effect activation")
                              .AcceptableValues(new AcceptableValueMin<int>(1))
                              .OptionConfig(new IntFieldConfig
                              {
                                  Min = 1
                              })
                              .Build();

        [EffectConfig]
        static readonly ConfigHolder<string> _itemBlacklistConfig =
            ConfigFactory<string>.CreateConfig("Item Blacklist", string.Empty)
                                 .Description("A comma-separated list of items and equipment that should not be allowed to be consumed. Both internal and English display names are accepted, with spaces and commas removed.")
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

        static bool canConsume(ConsumableItemUtils.ConsumableItemPair consumablePair)
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
                    if (canConsume(ConsumableItemUtils.ConsumableItemPairs[i]) && inventory.GetPickupCount(ConsumableItemUtils.ConsumableItemPairs[i].Item) > 0)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        ChaosEffectComponent _effectComponent;

        ulong _consumeRngSeed;

        void Awake()
        {
            _effectComponent = GetComponent<ChaosEffectComponent>();
        }

        public override void OnStartServer()
        {
            base.OnStartServer();

            Xoroshiro128Plus rng = new Xoroshiro128Plus(_effectComponent.Rng.nextUlong);

            _consumeRngSeed = rng.nextUlong;
        }

        void Start()
        {
            if (!NetworkServer.active)
                return;

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

                Xoroshiro128Plus playerRng = new Xoroshiro128Plus(_consumeRngSeed);

                HashSet<ConsumableItemUtils.ConsumableItemPair> consumedPairs = new HashSet<ConsumableItemUtils.ConsumableItemPair>(ConsumableItemUtils.ConsumableItemPairs.Length);

                int consumedItems = 0;
                while (consumedItems < _consumeCountConfig.Value && tryConsumeItemPair(master, new Xoroshiro128Plus(playerRng.nextUlong), consumedPairs))
                {
                    consumedItems++;
                }

                if (consumedPairs.Count > 0)
                {
                    PickupIndex[] pickupIndices = new PickupIndex[consumedPairs.Count];
                    int pickupIndex = 0;
                    foreach (ConsumableItemUtils.ConsumableItemPair consumablePair in consumedPairs)
                    {
                        CharacterMasterNotificationQueueUtils.SendPickupTransformNotification(master, consumablePair.Item, consumablePair.ConsumedItem, CharacterMasterNotificationQueue.TransformationType.Default);

                        pickupIndices[pickupIndex] = consumablePair.ConsumedItem;
                        pickupIndex++;
                    }

                    PickupUtils.QueuePickupsMessage(master, pickupIndices, PickupNotificationFlags.SendChatMessage);
                }
            }
        }

        bool tryConsumeItemPair(CharacterMaster master, Xoroshiro128Plus rng, HashSet<ConsumableItemUtils.ConsumableItemPair> consumedPairs)
        {
            if (!master)
                return false;

            Inventory inventory = master.inventory;
            if (!inventory)
                return false;

            ConsumableItemUtils.ConsumableItemPair[] consumeOrder = new ConsumableItemUtils.ConsumableItemPair[ConsumableItemUtils.ConsumableItemPairs.Length];
            ConsumableItemUtils.ConsumableItemPairs.CopyTo(consumeOrder, 0);

            Util.ShuffleArray(consumeOrder, rng);

            foreach (ConsumableItemUtils.ConsumableItemPair consumablePair in consumeOrder)
            {
                if (canConsume(consumablePair) && inventory.GetPickupCount(consumablePair.Item) > 0)
                {
                    if (inventory.TryRemove(consumablePair.Item))
                    {
                        inventory.TryGrant(consumablePair.ConsumedItem, InventoryExtensions.ItemReplacementRule.DropExisting);

                        consumedPairs.Add(consumablePair);
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
