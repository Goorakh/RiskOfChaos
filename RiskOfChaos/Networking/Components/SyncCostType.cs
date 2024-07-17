using RiskOfChaos.Components.CostProviders;
using RiskOfChaos.Networking.Wrappers;
using RoR2;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;

namespace RiskOfChaos.Networking.Components
{
    public sealed class SyncCostType : NetworkBehaviour
    {
        ICostProvider _costProvider;

        [SyncVar(hook = nameof(syncCostType))]
        CostTypeIndexWrapper _costType;

        void Awake()
        {
            if (TryGetComponent(out PurchaseInteraction purchaseInteraction))
            {
                _costProvider = new PurchaseInteractionCostProvider(purchaseInteraction);
            }
            else if (TryGetComponent(out MultiShopController multiShopController))
            {
                _costProvider = new MultiShopControllerCostProvider(multiShopController);
            }
            else
            {
                Log.Error($"No valid component found for {this}");
                enabled = false;
                return;
            }
        }

        public override void OnStartServer()
        {
            base.OnStartServer();

            _costType = _costProvider.CostType;
        }

        public override void OnStartClient()
        {
            base.OnStartClient();

            syncCostType(_costType);
        }

        void FixedUpdate()
        {
            if (NetworkServer.active)
            {
                _costType = _costProvider.CostType;
            }
        }

        void syncCostType(CostTypeIndexWrapper newCostType)
        {
            _costType = newCostType;

            if (_costProvider.CostType == newCostType)
                return;

#if DEBUG
            Log.Debug($"{name} ({netId}): Cost type changed ({_costProvider.CostType}->{newCostType})");
#endif

            _costProvider.CostType = newCostType;
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
                }
            }

            addToPrefab("RoR2/Base/TripleShop/TripleShop.prefab");
            addToPrefab("RoR2/Base/TripleShopEquipment/TripleShopEquipment.prefab");
            addToPrefab("RoR2/Base/TripleShopLarge/TripleShopLarge.prefab");
        }
    }
}
