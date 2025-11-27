using HG;
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
    [ChaosEffect("kill_all_allies", DefaultSelectionWeight = 0.5f)]
    public sealed class KillAllPlayerAllies : MonoBehaviour
    {
        static bool canKillCharacter(CharacterBody body)
        {
            if (body.isPlayerControlled || !body.IsPlayerOrPlayerAlly())
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

            using (ListPool<CharacterBody>.RentCollection(out List<CharacterBody> charactersToKill))
            {
                charactersToKill.EnsureCapacity(CharacterBody.readOnlyInstancesList.Count);

                foreach (CharacterBody body in CharacterBody.readOnlyInstancesList)
                {
                    if (canKillCharacter(body))
                    {
                        charactersToKill.Add(body);
                    }
                }

                foreach (CharacterBody body in charactersToKill)
                {
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
}
