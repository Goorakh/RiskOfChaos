using R2API.Networking.Interfaces;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.Networking
{
    public sealed class PickupNotificationMessage : INetMessage
    {
        public CharacterMaster CharacterMaster;

        public PickupIndex PickupIndex;

        public bool DisplayPushNotification = true;

        public bool PlaySound = true;

        uint _pickupQuantity;

        public PickupNotificationMessage()
        {
        }

        public PickupNotificationMessage(CharacterMaster characterMaster, PickupIndex pickupIndex, bool displayPushNotification, bool playSound)
        {
            CharacterMaster = characterMaster;
            PickupIndex = pickupIndex;
            DisplayPushNotification = displayPushNotification;
            PlaySound = playSound;

            uint pickupQuantity = 1;
            if (CharacterMaster)
            {
                Inventory inventory = CharacterMaster.inventory;
                if (inventory)
                {
                    PickupDef pickupDef = PickupCatalog.GetPickupDef(PickupIndex);
                    if (pickupDef != null)
                    {
                        if (pickupDef.itemIndex != ItemIndex.None)
                        {
                            pickupQuantity = (uint)inventory.GetItemCount(pickupDef.itemIndex);
                        }
                    }
                }
            }

            _pickupQuantity = pickupQuantity;
        }

        void ISerializableObject.Serialize(NetworkWriter writer)
        {
            writer.Write(CharacterMaster ? CharacterMaster.gameObject : null);
            writer.Write(PickupIndex);
            writer.WritePackedUInt32(_pickupQuantity);

            writer.WriteBitArray([DisplayPushNotification, PlaySound]);
        }

        void ISerializableObject.Deserialize(NetworkReader reader)
        {
            GameObject masterObject = reader.ReadGameObject();
            CharacterMaster = masterObject ? masterObject.GetComponent<CharacterMaster>() : null;
            PickupIndex = reader.ReadPickupIndex();

            _pickupQuantity = reader.ReadPackedUInt32();

            bool[] flags = new bool[2];
            reader.ReadBitArray(flags);

            DisplayPushNotification = flags[0];
            PlaySound = flags[1];
        }

        void INetMessage.OnReceived()
        {
            PickupDef pickupDef = PickupCatalog.GetPickupDef(PickupIndex);

            string pickupToken = PickupCatalog.invalidPickupToken;
            Color pickupColor = PickupCatalog.invalidPickupColor;

            if (pickupDef != null)
            {
                pickupToken = pickupDef.nameToken;
                pickupColor = pickupDef.baseColor;
            }

            CharacterBody body = null;

            if (CharacterMaster)
            {
                body = CharacterMaster.GetBody();

                if (DisplayPushNotification && CharacterMaster.hasAuthority)
                {
                    CharacterMasterNotificationQueue.PushPickupNotification(CharacterMaster, PickupIndex);
                }
            }

            Chat.AddPickupMessage(body, pickupToken, pickupColor, _pickupQuantity);

            if (PlaySound && body)
            {
                Util.PlaySound("Play_UI_item_pickup", body.gameObject);
            }
        }
    }
}
