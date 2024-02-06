using HG;
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

        struct InventoryInfo : IEquatable<InventoryInfo>
        {
            public Inventory Inventory;
            public int StolenItemCount;

            public InventoryInfo(Inventory inventory, int stolenItemCount)
            {
                Inventory = inventory;
                StolenItemCount = stolenItemCount;
            }

            public override readonly bool Equals(object obj)
            {
                return obj is InventoryInfo info && Equals(info);
            }

            public readonly bool Equals(InventoryInfo other)
            {
                return EqualityComparer<Inventory>.Default.Equals(Inventory, other.Inventory) &&
                       StolenItemCount == other.StolenItemCount;
            }

            public override readonly int GetHashCode()
            {
                int hashCode = 1658819947;
                hashCode = (hashCode * -1521134295) + EqualityComparer<Inventory>.Default.GetHashCode(Inventory);
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

        int _inventoryInfosLength = 0;
        InventoryInfo[] _inventoryInfos = [];

        const uint INVENTORY_INFOS_DIRTY_BIT = 1 << 0;

        void Awake()
        {
            _itemStealController = GetComponent<ItemStealController>();
        }

        void FixedUpdate()
        {
            if (!_itemStealController || !NetworkServer.active)
                return;

#pragma warning disable Publicizer001 // Accessing a member that was not originally public
            ItemStealController.StolenInventoryInfo[] stolenInventoryInfos = _itemStealController.stolenInventoryInfos;
#pragma warning restore Publicizer001 // Accessing a member that was not originally public

            InventoryInfo[] newInventoryInfos = stolenInventoryInfos.Select(i => new InventoryInfo(i.victimInventory, i.stolenItemCount)).ToArray();

            if (_inventoryInfosLength != newInventoryInfos.Length ||
                Enumerable.Range(0, newInventoryInfos.Length).Any(i => _inventoryInfos[i] != newInventoryInfos[i]))
            {
                _inventoryInfosLength = newInventoryInfos.Length;

                ArrayUtils.EnsureCapacity(ref _inventoryInfos, _inventoryInfosLength);
                Array.Copy(newInventoryInfos, _inventoryInfos, _inventoryInfosLength);

                SetDirtyBit(INVENTORY_INFOS_DIRTY_BIT);
            }
        }

        public int GetStolenItemsCount(Inventory victimInventory)
        {
            for (int i = 0; i < _inventoryInfosLength; i++)
            {
                if (_inventoryInfos[i].Inventory == victimInventory)
                {
                    return _inventoryInfos[i].StolenItemCount;
                }
            }

            return 0;
        }

        public override bool OnSerialize(NetworkWriter writer, bool initialState)
        {
            if (initialState)
            {
                writer.WritePackedUInt32((uint)_inventoryInfosLength);
                for (int i = 0; i < _inventoryInfosLength; i++)
                {
                    writer.Write(_inventoryInfos[i].Inventory.gameObject);
                    writer.WritePackedUInt32((uint)_inventoryInfos[i].StolenItemCount);
                }

                return true;
            }

            uint dirtyBits = syncVarDirtyBits;
            writer.WritePackedUInt32(dirtyBits);

            bool anythingWritten = false;
            if ((dirtyBits & INVENTORY_INFOS_DIRTY_BIT) != 0)
            {
                writer.WritePackedUInt32((uint)_inventoryInfosLength);
                for (int i = 0; i < _inventoryInfosLength; i++)
                {
                    writer.Write(_inventoryInfos[i].Inventory.gameObject);
                    writer.WritePackedUInt32((uint)_inventoryInfos[i].StolenItemCount);
                }

                anythingWritten = true;
            }

            return anythingWritten;
        }

        public override void OnDeserialize(NetworkReader reader, bool initialState)
        {
            if (initialState)
            {
                _inventoryInfosLength = (int)reader.ReadPackedUInt32();
                ArrayUtils.EnsureCapacity(ref _inventoryInfos, _inventoryInfosLength);

                for (int i = 0; i < _inventoryInfosLength; i++)
                {
                    _inventoryInfos[i] = new InventoryInfo(reader.ReadGameObject()?.GetComponent<Inventory>(), (int)reader.ReadPackedUInt32());
                }

                return;
            }

            uint dirtybits = reader.ReadPackedUInt32();

            if ((dirtybits & INVENTORY_INFOS_DIRTY_BIT) != 0)
            {
                _inventoryInfosLength = (int)reader.ReadPackedUInt32();
                ArrayUtils.EnsureCapacity(ref _inventoryInfos, _inventoryInfosLength);

                for (int i = 0; i < _inventoryInfosLength; i++)
                {
                    _inventoryInfos[i] = new InventoryInfo(reader.ReadGameObject()?.GetComponent<Inventory>(), (int)reader.ReadPackedUInt32());
                }
            }
        }
    }
}
