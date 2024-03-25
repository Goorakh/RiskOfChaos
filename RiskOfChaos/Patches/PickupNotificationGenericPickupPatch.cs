using RoR2;
using RoR2.UI;

namespace RiskOfChaos.Patches
{
    static class PickupNotificationGenericPickupPatch
    {
        [SystemInitializer]
        static void Init()
        {
            On.RoR2.UI.NotificationUIController.SetUpNotification += NotificationUIController_SetUpNotification;
        }

        static void NotificationUIController_SetUpNotification(On.RoR2.UI.NotificationUIController.orig_SetUpNotification orig, NotificationUIController self, CharacterMasterNotificationQueue.NotificationInfo notificationInfo)
        {
            orig(self, notificationInfo);

#pragma warning disable Publicizer001 // Accessing a member that was not originally public
            GenericNotification notification = self.currentNotification;
#pragma warning restore Publicizer001 // Accessing a member that was not originally public

            if (!notification || notificationInfo is null)
                return;

            if (notificationInfo.transformation is not null)
            {
                if (notificationInfo.transformation.previousData is PickupDef fromPickup)
                {
                    setPreviousPickup(notification, fromPickup);
                }
            }

            if (notificationInfo.data is PickupDef toPickup)
            {
                setPickup(notification, toPickup);
            }
        }

        static void setPreviousPickup(GenericNotification notification, PickupDef pickup)
        {
            if (notification.previousIconImage && pickup.iconTexture)
            {
                notification.previousIconImage.texture = pickup.iconTexture;
            }
        }

        static void setPickup(GenericNotification notification, PickupDef pickup)
        {
            notification.titleText.token = pickup.nameToken;

            if (pickup.itemIndex != ItemIndex.None)
            {
                ItemDef item = ItemCatalog.GetItemDef(pickup.itemIndex);
                notification.descriptionText.token = item.pickupToken;
            }
            else if (pickup.equipmentIndex != EquipmentIndex.None)
            {
                EquipmentDef equipment = EquipmentCatalog.GetEquipmentDef(pickup.equipmentIndex);
                notification.descriptionText.token = equipment.pickupToken;
            }

            if (pickup.iconTexture)
            {
                notification.iconImage.texture = pickup.iconTexture;
            }

            notification.titleTMP.color = pickup.baseColor;
        }
    }
}
