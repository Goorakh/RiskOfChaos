using R2API.Networking.Interfaces;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.Networking
{
    public sealed class PickupTransformationNotificationMessage : INetMessage
    {
        CharacterMaster _master;
        PickupIndex _fromPickupIndex;
        PickupIndex _toPickupIndex;
        CharacterMasterNotificationQueue.TransformationType _transformationType;

        public PickupTransformationNotificationMessage()
        {
        }

        public PickupTransformationNotificationMessage(CharacterMaster characterMaster, PickupIndex fromPickupIndex, PickupIndex toPickupIndex, CharacterMasterNotificationQueue.TransformationType transformationType)
        {
            _master = characterMaster;
            _fromPickupIndex = fromPickupIndex;
            _toPickupIndex = toPickupIndex;
            _transformationType = transformationType;
        }

        void ISerializableObject.Serialize(NetworkWriter writer)
        {
            writer.Write(_master.gameObject);
            writer.Write(_fromPickupIndex);
            writer.Write(_toPickupIndex);
            writer.WritePackedUInt32((uint)_transformationType);
        }

        void ISerializableObject.Deserialize(NetworkReader reader)
        {
            GameObject masterObject = reader.ReadGameObject();
            _master = masterObject ? masterObject.GetComponent<CharacterMaster>() : null;

            _fromPickupIndex = reader.ReadPickupIndex();
            _toPickupIndex = reader.ReadPickupIndex();

            _transformationType = (CharacterMasterNotificationQueue.TransformationType)reader.ReadPackedUInt32();
        }

        void INetMessage.OnReceived()
        {
            if (!_master || !_master.hasAuthority)
                return;

            if (!_fromPickupIndex.isValid || !_toPickupIndex.isValid)
                return;

            CharacterMasterNotificationQueue notificationQueue = CharacterMasterNotificationQueue.GetNotificationQueueForMaster(_master);
            if (!notificationQueue)
                return;

            CharacterMasterNotificationQueue.TransformationInfo transformationInfo = new CharacterMasterNotificationQueue.TransformationInfo(_transformationType, PickupCatalog.GetPickupDef(_fromPickupIndex));

            CharacterMasterNotificationQueue.NotificationInfo notificationInfo = new CharacterMasterNotificationQueue.NotificationInfo(PickupCatalog.GetPickupDef(_toPickupIndex), transformationInfo);

            notificationQueue.PushNotification(notificationInfo, CharacterMasterNotificationQueue.firstNotificationLengthSeconds);
        }
    }
}
