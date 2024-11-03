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
        public string MessageToken;

        public CharacterMaster CharacterMaster;

        public PickupIndex[] PickupIndices;

        public bool DisplayPushNotification = true;

        public bool PlaySound = true;

        uint[] _pickupQuantities;

        public PickupsNotificationMessage()
        {
        }

        public PickupsNotificationMessage(CharacterMaster characterMaster, PickupIndex[] pickupIndices)
        {
            if (!characterMaster)
                throw new ArgumentNullException(nameof(characterMaster));

            if (pickupIndices == null)
                throw new ArgumentNullException(nameof(pickupIndices));

            MessageToken = "PLAYER_MULTI_PICKUP";

            CharacterMaster = characterMaster;
            PickupIndices = pickupIndices;

            Inventory inventory = CharacterMaster.inventory;

            _pickupQuantities = new uint[PickupIndices.Length];
            for (int i = 0; i < PickupIndices.Length; i++)
            {
                uint pickupQuantity = 1;
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

                _pickupQuantities[i] = pickupQuantity;
            }
        }

        public PickupsNotificationMessage(string messageToken, PickupIndex[] pickupIndices, uint[] pickupQuantities)
        {
            if (string.IsNullOrWhiteSpace(messageToken))
                throw new ArgumentException($"'{nameof(messageToken)}' cannot be null or whitespace.", nameof(messageToken));

            if (pickupIndices is null)
                throw new ArgumentNullException(nameof(pickupIndices));

            if (pickupQuantities is null)
                throw new ArgumentNullException(nameof(pickupQuantities));

            if (pickupIndices.Length != pickupQuantities.Length)
                throw new ArgumentException("Pickup indices and quantities count must match");

            MessageToken = messageToken;
            PickupIndices = pickupIndices;
            _pickupQuantities = pickupQuantities;
        }

        void ISerializableObject.Serialize(NetworkWriter writer)
        {
            writer.Write(MessageToken);

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
            MessageToken = reader.ReadString();

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

            SubjectPickupListChatMessage pickupMessage = new SubjectPickupListChatMessage
            {
                baseToken = MessageToken,
                PickupIndices = PickupIndices,
                PickupQuantities = _pickupQuantities
            };

            if (networkUser)
            {
                pickupMessage.subjectAsNetworkUser = networkUser;
            }
            else if (body)
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
