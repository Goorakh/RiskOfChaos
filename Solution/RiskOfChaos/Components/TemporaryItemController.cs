using RiskOfChaos.Patches;
using RiskOfChaos.Utilities;
using RiskOfChaos.Utilities.Extensions;
using RiskOfChaos.Utilities.Pickup;
using RoR2;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.Components
{
    public class TemporaryItemController : MonoBehaviour
    {
        Inventory _inventory;

        readonly List<TemporaryItemInfo> _temporaryItems = [];

        void Awake()
        {
            _inventory = GetComponent<Inventory>();
        }

        void OnEnable()
        {
            CharacterBody.onBodyDestroyGlobal += onBodyDestroyGlobal;
            OnCharacterHitGroundServerHook.OnCharacterHitGround += onCharacterHitGroundServer;
        }

        void OnDisable()
        {
            CharacterBody.onBodyDestroyGlobal -= onBodyDestroyGlobal;
            OnCharacterHitGroundServerHook.OnCharacterHitGround -= onCharacterHitGroundServer;
        }

        void OnDestroy()
        {
            clearTemporaryItems(TemporaryItemCondition.All);
        }

        bool isOurBody(CharacterBody body)
        {
            return body && _inventory && body.inventory == _inventory;
        }

        void clearTemporaryItems(TemporaryItemCondition removeCondition)
        {
            if (!NetworkServer.active)
            {
                Log.Warning("Called on client");
                return;
            }

            for (int i = _temporaryItems.Count - 1; i >= 0; i--)
            {
                TemporaryItemInfo temporaryItemInfo = _temporaryItems[i];
                if (temporaryItemInfo == null || (temporaryItemInfo.Condition & removeCondition) != 0)
                {
                    temporaryItemInfo?.RemoveFrom(_inventory);

                    _temporaryItems.RemoveAt(i);
                }
            }
        }

        void onCharacterHitGroundServer(CharacterBody characterBody, in CharacterMotor.HitGroundInfo hitGroundInfo)
        {
            if (isOurBody(characterBody))
            {
                clearTemporaryItems(TemporaryItemCondition.Airborne);
            }
        }

        void onBodyDestroyGlobal(CharacterBody body)
        {
            if (!NetworkServer.active)
                return;

            if (isOurBody(body))
            {
                clearTemporaryItems(TemporaryItemCondition.Airborne);
            }
        }

        public void AddTemporaryItem(ItemIndex itemIndex, int count, TemporaryItemCondition condition, TemporaryItemFlags flags = TemporaryItemFlags.None)
        {
            if (!NetworkServer.active)
            {
                Log.Warning("Called on client");
                return;
            }

            if (itemIndex == ItemIndex.None)
                return;

            if (_inventory)
            {
                TemporaryItemInfo temporaryItemInfo = new TemporaryItemInfo(itemIndex, count, condition, flags);
                temporaryItemInfo.AddTo(_inventory);
                _temporaryItems.Add(temporaryItemInfo);
            }
        }

        public static void AddTemporaryItem(Inventory inventory, ItemDef itemDef, TemporaryItemCondition condition, TemporaryItemFlags flags = TemporaryItemFlags.None)
        {
            AddTemporaryItem(inventory, itemDef, 1, condition, flags);
        }

        public static void AddTemporaryItem(Inventory inventory, ItemDef itemDef, int count, TemporaryItemCondition condition, TemporaryItemFlags flags = TemporaryItemFlags.None)
        {
            AddTemporaryItem(inventory, itemDef ? itemDef.itemIndex : ItemIndex.None, count, condition, flags);
        }

        public static void AddTemporaryItem(Inventory inventory, ItemIndex itemIndex, TemporaryItemCondition condition, TemporaryItemFlags flags = TemporaryItemFlags.None)
        {
            AddTemporaryItem(inventory, itemIndex, condition, flags);
        }

        public static void AddTemporaryItem(Inventory inventory, ItemIndex itemIndex, int count, TemporaryItemCondition condition, TemporaryItemFlags flags = TemporaryItemFlags.None)
        {
            if (!inventory || itemIndex == ItemIndex.None)
                return;

            TemporaryItemController tempItemController = inventory.gameObject.EnsureComponent<TemporaryItemController>();
            tempItemController.AddTemporaryItem(itemIndex, count, condition, flags);
        }

        class TemporaryItemInfo
        {
            public readonly ItemIndex ItemIndex;
            public readonly int Count;
            public readonly TemporaryItemCondition Condition;
            public readonly TemporaryItemFlags Flags;

            public TemporaryItemInfo(ItemIndex itemIndex, int count, TemporaryItemCondition condition, TemporaryItemFlags flags)
            {
                ItemIndex = itemIndex;
                Count = count;
                Condition = condition;
                Flags = flags;
            }

            public void AddTo(Inventory inventory)
            {
                if (!inventory)
                    return;

                ItemDef itemDef = ItemCatalog.GetItemDef(ItemIndex);
                CharacterMaster master = inventory.GetComponent<CharacterMaster>();

                try
                {
                    inventory.GiveItem(ItemIndex, Count);
                }
                catch (Exception e)
                {
                    Log.Error_NoCallerPrefix($"Unhandled exception giving temporary item {this} to inventory {inventory}: {e}");
                }

                if ((Flags & TemporaryItemFlags.Silent) == 0)
                {
                    if (!itemDef.hidden && master && master.playerCharacterMasterController)
                    {
                        PickupUtils.QueuePickupMessage(master, PickupCatalog.FindPickupIndex(ItemIndex), PickupNotificationFlags.DisplayPushNotificationIfNoneQueued | PickupNotificationFlags.PlaySound);
                    }
                }

                if ((Flags & TemporaryItemFlags.SuppressItemTransformation) != 0)
                {
                    IgnoreItemTransformations.IgnoreTransformationsFor(inventory, ItemIndex);
                }

                Log.Debug($"Gave temporary item {this} to {inventory}");
            }

            public void RemoveFrom(Inventory inventory)
            {
                if (!inventory)
                    return;

                try
                {
                    inventory.RemoveItem(ItemIndex, Count);
                }
                catch (Exception e)
                {
                    Log.Error_NoCallerPrefix($"Unhandled exception removing temporary item {this} from inventory {inventory}: {e}");
                }

                if ((Flags & TemporaryItemFlags.SuppressItemTransformation) != 0)
                {
                    IgnoreItemTransformations.ResumeTransformationsFor(inventory, ItemIndex);
                }

                Log.Debug($"Removed temporary item {this} from {inventory}");
            }

            public override string ToString()
            {
                return $"{FormatUtils.GetBestItemDisplayName(ItemIndex)} ({Count}): conditions=[{Condition}], flags=[{Flags}]";
            }
        }

        [Flags]
        public enum TemporaryItemCondition
        {
            None = 0,
            Airborne = 1 << 0,
            All = ~0
        }

        [Flags]
        public enum TemporaryItemFlags
        {
            None = 0,
            SuppressItemTransformation = 1 << 0,
            Silent = 1 << 1,
        }
    }
}
