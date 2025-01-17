using RiskOfChaos.Content;
using RiskOfChaos.EffectHandling;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.Utilities;
using RiskOfChaos.Utilities.Extensions;
using RoR2;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectDefinitions.Character
{
    [ChaosEffect("kill_all", DefaultSelectionWeight = 0.7f)]
    public sealed class KillAll : MonoBehaviour
    {
        static bool canKillCharacter(CharacterBody body)
        {
            if (body.IsPlayerOrPlayerAlly())
                return false;

            if (body.master && body.master.isBoss)
                return false;

            return true;
        }

        [EffectCanActivate]
        static bool CanActivate(in EffectCanActivateContext context)
        {
            if (!context.IsNow)
                return true;

            foreach (CharacterBody body in CharacterBody.readOnlyInstancesList)
            {
                if (canKillCharacter(body))
                {
                    return true;
                }
            }

            return false;
        }

        void Start()
        {
            if (!NetworkServer.active)
                return;

            List<CharacterBody> charactersToKill = new List<CharacterBody>(CharacterBody.readOnlyInstancesList.Count);

            foreach (CharacterBody body in CharacterBody.readOnlyInstancesList)
            {
                if (canKillCharacter(body))
                {
                    charactersToKill.Add(body);
                }
            }

            bool hasSentInvincibleLemurianKillFailMessage = false;

            foreach (CharacterBody body in charactersToKill)
            {
                Inventory inventory = body.inventory;
                if (inventory)
                {
                    if (inventory.GetItemCount(RoCContent.Items.InvincibleLemurianMarker) > 0)
                    {
                        if (!hasSentInvincibleLemurianKillFailMessage)
                        {
                            Chat.SendBroadcastChat(new Chat.SimpleChatMessage
                            {
                                baseToken = "INVINCIBLE_LEMURIAN_KILL_FAIL_MESSAGE"
                            });

                            hasSentInvincibleLemurianKillFailMessage = true;
                        }

                        continue;
                    }
                }

                if (body.healthComponent)
                {
                    try
                    {
                        body.healthComponent.Suicide();
                    }
                    catch (Exception e)
                    {
                        Log.Error_NoCallerPrefix($"Failed to kill {FormatUtils.GetBestBodyName(body)}: {e}");
                    }
                }
            }
        }
    }
}
