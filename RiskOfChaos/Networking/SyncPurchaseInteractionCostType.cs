using R2API.Networking;
using R2API.Networking.Interfaces;
using RoR2;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.Networking
{
    public sealed class SyncPurchaseInteractionCostType : INetMessage
    {
        NetworkInstanceId _purchaseInteractionNetIdentity;
        CostTypeIndex _costType;

        public SyncPurchaseInteractionCostType()
        {
        }

        public SyncPurchaseInteractionCostType(NetworkInstanceId purchaseInteractionNetId, CostTypeIndex costType)
        {
            _purchaseInteractionNetIdentity = purchaseInteractionNetId;
            _costType = costType;
        }

        public static void SetCostTypeNetworked(PurchaseInteraction purchaseInteraction, CostTypeIndex costType)
        {
            if (purchaseInteraction.costType == costType)
                return;

            NetworkIdentity networkIdentity = purchaseInteraction.GetComponent<NetworkIdentity>();
            if (!networkIdentity)
                return;

            static IEnumerator waitForNetInitThenSendMessage(NetworkIdentity networkIdentity, CostTypeIndex costType)
            {
                while (networkIdentity && networkIdentity.netId.IsEmpty())
                {
                    yield return 0;
                }

                if (!networkIdentity)
                    yield break;

                new SyncPurchaseInteractionCostType(networkIdentity.netId, costType).Send(NetworkDestination.Clients | NetworkDestination.Server);
            }

            Main.Instance.StartCoroutine(waitForNetInitThenSendMessage(networkIdentity, costType));
        }

        void ISerializableObject.Serialize(NetworkWriter writer)
        {
            writer.Write(_purchaseInteractionNetIdentity);
            writer.WritePackedUInt32((uint)_costType);
        }

        void ISerializableObject.Deserialize(NetworkReader reader)
        {
            _purchaseInteractionNetIdentity = reader.ReadNetworkId();
            _costType = (CostTypeIndex)reader.ReadPackedUInt32();
        }

        void INetMessage.OnReceived()
        {
            static IEnumerator waitForNetInitThenSetCostType(NetworkInstanceId netId, CostTypeIndex costType)
            {
                GameObject resolvedObject;
                while (!(resolvedObject = NetworkServer.active ? NetworkServer.FindLocalObject(netId)
                                                               : ClientScene.FindLocalObject(netId)))
                {
                    yield return 0;
                }

#if DEBUG
                Log.Debug($"Resolved net object {resolvedObject} with id {netId}");
#endif

                if (resolvedObject.TryGetComponent(out PurchaseInteraction purchaseInteraction))
                {
                    purchaseInteraction.costType = costType;
                }
            }

            Main.Instance.StartCoroutine(waitForNetInitThenSetCostType(_purchaseInteractionNetIdentity, _costType));
        }
    }
}
