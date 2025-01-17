using RiskOfChaos.Patches;
using RiskOfChaos.Utilities.Extensions;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.Components.CostProviders
{
    public class OriginalCostProvider : MonoBehaviour, ICostProvider
    {
        [SystemInitializer]
        static void Init()
        {
            PurchaseInteractionHooks.OnPurchaseInteractionAwakeGlobal += onPurchaseInteractionAwakeGlobal;

            MultiShopControllerHooks.OnMultiShopControllerStartGlobal += onMultiShopControllerStartGlobal;
        }

        static void onPurchaseInteractionAwakeGlobal(PurchaseInteraction purchaseInteraction)
        {
            if (NetworkServer.active)
            {
                purchaseInteraction.gameObject.EnsureComponent<OriginalCostProvider>();
            }
        }

        static void onMultiShopControllerStartGlobal(MultiShopController multiShopController)
        {
            if (NetworkServer.active)
            {
                multiShopController.gameObject.EnsureComponent<OriginalCostProvider>();
            }
        }

        public delegate void OnOriginalCostInitializedDelegate(OriginalCostProvider originalCost);
        public static event OnOriginalCostInitializedDelegate OnOriginalCostInitialized;

        CostTypeIndex ICostProvider.CostType
        {
            get => OriginalCostType;
            set => OriginalCostType = value;
        }
        public CostTypeIndex OriginalCostType { get; private set; }

        int ICostProvider.Cost
        {
            get => OriginalCost;
            set => OriginalCost = value;
        }
        public int OriginalCost { get; private set; }

        public int BaseCost { get; private set; }

        public ICostProvider ActiveCostProvider { get; private set; }

        bool _isInitialized;

        void Awake()
        {
            if (!NetworkServer.active)
            {
                Log.Error($"Added to {Util.GetGameObjectHierarchyName(gameObject)} on client");
                enabled = false;
                return;
            }

            if (TryGetComponent(out PurchaseInteraction purchaseInteraction))
            {
                BaseCost = purchaseInteraction.cost;
                ActiveCostProvider = new PurchaseInteractionCostProvider(purchaseInteraction);
            }
            else if (TryGetComponent(out MultiShopController multiShopController))
            {
                BaseCost = multiShopController.baseCost;
                ActiveCostProvider = new MultiShopControllerCostProvider(multiShopController);
            }
            else
            {
                Log.Error($"No valid component found for {this}");
                enabled = false;
                return;
            }

            Log.Debug($"Determined base cost of {Util.GetGameObjectHierarchyName(gameObject)}: {BaseCost} ({ActiveCostProvider.CostType})");
        }

        void Start()
        {
            if (TryGetComponent(out ShopTerminalBehavior shopTerminalBehavior))
            {
                if (shopTerminalBehavior.serverMultiShopController &&
                    shopTerminalBehavior.serverMultiShopController.TryGetComponent(out OriginalCostProvider multishopControllerCostProvider))
                {
                    BaseCost = multishopControllerCostProvider.BaseCost;

                    Log.Debug($"Determined actual base cost of {Util.GetGameObjectHierarchyName(gameObject)} from {Util.GetGameObjectHierarchyName(multishopControllerCostProvider.gameObject)}: {BaseCost}");
                }
            }

            OriginalCostType = ActiveCostProvider.CostType;
            OriginalCost = ActiveCostProvider.Cost;

            Log.Debug($"Initialized cost of {Util.GetGameObjectHierarchyName(gameObject)}: {OriginalCostType} ({OriginalCost})");

            _isInitialized = true;
            InstanceTracker.Add(this);

            OnOriginalCostInitialized?.Invoke(this);
        }

        void OnEnable()
        {
            if (_isInitialized)
            {
                InstanceTracker.Add(this);
                OnOriginalCostInitialized?.Invoke(this);
            }
        }

        void OnDisable()
        {
            InstanceTracker.Remove(this);
        }

        public void ResetCost()
        {
            if (ActiveCostProvider == null)
            {
                Log.Error("Cannot reset cost, no active cost provider");
                return;
            }

            ActiveCostProvider.Cost = OriginalCost;
            ActiveCostProvider.CostType = OriginalCostType;
        }
    }
}
