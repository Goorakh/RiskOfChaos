using R2API.Networking.Interfaces;
using RiskOfChaos.ChatMessages;
using RoR2;
using System;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.Networking
{
    public sealed class PickupsNotificationMessage : INetMessage
    {
        public CharacterMaster CharacterMaster;

        public PickupIndex[] PickupIndices;

        public bool DisplayPushNotification = true;

        public bool PlaySound = true;

        uint[] _pickupQuantities;

        public PickupsNotificationMessage()
        {
        }

        public PickupsNotificationMessage(CharacterMaster characterMaster, PickupIndex[] pickupIndices, bool displayPushNotification, bool playSound)
        {
            if (pickupIndices == null)
                throw new ArgumentNullException(nameof(pickupIndices));

            CharacterMaster = characterMaster;
            PickupIndices = pickupIndices;
            DisplayPushNotification = displayPushNotification;
            PlaySound = playSound;

            _pickupQuantities = new uint[PickupIndices.Length];
            for (int i = 0; i < PickupIndices.Length; i++)
            {
                uint pickupQuantity = 1;
                if (CharacterMaster)
                {
                    Inventory inventory = CharacterMaster.inventory;
                    if (inventory)
                    {
                        PickupDef pickupDef = PickupCatalog.GetPickupDef(PickupIndices[i]);
                        if (pickupDef != null)
                        {
                            if (pickupDef.itemIndex != ItemIndex.None)
                            {
                                pickupQuantity = (uint)inventory.GetItemCount(pickupDef.itemIndex);
                            }
                        }
                    }
                }

                _pickupQuantities[i] = pickupQuantity;
            }
        }

        void ISerializableObject.Serialize(NetworkWriter writer)
        {
            writer.Write(CharacterMaster ? CharacterMaster.gameObject : null);

            writer.WritePackedUInt32((uint)PickupIndices.Length);

            for (int i = 0; i < PickupIndices.Length; i++)
            {
                writer.Write(PickupIndices[i]);
                writer.WritePackedUInt32(_pickupQuantities[i]);
            }

            writer.WriteBitArray([DisplayPushNotification, PlaySound]);
        }

        void ISerializableObject.Deserialize(NetworkReader reader)
        {
            GameObject masterObject = reader.ReadGameObject();
            CharacterMaster = masterObject ? masterObject.GetComponent<CharacterMaster>() : null;

            uint pickupCount = reader.ReadPackedUInt32();

            PickupIndices = new PickupIndex[pickupCount];
            _pickupQuantities = new uint[pickupCount];

            for (int i = 0; i < pickupCount; i++)
            {
                PickupIndices[i] = reader.ReadPickupIndex();
                _pickupQuantities[i] = reader.ReadPackedUInt32();
            }

            bool[] flags = new bool[2];
            reader.ReadBitArray(flags);

            DisplayPushNotification = flags[0];
            PlaySound = flags[1];
        }

        void INetMessage.OnReceived()
        {
            NetworkUser networkUser = null;
            CharacterBody body = null;

            if (CharacterMaster)
            {
                body = CharacterMaster.GetBody();

                PlayerCharacterMasterController playerMasterController = CharacterMaster.playerCharacterMasterController;
                if (playerMasterController)
                {
                    networkUser = playerMasterController.networkUser;
                }

                if (!networkUser && body)
                {
                    networkUser = Util.LookUpBodyNetworkUser(body);
                }

                if (DisplayPushNotification && CharacterMaster.hasAuthority)
                {
                    foreach (PickupIndex pickupIndex in PickupIndices)
                    {
                        CharacterMasterNotificationQueue.PushPickupNotification(CharacterMaster, pickupIndex);
                    }
                }
            }

            PlayerPickupListChatMessage pickupMessage = new PlayerPickupListChatMessage
            {
                baseToken = "PLAYER_MULTI_PICKUP",
                PickupIndices = PickupIndices,
                PickupQuantities = _pickupQuantities
            };

            if (networkUser)
            {
                pickupMessage.subjectAsNetworkUser = networkUser;
            }
            else
            {
                pickupMessage.subjectAsCharacterBody = body;
            }

            Chat.AddMessage(pickupMessage);

            if (PlaySound && body)
            {
                Util.PlaySound("Play_UI_item_pickup", body.gameObject);
            }
        }
    }
}
