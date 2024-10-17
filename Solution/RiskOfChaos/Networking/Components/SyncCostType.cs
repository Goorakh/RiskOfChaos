using RiskOfChaos.Components.CostProviders;
using RiskOfChaos.Utilities.Extensions;
using RoR2;
using System.Runtime.CompilerServices;
using UnityEngine.Networking;

namespace RiskOfChaos.Networking.Components
{
    public sealed class SyncCostType : NetworkBehaviour
    {
        [SystemInitializer]
        static void Init()
        {
            On.RoR2.PurchaseInteraction.Awake += (orig, self) =>
            {
                orig(self);

                self.gameObject.EnsureComponent<SyncCostType>();
            };

            On.RoR2.MultiShopController.Start += (orig, self) =>
            {
                orig(self);

                self.gameObject.EnsureComponent<SyncCostType>();
            };
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

#if DEBUG
            Log.Debug($"{name} ({netId}): Cost type changed ({_costProvider.CostType}->{CostType})");
#endif

            _costProvider.CostType = CostType;
        }
    }
}
