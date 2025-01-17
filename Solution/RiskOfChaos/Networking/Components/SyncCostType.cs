using RiskOfChaos.Components.CostProviders;
using RiskOfChaos.Patches;
using RiskOfChaos.Utilities.Extensions;
using RoR2;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace RiskOfChaos.Networking.Components
{
    public sealed class SyncCostType : NetworkBehaviour
    {
        [SystemInitializer]
        static void Init()
        {
            PurchaseInteractionHooks.OnPurchaseInteractionAwakeGlobal += purchaseInteraction =>
            {
                purchaseInteraction.gameObject.EnsureComponent<SyncCostType>();
            };

            MultiShopControllerHooks.OnMultiShopControllerStartGlobal += multiShopController =>
            {
                if (!multiShopController.gameObject.GetComponent<SyncCostType>())
                {
                    Log.Warning($"MultiShopController {Util.GetGameObjectHierarchyName(multiShopController.gameObject)} is missing SyncCostType component, cost type will not be synchronized over the network");
                }
            };

            static void addComponentToPrefab(string prefabAssetPath)
            {
                AsyncOperationHandle<GameObject> prefabLoad = Addressables.LoadAssetAsync<GameObject>(prefabAssetPath);
                prefabLoad.OnSuccess(prefab => prefab.EnsureComponent<SyncCostType>());
            }

            addComponentToPrefab("RoR2/Base/TripleShop/TripleShop.prefab");
            addComponentToPrefab("RoR2/Base/TripleShopEquipment/TripleShopEquipment.prefab");
            addComponentToPrefab("RoR2/Base/TripleShopLarge/TripleShopLarge.prefab");
            addComponentToPrefab("RoR2/DLC1/FreeChestMultiShop/FreeChestMultiShop.prefab");
            addComponentToPrefab("RoR2/Junk/SingleLunarShop.prefab");
        }

        ICostProvider _costProvider;

        [SyncVar(hook = nameof(syncCostType))]
        int _costTypeInternal;

        public CostTypeIndex CostType
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (CostTypeIndex)_costTypeInternal;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private set => _costTypeInternal = (int)value;
        }

        void Awake()
        {
            _costProvider = ICostProvider.GetFromObject(gameObject);

            if (_costProvider == null)
            {
                Log.Error($"No valid component found for {this}");
                enabled = false;
                return;
            }
        }

        public override void OnStartServer()
        {
            base.OnStartServer();

            CostType = _costProvider.CostType;
        }

        public override void OnStartClient()
        {
            base.OnStartClient();

            syncCostType(_costTypeInternal);
        }

        void FixedUpdate()
        {
            if (NetworkServer.active)
            {
                CostType = _costProvider.CostType;
            }
        }

        void syncCostType(int newCostType)
        {
            _costTypeInternal = newCostType;

            if (_costProvider.CostType == CostType)
                return;

            Log.Debug($"{Util.GetGameObjectHierarchyName(gameObject)} ({netId}): Cost type changed ({_costProvider.CostType}->{CostType})");

            _costProvider.CostType = CostType;
        }
    }
}
