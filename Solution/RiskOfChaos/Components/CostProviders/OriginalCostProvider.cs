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
            On.RoR2.PurchaseInteraction.Awake += (orig, self) =>
            {
                if (NetworkServer.active)
                {
                    self.gameObject.EnsureComponent<OriginalCostProvider>();
                }

                orig(self);
            };

            On.RoR2.MultiShopController.Start += (orig, self) =>
            {
                if (NetworkServer.active)
                {
                    self.gameObject.EnsureComponent<OriginalCostProvider>();
                }

                orig(self);
            };
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
                Log.Error($"Added to {name} on client");
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

#if DEBUG
            Log.Debug($"Determined base cost of {name}: {BaseCost} ({ActiveCostProvider.CostType})");
#endif
        }

        void Start()
        {
            if (TryGetComponent(out ShopTerminalBehavior shopTerminalBehavior))
            {
                if (shopTerminalBehavior.serverMultiShopController &&
                    shopTerminalBehavior.serverMultiShopController.TryGetComponent(out OriginalCostProvider multishopControllerCostProvider))
                {
                    BaseCost = multishopControllerCostProvider.BaseCost;

#if DEBUG
                    Log.Debug($"Determined actual base cost of {name} from {multishopControllerCostProvider.name}: {BaseCost}");
#endif
                }
            }

            OriginalCostType = ActiveCostProvider.CostType;
            OriginalCost = ActiveCostProvider.Cost;

#if DEBUG
            Log.Debug($"Initialized cost of {name}: {OriginalCostType} ({OriginalCost})");
#endif

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
