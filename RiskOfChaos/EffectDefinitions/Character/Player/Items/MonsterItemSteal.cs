using HG;
using RiskOfChaos.ConfigHandling;
using RiskOfChaos.EffectHandling;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Data;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.Utilities;
using RiskOfChaos.Utilities.Comparers;
using RiskOfChaos.Utilities.Extensions;
using RiskOfChaos.Utilities.ParsedValueHolders.ParsedList;
using RiskOfOptions.OptionConfigs;
using RoR2;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RiskOfChaos.EffectDefinitions.Character.Player.Items
{
    [ChaosEffect("monster_item_steal", DefaultSelectionWeight = 0.6f)]
    [EffectConfigBackwardsCompatibility("Effect: Steal All Player Items")]
    public sealed class MonsterItemSteal : BaseEffect, ICoroutineEffect
    {
        const int MAX_ITEM_STEAL_COUNT_DEFAULT_VALUE = 2;

        [EffectConfig]
        static readonly ConfigHolder<string> _maxItemStealCountConfig =
            ConfigFactory<string>.CreateConfig("Max Item Steal Count", MAX_ITEM_STEAL_COUNT_DEFAULT_VALUE.ToString())
                                 .Description("The maximum amount of item stacks each enemy can steal from each player")
                                 .OptionConfig(new InputFieldConfig
                                 {
                                     lineType = TMPro.TMP_InputField.LineType.SingleLine,
                                     submitOn = InputFieldConfig.SubmitEnum.OnExitOrSubmit
                                 })
                                 .Build();

        static readonly ParsedInt32 _maxItemStealCount = new ParsedInt32
        {
            ConfigHolder = _maxItemStealCountConfig
        };

        [EffectConfig]
        static readonly ConfigHolder<bool> _ignoreAILendFilter =
            ConfigFactory<bool>.CreateConfig("Ignore AI Item Blacklist", false)
                               .Description("If the enemies should ignore the AI blacklist while having your items")
                               .OptionConfig(new CheckBoxConfig())
                               .Build();

        [EffectConfig]
        static readonly ConfigHolder<string> _itemBlacklistConfig =
            ConfigFactory<string>.CreateConfig("Item Steal Blacklist", string.Empty)
                                 .Description("A comma-separated list of items that will not be stolen from players. Both internal and English display names are accepted, with spaces and commas removed.")
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
            if (!NetPrefabs.MonsterItemStealControllerPrefab)
                return false;

            return !context.IsNow || PlayerUtils.GetAllPlayerMasters(true).Any(playerMaster =>
            {
                return getStealableItemStacks(playerMaster.inventory).Any() &&
                       CharacterMaster.readOnlyInstancesList.Any(m => canSteal(m, playerMaster));
            });
        }

        static bool itemStealFilter(ItemIndex itemIndex)
        {
            return ItemStealController.DefaultItemFilter(itemIndex) && !_itemBlacklist.Contains(itemIndex);
        }

        static IEnumerable<ItemIndex> getStealableItemStacks(Inventory inventory)
        {
            if (!inventory)
                yield break;

            foreach (ItemIndex item in inventory.itemAcquisitionOrder)
            {
                if (!itemStealFilter(item))
                    continue;

                yield return item;
            }
        }

        static bool canSteal(CharacterMaster stealer, CharacterMaster victim)
        {
            return stealer && stealer.hasBody && victim && victim.hasBody && stealer != victim && stealer.teamIndex != victim.teamIndex && !stealer.IsDeadAndOutOfLivesServer();
        }

        readonly List<SteppedStealController> _activeStealControllers = new List<SteppedStealController>();

        public override void OnStart()
        {
            _activeStealControllers.Capacity = CharacterMaster.readOnlyInstancesList.Count;

            CharacterMaster.onStartGlobal += initializeMasterForStealing;
        }

        void initializeMasterForStealing(CharacterMaster master)
        {
            if (!master)
                return;

            CharacterBody body = master.GetBody();
            if (!body || !body.healthComponent || !body.healthComponent.alive)
                return;

            CharacterItemStealInitializer stealInitializer = new CharacterItemStealInitializer(master);

            PlayerUtils.GetAllPlayerMasters(true).TryDo(playerMaster =>
            {
                if (!getStealableItemStacks(playerMaster.inventory).Any() || !canSteal(stealInitializer.Master, playerMaster))
                    return;

                stealInitializer.StartStealingFrom(playerMaster.inventory);
            }, Util.GetBestMasterName);

            if (stealInitializer.ItemStealController)
            {
                _activeStealControllers.Add(new SteppedStealController(stealInitializer));
            }
        }

        public IEnumerator OnStartCoroutine()
        {
            CharacterMaster.readOnlyInstancesList.TryDo(initializeMasterForStealing, Util.GetBestMasterName);

            int stealIterations = 0;
            float currentStealInterval = 0.3f;

            while (_activeStealControllers.Count > 0)
            {
                Util.ShuffleList(_activeStealControllers);

                for (int i = _activeStealControllers.Count - 1; i >= 0; i--)
                {
                    SteppedStealController stealController = _activeStealControllers[i];
                    if (!stealController.IsValid())
                    {
                        _activeStealControllers.RemoveAt(i);
                        continue;
                    }

                    stealController.ItemStealController.StepSteal();
                    stealController.NumSteps++;

                    if (stealController.NumSteps >= _maxItemStealCount.GetValue(MAX_ITEM_STEAL_COUNT_DEFAULT_VALUE) || !stealController.ItemStealController.inItemSteal)
                    {
                        _activeStealControllers.RemoveAt(i);

                        stealController.OnLastItemStolen();
                    }

                    yield return new WaitForSeconds(currentStealInterval);

                    stealIterations++;
                    if (stealIterations % 3 == 0)
                    {
                        currentStealInterval = Mathf.Max(0.1f, currentStealInterval * 0.925f);
                    }
                }
            }

            onEnd();
        }

        public void OnForceStopped()
        {
            onEnd();
        }

        void onEnd()
        {
            CharacterMaster.onStartGlobal -= initializeMasterForStealing;
        }

        record SteppedStealController(ItemStealController ItemStealController, CharacterMaster Master, int NumSteps)
        {
            public int NumSteps = NumSteps;

            public SteppedStealController(CharacterItemStealInitializer initializer) : this(initializer.ItemStealController, initializer.Master, 0)
            {
            }

            public bool IsValid()
            {
                if (!ItemStealController || !Master)
                    return false;

                CharacterBody body = Master.GetBody();
                if (!body || !body.healthComponent || !body.healthComponent.alive)
                    return false;

                return true;
            }

            public void OnLastItemStolen()
            {
                if (ItemStealController.inItemSteal)
                {
                    IEnumerator waitUntilAllOrbsArrivedThenSetNotStealing()
                    {
#pragma warning disable Publicizer001 // Accessing a member that was not originally public
                        yield return new WaitWhile(() => ItemStealController && ItemStealController.stolenInventoryInfos.Any(i => i.hasOrbsInFlight));
#pragma warning restore Publicizer001 // Accessing a member that was not originally public

                        if (!ItemStealController)
                            yield break;

#pragma warning disable Publicizer001 // Accessing a member that was not originally public
                        ItemStealController.inItemSteal = false;
#pragma warning restore Publicizer001 // Accessing a member that was not originally public
                    }

                    ItemStealController.StartCoroutine(waitUntilAllOrbsArrivedThenSetNotStealing());
                }
            }
        }

        class CharacterItemStealInitializer
        {
            public readonly CharacterMaster Master;

            public ItemStealController ItemStealController { get; private set; }
            ReturnStolenItemsOnGettingHit _returnStolenItems;

            bool _isStealing;

            public CharacterItemStealInitializer(CharacterMaster master)
            {
                Master = master;
            }

            ItemStealController getOrCreateItemStealController()
            {
                if (!ItemStealController)
                {
                    GameObject bodyObject = Master.GetBodyObject();
                    if (!bodyObject)
                        return null;

                    GameObject itemStealControllerObj = GameObject.Instantiate(NetPrefabs.MonsterItemStealControllerPrefab);
                    ItemStealController = itemStealControllerObj.GetComponent<ItemStealController>();

                    if (_ignoreAILendFilter.Value)
                    {
                        ItemStealController.itemLendFilter = _ => true;
                    }

                    ItemStealController.itemStealFilter = itemStealFilter;

                    ItemStealController.stealInterval = RoR2Application.rng.RangeFloat(0.3f, 0.6f);

                    if (!_returnStolenItems)
                    {
                        _returnStolenItems = bodyObject.GetComponent<ReturnStolenItemsOnGettingHit>();
                        if (!_returnStolenItems)
                        {
                            _returnStolenItems = bodyObject.AddComponent<ReturnStolenItemsOnGettingHit>();

#pragma warning disable Publicizer001 // Accessing a member that was not originally public
                            _returnStolenItems.minPercentagePerItem = 0.01f;
                            _returnStolenItems.maxPercentagePerItem = 100f;
#pragma warning restore Publicizer001 // Accessing a member that was not originally public

                            _returnStolenItems.itemStealController = ItemStealController;

                            HealthComponent healthComponent = bodyObject.GetComponent<CharacterBody>().healthComponent;
                            _returnStolenItems.healthComponent = healthComponent;

#pragma warning disable Publicizer001 // Accessing a member that was not originally public
                            ArrayUtils.ArrayAppend(ref healthComponent.onTakeDamageReceivers, _returnStolenItems);
#pragma warning restore Publicizer001 // Accessing a member that was not originally public
                        }
                    }

                    itemStealControllerObj.GetComponent<NetworkedBodyAttachment>().AttachToGameObjectAndSpawn(bodyObject);
                }

                return ItemStealController;
            }

            public void StartStealingFrom(Inventory inventory)
            {
                ItemStealController itemStealController = getOrCreateItemStealController();
                if (!itemStealController)
                    return;

                itemStealController.StartStealingFromInventory(inventory);

                // We want to control the stealing manually, this is probably the best way to do it?
                itemStealController.stealInterval = float.PositiveInfinity;
#pragma warning disable Publicizer001 // Accessing a member that was not originally public
                itemStealController.stealTimer = float.PositiveInfinity;
#pragma warning restore Publicizer001 // Accessing a member that was not originally public

                if (!_isStealing)
                {
                    _isStealing = true;
                    itemStealController.onStealFinishServer.AddListener(onStealFinish);
                }
            }

            void onStealFinish()
            {
                if (!_isStealing)
                    return;

                _isStealing = false;

                if (ItemStealController)
                {
                    ItemStealController.onStealFinishServer.RemoveListener(onStealFinish);
                    ItemStealController.LendImmediately(Master.inventory);
                }
            }
        }
    }
}
