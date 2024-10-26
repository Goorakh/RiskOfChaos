using RiskOfChaos.EffectHandling;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.Utilities.Extensions;
using RoR2;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectDefinitions.Character
{
    [ChaosEffect("kill_all_allies", DefaultSelectionWeight = 0.5f)]
    public sealed class KillAllPlayerAllies : MonoBehaviour
    {
        static IEnumerable<HealthComponent> getCharactersToKill()
        {
            for (int i = CharacterBody.readOnlyInstancesList.Count - 1; i >= 0; i--)
            {
                CharacterBody body = CharacterBody.readOnlyInstancesList[i];
                CharacterMaster master = body.master;
                HealthComponent healthComponent = body.healthComponent;
                if ((master && master.isBoss) || body.isPlayerControlled || !healthComponent)
                    continue;

                CharacterMaster ownerMaster = null;
                if (master)
                {
                    MinionOwnership minionOwnership = master.minionOwnership;
                    if (minionOwnership)
                    {
                        ownerMaster = minionOwnership.ownerMaster;
                    }
                }

                if (body.teamComponent.teamIndex == TeamIndex.Player || (ownerMaster && ownerMaster.playerCharacterMasterController))
                {
                    yield return healthComponent;
                }
            }
        }

        [EffectCanActivate]
        static bool CanActivate(in EffectCanActivateContext context)
        {
            return !context.IsNow || getCharactersToKill().Any();
        }

        void Start()
        {
            if (NetworkServer.active)
            {
                getCharactersToKill().TryDo(healthComponent => healthComponent.Suicide());
            }
        }
    }
}
