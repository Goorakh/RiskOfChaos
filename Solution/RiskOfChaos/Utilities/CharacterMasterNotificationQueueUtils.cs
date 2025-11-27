using R2API.Networking;
using R2API.Networking.Interfaces;
using RiskOfChaos.Networking;
using RiskOfChaos.Utilities.Extensions;
using RoR2;
using System;
using UnityEngine.Networking;

namespace RiskOfChaos.Utilities
{
    public static class CharacterMasterNotificationQueueUtils
    {
        [SystemInitializer]
        static void Init()
        {
            InventoryExtensions.PickupTransformation.OnPickupTransformedServerGlobal += onPickupTransformedServerGlobal;
        }

        static void onPickupTransformedServerGlobal(InventoryExtensions.PickupTransformation.TryTransformResult transformResult)
        {
            CharacterMasterNotificationQueue.TransformationType transformationType = (CharacterMasterNotificationQueue.TransformationType)transformResult.TransformationType;
            if (transformationType == CharacterMasterNotificationQueue.TransformationType.None)
                return;

            if (transformResult.Inventory && transformResult.Inventory.TryGetComponent(out CharacterMaster master) && master.playerCharacterMasterController)
            {
                if (Util.HasEffectiveAuthority(master.gameObject))
                {
                    CharacterMasterNotificationQueue notificationQueue = CharacterMasterNotificationQueue.GetNotificationQueueForMaster(master);
                    if (notificationQueue)
                    {
                        CharacterMasterNotificationQueue.TransformationInfo transformationInfo = new CharacterMasterNotificationQueue.TransformationInfo(transformationType, PickupCatalog.GetPickupDef(transformResult.TakenPickup.PickupIndex));

                        CharacterMasterNotificationQueue.NotificationInfo notificationInfo = new CharacterMasterNotificationQueue.NotificationInfo(PickupCatalog.GetPickupDef(transformResult.GivenPickup.PickupIndex), transformationInfo, null, false, transformResult.GivenPickup.StackValues.temporaryStacksValue > 0);

                        notificationQueue.PushNotification(notificationInfo, CharacterMasterNotificationQueue.firstNotificationLengthSeconds);
                    }
                }
                else
                {
                    new PickupTransformationNotificationMessage(master, transformResult.TakenPickup.PickupIndex, transformResult.GivenPickup.PickupIndex, transformationType).Send(master.connectionToClient);
                }
            }
        }

        public static void SendPickupTransformNotification(CharacterMaster characterMaster, PickupIndex fromPickupIndex, PickupIndex toPickupIndex, CharacterMasterNotificationQueue.TransformationType transformationType)
        {
            if (!NetworkServer.active)
            {
                Log.Error("Called on client");
                return;
            }

            new PickupTransformationNotificationMessage(characterMaster, fromPickupIndex, toPickupIndex, transformationType).Send(NetworkDestination.Clients | NetworkDestination.Server);
        }

        public static bool IsAnyNotificationQueued(CharacterMaster viewerMaster)
        {
            if (!viewerMaster || !viewerMaster.hasAuthority)
                return false;

            CharacterMasterNotificationQueue notificationQueue = viewerMaster.GetComponent<CharacterMasterNotificationQueue>();
            return notificationQueue && notificationQueue.GetCurrentNotification() != null;
        }
    }
}
