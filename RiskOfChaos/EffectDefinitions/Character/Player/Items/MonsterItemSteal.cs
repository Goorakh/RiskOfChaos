using HG;
using RiskOfChaos.EffectHandling;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.Utilities;
using RoR2;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RiskOfChaos.EffectDefinitions.Character.Player.Items
{
    [ChaosEffect("monster_item_steal", DefaultSelectionWeight = 0.6f)]
    public sealed class MonsterItemSteal : BaseEffect
    {
        [EffectCanActivate]
        static bool CanActivate(EffectCanActivateContext context)
        {
            if (!NetPrefabs.MonsterItemStealControllerPrefab)
                return false;

            return !context.IsNow || PlayerUtils.GetAllPlayerMasters(true).Any(playerMaster =>
            {
                return getStealableItemStacks(playerMaster.inventory).Any() &&
                       CharacterMaster.readOnlyInstancesList.Any(m => canSteal(m, playerMaster));
            });
        }

        static IEnumerable<ItemIndex> getStealableItemStacks(Inventory inventory)
        {
            if (!inventory)
                yield break;

            foreach (ItemIndex item in inventory.itemAcquisitionOrder)
            {
                if (ItemStealController.DefaultItemFilter(item) && inventory.GetItemCount(item) > 0)
                {
                    yield return item;
                }
            }
        }

        static bool canSteal(CharacterMaster stealer, CharacterMaster victim)
        {
            return stealer && stealer.hasBody && victim && victim.hasBody && stealer != victim && stealer.teamIndex != victim.teamIndex && !stealer.IsDeadAndOutOfLivesServer();
        }

        List<CharacterItemStealInitializer> _itemStealInitializers;

        public override void OnStart()
        {
            _itemStealInitializers = new List<CharacterItemStealInitializer>(CharacterMaster.readOnlyInstancesList.Count);
            foreach (CharacterMaster master in CharacterMaster.readOnlyInstancesList)
            {
                if (master && !master.IsDeadAndOutOfLivesServer() && master.hasBody)
                {
                    _itemStealInitializers.Add(new CharacterItemStealInitializer(master));
                }
            }

            foreach (CharacterMaster playerMaster in PlayerUtils.GetAllPlayerMasters(true))
            {
                if (playerMaster.hasBody)
                {
                    tryStealItemsFrom(playerMaster);
                }
            }
        }

        void tryStealItemsFrom(CharacterMaster master)
        {
            if (!getStealableItemStacks(master.inventory).Any())
                return;

            foreach (CharacterItemStealInitializer itemStealInitializer in _itemStealInitializers)
            {
                if (canSteal(itemStealInitializer.Master, master))
                {
                    itemStealInitializer.StartStealingFrom(master.inventory);
                }
            }
        }

        class CharacterItemStealInitializer
        {
            public readonly CharacterMaster Master;

            ItemStealController _itemStealController;
            ReturnStolenItemsOnGettingHit _returnStolenItems;

            bool _isStealing;

            public CharacterItemStealInitializer(CharacterMaster master)
            {
                Master = master;
            }

            ItemStealController getOrCreateItemStealController()
            {
                if (!_itemStealController)
                {
                    GameObject bodyObject = Master.GetBodyObject();
                    if (!bodyObject)
                        return null;

                    GameObject itemStealControllerObj = GameObject.Instantiate(NetPrefabs.MonsterItemStealControllerPrefab);
                    _itemStealController = itemStealControllerObj.GetComponent<ItemStealController>();

                    _itemStealController.stealInterval = RoR2Application.rng.RangeFloat(0.3f, 0.6f);

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

                            _returnStolenItems.itemStealController = _itemStealController;

                            HealthComponent healthComponent = bodyObject.GetComponent<CharacterBody>().healthComponent;
                            _returnStolenItems.healthComponent = healthComponent;

#pragma warning disable Publicizer001 // Accessing a member that was not originally public
                            ArrayUtils.ArrayAppend(ref healthComponent.onTakeDamageReceivers, _returnStolenItems);
#pragma warning restore Publicizer001 // Accessing a member that was not originally public
                        }
                    }

                    itemStealControllerObj.GetComponent<NetworkedBodyAttachment>().AttachToGameObjectAndSpawn(bodyObject);
                }

                return _itemStealController;
            }

            public void StartStealingFrom(Inventory inventory)
            {
                ItemStealController itemStealController = getOrCreateItemStealController();
                if (!itemStealController)
                    return;

                itemStealController.StartStealingFromInventory(inventory);

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

                if (_itemStealController)
                {
                    _itemStealController.onStealFinishServer.RemoveListener(onStealFinish);
                    _itemStealController.LendImmediately(Master.inventory);
                }
            }
        }
    }
}
