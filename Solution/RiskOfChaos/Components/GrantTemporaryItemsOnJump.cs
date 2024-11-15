using R2API.Networking;
using R2API.Networking.Interfaces;
using RiskOfChaos.Content;
using RiskOfChaos.Networking;
using RiskOfChaos.Patches;
using RiskOfChaos.Utilities;
using RoR2;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.Components
{
    public sealed class GrantTemporaryItemsOnJump : NetworkBehaviour
    {
        public ConditionalItem[] Items;

        public JumpVolume JumpVolume;

        GameObject _lastJumpedObject;
        float _sameObjectJumpCooldownTimer;

        void Awake()
        {
            if (!JumpVolume)
            {
                JumpVolume = GetComponent<JumpVolume>();
            }
        }

        void OnEnable()
        {
            JumpVolumeHooks.OnJumpVolumeJumpAuthority += onJumpVolumeJumpAuthority;
        }

        void OnDisable()
        {
            JumpVolumeHooks.OnJumpVolumeJumpAuthority -= onJumpVolumeJumpAuthority;
        }

        void FixedUpdate()
        {
            if (_sameObjectJumpCooldownTimer > 0f)
            {
                _sameObjectJumpCooldownTimer -= Time.fixedDeltaTime;
            }
        }

        void onJumpVolumeJumpAuthority(JumpVolume jumpVolume, CharacterMotor jumpingCharacterMotor)
        {
            if (jumpVolume != JumpVolume)
                return;

            GameObject jumpedObject = jumpingCharacterMotor.gameObject;
            if (jumpedObject == _lastJumpedObject && _sameObjectJumpCooldownTimer > 0f)
                return;

            // A Command can't be used here since the client doesn't have authority on the jumpvolume object
            // This is the only workaround as far as I'm aware :/
            if (NetworkServer.active)
            {
                TryGiveItemsTo(jumpedObject);
            }
            else
            {
                new GrantTemporaryItemsOnJumpMessage(this, jumpedObject).Send(NetworkDestination.Server);
            }

            _sameObjectJumpCooldownTimer = 1f;
            _lastJumpedObject = jumpedObject;
        }

        [Server]
        public void TryGiveItemsTo(GameObject bodyObject)
        {
            if (!bodyObject)
                return;

            CharacterBody body = bodyObject.GetComponent<CharacterBody>();
            if (!body)
                return;

            CharacterMotor characterMotor = body.characterMotor;
            if (!characterMotor)
                return;

            Inventory inventory = body.inventory;
            if (!inventory)
                return;

            List<ItemDef> removeOnLandItems = new List<ItemDef>(Items.Length);

            foreach (ConditionalItem item in Items)
            {
                if (item.IgnoreIfItemAlreadyPresent && inventory.GetItemCount(item.ItemDef) > 0)
                    continue;

                if ((item.GrantToPlayers && body.isPlayerControlled) ||
                    (item.GrantToInvincibleLemurian && inventory.GetItemCount(RoCContent.Items.InvincibleLemurianMarker) > 0))
                {
                    inventory.GiveItem(item.ItemDef);

                    removeOnLandItems.Add(item.ItemDef);

#if DEBUG
                    Log.Debug($"Gave jump item {FormatUtils.GetBestItemDisplayName(item.ItemDef)} to {FormatUtils.GetBestBodyName(body)}");
#endif
                }
            }

            if (removeOnLandItems.Count > 0)
            {
                IgnoreItemTransformations.IgnoreTransformationsFor(inventory);

                void onHitGroundServer(CharacterBody characterBody, in CharacterMotor.HitGroundInfo hitGroundInfo)
                {
                    if (!body || characterBody == body)
                    {
                        OnCharacterHitGroundServerHook.OnCharacterHitGround -= onHitGroundServer;

                        if (characterBody)
                        {
                            Inventory inventory = characterBody.inventory;
                            if (inventory)
                            {
                                foreach (ItemDef item in removeOnLandItems)
                                {
                                    inventory.RemoveItem(item);
                                }

                                IgnoreItemTransformations.ResumeTransformationsFor(inventory);
                            }
                        }
                    }
                }

                OnCharacterHitGroundServerHook.OnCharacterHitGround += onHitGroundServer;
            }
        }

        [Serializable]
        public struct ConditionalItem
        {
            public ItemDef ItemDef;

            public bool GrantToPlayers;

            public bool GrantToInvincibleLemurian;

            public bool IgnoreIfItemAlreadyPresent;
        }
    }
}
