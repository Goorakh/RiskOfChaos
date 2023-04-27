using RiskOfChaos.Components.CostTypeProvider;
using RoR2;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;

namespace RiskOfChaos.Networking.Components
{
    public sealed class SyncCostType : NetworkBehaviour
    {
        const uint COST_TYPE_DIRTY_BIT = 1 << 0;

        CostTypeIndex _lastCostType;
        ICostTypeProvider _costTypeProvider;

        void Awake()
        {
            if (TryGetComponent(out PurchaseInteraction purchaseInteraction))
            {
                _costTypeProvider = new PurchaseInteractionCostTypeProvider(purchaseInteraction);
            }
            else if (TryGetComponent(out MultiShopController multiShopController))
            {
                _costTypeProvider = new MultiShopControllerCostTypeProvider(multiShopController);
            }
            else
            {
                Log.Error($"No valid component found for {this}");
                enabled = false;
                return;
            }

            _lastCostType = _costTypeProvider.CostType;
        }

        void FixedUpdate()
        {
            if (hasAuthority && _lastCostType != _costTypeProvider.CostType)
            {
#if DEBUG
                Log.Debug($"{name} ({netId}): Cost type changed ({_lastCostType}->{_costTypeProvider.CostType}), setting dirty bit");
#endif

                SetDirtyBit(COST_TYPE_DIRTY_BIT);
                _lastCostType = _costTypeProvider.CostType;
            }
        }

        public override bool OnSerialize(NetworkWriter writer, bool initialState)
        {
            if (initialState)
            {
                writer.WritePackedUInt32((uint)_costTypeProvider.CostType);
                return true;
            }

            uint dirtyBits = syncVarDirtyBits;
            writer.WritePackedUInt32(dirtyBits);

            bool anythingWritten = false;
            if ((dirtyBits & COST_TYPE_DIRTY_BIT) != 0)
            {
                writer.WritePackedUInt32((uint)_costTypeProvider.CostType);
                anythingWritten |= true;
            }

            return anythingWritten;
        }

        public override void OnDeserialize(NetworkReader reader, bool initialState)
        {
            if (initialState)
            {
                _costTypeProvider.CostType = (CostTypeIndex)reader.ReadPackedUInt32();

#if DEBUG
                Log.Debug($"Set costType to {_costTypeProvider.CostType} (initialState)");
#endif

                return;
            }

            uint dirtyBits = reader.ReadPackedUInt32();
            if ((dirtyBits & COST_TYPE_DIRTY_BIT) != 0)
            {
                _costTypeProvider.CostType = (CostTypeIndex)reader.ReadPackedUInt32();

#if DEBUG
                Log.Debug($"Set costType to {_costTypeProvider.CostType} (dirtyBits)");
#endif
            }
        }

        [SystemInitializer]
        static void Init()
        {
            On.RoR2.PurchaseInteraction.Awake += (orig, self) =>
            {
                orig(self);

                if (!self.GetComponent<SyncCostType>())
                {
                    SyncCostType syncCostType = self.gameObject.AddComponent<SyncCostType>();
#if DEBUG
                    Log.Debug($"Added component to {self}");
#endif
                }
            };

            static void addToPrefab(string assetPath)
            {
                GameObject prefab = Addressables.LoadAssetAsync<GameObject>(assetPath).WaitForCompletion();

                if (!prefab)
                {
                    Log.Warning($"Null prefab at path {assetPath}");
                    return;
                }

                if (!prefab.GetComponent<SyncCostType>())
                {
                    prefab.AddComponent<SyncCostType>();
#if DEBUG
                    Log.Debug($"Added component to prefab {prefab}");
#endif
                }
            }

            addToPrefab("RoR2/Base/TripleShop/TripleShop.prefab");
            addToPrefab("RoR2/Base/TripleShopEquipment/TripleShopEquipment.prefab");
            addToPrefab("RoR2/Base/TripleShopLarge/TripleShopLarge.prefab");
        }
    }
}
