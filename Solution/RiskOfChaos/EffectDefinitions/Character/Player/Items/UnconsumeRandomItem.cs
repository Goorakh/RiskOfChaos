using HG;
using RiskOfChaos.Collections.ParsedValue;
using RiskOfChaos.ConfigHandling;
using RiskOfChaos.ConfigHandling.AcceptableValues;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Data;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.EffectHandling.EffectComponents;
using RiskOfChaos.EffectUtils.Character.Player.Items;
using RiskOfChaos.Utilities.Comparers;
using RiskOfChaos.Utilities.Extensions;
using RiskOfChaos.Utilities.Pickup;
using RiskOfOptions.OptionConfigs;
using RoR2;
using System;
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

        static bool canUnconsume(in ConsumableItemUtils.ConsumableItemPair consumablePair)
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
                    if (canUnconsume(ConsumableItemUtils.ConsumableItemPairs.Span[i]) && inventory.GetOwnedPickupCount(ConsumableItemUtils.ConsumableItemPairs.Span[i].ConsumedItem) > 0)
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

                using (SetPool<PickupIndex>.RentCollection(out HashSet<PickupIndex> newUnconsumedPickups))
                {
                    newUnconsumedPickups.EnsureCapacity(ConsumableItemUtils.ConsumableItemPairs.Length);

                    for (int i = _unconsumeCountConfig.Value; i > 0; i--)
                    {
                        Xoroshiro128Plus rng = playerRng.Branch();

                        Span<ConsumableItemUtils.ConsumableItemPair> consumeOrder = [.. ConsumableItemUtils.ConsumableItemPairs.Span];
                        Util.ShuffleSpan(consumeOrder, rng);

                        foreach (ref readonly ConsumableItemUtils.ConsumableItemPair consumablePair in consumeOrder)
                        {
                            InventoryExtensions.PickupTransformation pickupTransformation = new InventoryExtensions.PickupTransformation
                            {
                                AllowWhenDisabled = true,
                                MinToTransform = 1,
                                MaxToTransform = _unconsumeFullStack.Value ? int.MaxValue : 1,
                                OriginalPickupIndex = consumablePair.ConsumedItem,
                                NewPickupIndex = consumablePair.Item,
                                TransformationType = (ItemTransformationTypeIndex)CharacterMasterNotificationQueue.TransformationType.Default,
                                ReplacementRule = InventoryExtensions.PickupReplacementRule.DropExisting
                            };

                            if (pickupTransformation.TryTransform(inventory, out InventoryExtensions.PickupTransformation.TryTransformResult result))
                            {
                                newUnconsumedPickups.Add(result.GivenPickup.PickupIndex);
                                break;
                            }
                        }
                    }

                    if (newUnconsumedPickups.Count > 0 && master.playerCharacterMasterController)
                    {
                        PickupUtils.QueuePickupsMessage(master, [.. newUnconsumedPickups], PickupNotificationFlags.SendChatMessage);
                    }
                }
            }
        }
    }
}
