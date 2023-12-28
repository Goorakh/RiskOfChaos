using RiskOfChaos.Patches;
using RoR2;
using System.Collections;
using UnityEngine;
using UnityEngine.AddressableAssets;
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
                orig(self);

                if (!self.GetComponent<OriginalCostProvider>())
                {
                    self.gameObject.AddComponent<OriginalCostProvider>();
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

                if (!prefab.GetComponent<OriginalCostProvider>())
                {
                    prefab.AddComponent<OriginalCostProvider>();
                }
            }

            addToPrefab("RoR2/Base/TripleShop/TripleShop.prefab");
            addToPrefab("RoR2/Base/TripleShopEquipment/TripleShopEquipment.prefab");
            addToPrefab("RoR2/Base/TripleShopLarge/TripleShopLarge.prefab");
        }

        public delegate void OnOriginalCostInitializedDelegate(OriginalCostProvider originalCost);
        public static event OnOriginalCostInitializedDelegate OnOriginalCostInitialized;

        CostTypeIndex ICostProvider.CostType { get; set; }
        public CostTypeIndex CostType
        {
            get => ((ICostProvider)this).CostType;
            private set => ((ICostProvider)this).CostType = value;
        }

        int ICostProvider.Cost { get; set; }
        public int Cost
        {
            get => ((ICostProvider)this).Cost;
            private set => ((ICostProvider)this).Cost = value;
        }

        public int EstimatedBaseCost { get; private set; } = 0;

        public bool IsInitialized { get; private set; }

        public ICostProvider ActiveCostProvider { get; private set; }

        void Awake()
        {
            bool alreadyInitialized;
            if (TryGetComponent(out PurchaseInteraction purchaseInteraction))
            {
                alreadyInitialized = NetworkServer.active && purchaseInteraction.automaticallyScaleCostWithDifficulty;
                ActiveCostProvider = new PurchaseInteractionCostProvider(purchaseInteraction);
            }
            else if (TryGetComponent(out MultiShopController multiShopController))
            {
                alreadyInitialized = false;
                ActiveCostProvider = new MultiShopControllerCostProvider(multiShopController);
            }
            else
            {
                Log.Error($"No valid component found for {this}");
                enabled = false;
                return;
            }

            if (alreadyInitialized)
            {
                markInitialized();
            }
            else
            {
                StartCoroutine(waitThenInitialize());
            }
        }

        IEnumerator waitThenInitialize()
        {
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();

            markInitialized();
        }

        void markInitialized()
        {
            CostType = ActiveCostProvider.CostType;
            Cost = ActiveCostProvider.Cost;

            if (ActiveCostProvider is MultiShopControllerCostProvider multishopCostProvider)
            {
                EstimatedBaseCost = multishopCostProvider.MultiShopController.baseCost;
            }
            else if (CostType == CostTypeIndex.Money && Run.instance)
            {
                EstimatedBaseCost = Mathf.RoundToInt(Cost / Mathf.Pow(Run.instance.difficultyCoefficient, 1.25f));
            }
            else
            {
                EstimatedBaseCost = Cost;
            }

            IsInitialized = true;
            InstanceTracker.Add(this);

            OnOriginalCostInitialized?.Invoke(this);

#if DEBUG
            Log.Debug($"Initialized cost of {name}: {CostType} ({Cost})");
#endif
        }

        void OnEnable()
        {
            if (IsInitialized)
            {
                OnOriginalCostInitialized?.Invoke(this);
                InstanceTracker.Add(this);
            }
        }

        void OnDisable()
        {
            InstanceTracker.Remove(this);
        }
    }
}
