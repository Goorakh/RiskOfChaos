using RoR2;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.Networking.Components
{
    [RequireComponent(typeof(ItemStealController))]
    public class SyncStolenItemCount : NetworkBehaviour
    {
        ItemStealController _itemStealController;

        class SyncListInventoryInfo : SyncListStruct<InventoryInfo> { }
        readonly SyncListInventoryInfo _inventoryInfos = new SyncListInventoryInfo();

        float _refreshInventoriesTimer;

        void Awake()
        {
            _itemStealController = GetComponent<ItemStealController>();
        }

        void FixedUpdate()
        {
            if (!_itemStealController)
                return;

            if (NetworkServer.active)
            {
                _refreshInventoriesTimer -= Time.fixedDeltaTime;
                if (_refreshInventoriesTimer <= 0)
                {
                    _refreshInventoriesTimer += GetNetworkSendInterval();
                    refreshInventories();
                }
            }
        }

        [Server]
        void refreshInventories()
        {
            List<InventoryInfo> previousInventoryInfos = _inventoryInfos.ToList();
            List<InventoryInfo> newInventoryInfos = _itemStealController.stolenInventoryInfos.Select(i => new InventoryInfo(i.victimInventory.gameObject, i.stolenItemCount)).ToList();

            for (int i = newInventoryInfos.Count - 1; i >= 0; i--)
            {
                InventoryInfo newInventoryInfo = newInventoryInfos[i];

                for (int j = 0; j < _inventoryInfos.Count; j++)
                {
                    if (_inventoryInfos[j].VictimObject == newInventoryInfo.VictimObject)
                    {
                        _inventoryInfos[j] = newInventoryInfo;

                        newInventoryInfos.RemoveAt(i);

                        break;
                    }
                }

                for (int j = 0; j < previousInventoryInfos.Count; j++)
                {
                    if (previousInventoryInfos[j].VictimObject == newInventoryInfo.VictimObject)
                    {
                        previousInventoryInfos.RemoveAt(j);
                        break;
                    }
                }
            }

            int replaceInventoryIndex = 0;

            foreach (InventoryInfo removeInventoryInfo in previousInventoryInfos)
            {
                for (int i = 0; i < _inventoryInfos.Count; i++)
                {
                    if (_inventoryInfos[i].VictimObject == removeInventoryInfo.VictimObject)
                    {
                        if (replaceInventoryIndex < newInventoryInfos.Count)
                        {
                            _inventoryInfos[i] = newInventoryInfos[replaceInventoryIndex++];
                        }
                        else
                        {
                            _inventoryInfos.RemoveAt(i);
                        }

                        break;
                    }
                }
            }

            for (int i = replaceInventoryIndex; i < newInventoryInfos.Count; i++)
            {
                _inventoryInfos.Add(newInventoryInfos[i]);
            }
        }

        public int GetStolenItemsCount(Inventory victimInventory)
        {
            for (int i = 0; i < _inventoryInfos.Count; i++)
            {
                if (_inventoryInfos[i].VictimObject == victimInventory.gameObject)
                {
                    return _inventoryInfos[i].StolenItemCount;
                }
            }

            return 0;
        }

        struct InventoryInfo : IEquatable<InventoryInfo>
        {
            public GameObject VictimObject;
            public int StolenItemCount;

            public InventoryInfo(GameObject victimObject, int stolenItemCount)
            {
                VictimObject = victimObject;
                StolenItemCount = stolenItemCount;
            }

            public override readonly bool Equals(object obj)
            {
                return obj is InventoryInfo info && Equals(info);
            }

            public readonly bool Equals(InventoryInfo other)
            {
                return VictimObject == other.VictimObject &&
                       StolenItemCount == other.StolenItemCount;
            }

            public override readonly int GetHashCode()
            {
                int hashCode = 1658819947;
                hashCode = (hashCode * -1521134295) + VictimObject.GetHashCode();
                hashCode = (hashCode * -1521134295) + StolenItemCount.GetHashCode();
                return hashCode;
            }

            public static bool operator ==(InventoryInfo left, InventoryInfo right)
            {
                return left.Equals(right);
            }

            public static bool operator !=(InventoryInfo left, InventoryInfo right)
            {
                return !(left == right);
            }
        }
    }
}
