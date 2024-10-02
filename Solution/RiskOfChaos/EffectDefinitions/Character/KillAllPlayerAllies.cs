using RiskOfChaos.EffectHandling;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.Utilities;
using RiskOfChaos.Utilities.Extensions;
using RoR2;
using System.Collections.Generic;
using System.Linq;

namespace RiskOfChaos.EffectDefinitions.Character
{
    [ChaosEffect("kill_all_allies", DefaultSelectionWeight = 0.5f)]
    public sealed class KillAllPlayerAllies : BaseEffect
    {
        static IEnumerable<HealthComponent> getCharactersToKill()
        {
            for (int i = CharacterMaster.readOnlyInstancesList.Count - 1; i >= 0; i--)
            {
                CharacterMaster master = CharacterMaster.readOnlyInstancesList[i];
                if (!master || master.isBoss || master.playerCharacterMasterController)
                    continue;

                CharacterBody body = master.GetBody();
                if (!body)
                    continue;

                switch (body.teamComponent.teamIndex)
                {
                    case TeamIndex.Player:
                        HealthComponent healthComponent = body.healthComponent;
                        if (healthComponent)
                        {
                            yield return healthComponent;
                        }

                        break;
                }
            }

            foreach (CharacterMaster player in PlayerUtils.GetAllPlayerMasters(false))
            {
                MinionOwnership minionOwnership = player.minionOwnership;
                if (!minionOwnership)
                    continue;

                MinionOwnership.MinionGroup minionGroup = minionOwnership.group;
                if (minionGroup == null)
                    continue;

                for (int i = 0; i < minionGroup.memberCount; i++)
                {
                    MinionOwnership minion = minionGroup.members[i];
                    if (!minion)
                        continue;

                    CharacterMaster minionMaster = minion.GetComponent<CharacterMaster>();
                    if (!minionMaster || minionMaster.teamIndex == TeamIndex.Player || minionMaster.playerCharacterMasterController)
                        continue;

                    CharacterBody minionBody = minionMaster.GetBody();
                    if (!minionBody)
                        continue;

                    yield return minionBody.healthComponent;
                }
            }
        }

        [EffectCanActivate]
        static bool CanActivate(in EffectCanActivateContext context)
        {
            return !context.IsNow || getCharactersToKill().Any();
        }

        public override void OnStart()
        {
            getCharactersToKill().TryDo(healthComponent => healthComponent.Suicide());
        }
    }
}
