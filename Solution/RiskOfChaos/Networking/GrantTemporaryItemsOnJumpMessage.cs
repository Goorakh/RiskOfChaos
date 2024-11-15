using R2API.Networking.Interfaces;
using RiskOfChaos.Components;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.Networking
{
    public sealed class GrantTemporaryItemsOnJumpMessage : INetMessage
    {
        GameObject _itemGranterObject;
        GameObject _jumpedBodyObject;

        public GrantTemporaryItemsOnJumpMessage(GrantTemporaryItemsOnJump itemGranter, GameObject jumpedBodyObject)
        {
            _itemGranterObject = itemGranter.gameObject;
            _jumpedBodyObject = jumpedBodyObject;
        }

        public GrantTemporaryItemsOnJumpMessage()
        {
        }

        public void Serialize(NetworkWriter writer)
        {
            writer.Write(_itemGranterObject);
            writer.Write(_jumpedBodyObject);
        }

        public void Deserialize(NetworkReader reader)
        {
            _itemGranterObject = reader.ReadGameObject();
            _jumpedBodyObject = reader.ReadGameObject();
        }

        public void OnReceived()
        {
            if (_itemGranterObject && _itemGranterObject.TryGetComponent(out GrantTemporaryItemsOnJump itemGranter))
            {
                itemGranter.TryGiveItemsTo(_jumpedBodyObject);
            }
        }
    }
}
