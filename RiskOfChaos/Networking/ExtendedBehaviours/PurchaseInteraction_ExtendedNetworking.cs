using RoR2;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;

namespace RiskOfChaos.Networking.ExtendedBehaviours
{
    [RequireComponent(typeof(PurchaseInteraction))]
    sealed class PurchaseInteraction_ExtendedNetworking : NetworkBehaviour
    {
        PurchaseInteraction _purchaseInteraction;

        const uint COST_TYPE_DIRTY_BIT = 1 << 0;

        CostTypeIndex _lastCostType;

        CostTypeIndex costType
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return _purchaseInteraction.costType;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                _purchaseInteraction.costType = value;

#if DEBUG
                Log.Debug($"{name} ({netId}): Set CostType to {value}");
#endif
            }
        }

        void Awake()
        {
            _purchaseInteraction = GetComponent<PurchaseInteraction>();
            _lastCostType = costType;
        }

        void FixedUpdate()
        {
            if (hasAuthority && _lastCostType != costType)
            {
#if DEBUG
                Log.Debug($"{name} ({netId}): Cost type changed ({_lastCostType}->{costType}), setting dirty bit");
#endif

                SetDirtyBit(COST_TYPE_DIRTY_BIT);
                _lastCostType = costType;
            }
        }

        public override bool OnSerialize(NetworkWriter writer, bool initialState)
        {
            if (initialState)
            {
                writer.WritePackedUInt32((uint)costType);
                return true;
            }

            uint dirtyBits = syncVarDirtyBits;
            writer.WritePackedUInt32(dirtyBits);

            bool anythingWritten = false;
            if ((dirtyBits & COST_TYPE_DIRTY_BIT) != 0)
            {
                writer.WritePackedUInt32((uint)costType);
                anythingWritten |= true;
            }

            return anythingWritten;
        }

        public override void OnDeserialize(NetworkReader reader, bool initialState)
        {
            if (initialState)
            {
                costType = (CostTypeIndex)reader.ReadPackedUInt32();
                return;
            }

            uint dirtyBits = reader.ReadPackedUInt32();
            if ((dirtyBits & COST_TYPE_DIRTY_BIT) != 0)
            {
                costType = (CostTypeIndex)reader.ReadPackedUInt32();
            }
        }

        [SystemInitializer]
        static void Init()
        {
            On.RoR2.PurchaseInteraction.Awake += (orig, self) =>
            {
                orig(self);

                if (!self.GetComponent<PurchaseInteraction_ExtendedNetworking>())
                {
                    self.gameObject.AddComponent<PurchaseInteraction_ExtendedNetworking>();
#if DEBUG
                    Log.Debug($"Added component to {self}");
#endif
                }
            };
        }
    }
}
