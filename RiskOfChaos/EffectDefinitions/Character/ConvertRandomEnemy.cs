using RiskOfChaos.EffectHandling;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RoR2;
using RoR2.CharacterAI;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RiskOfChaos.EffectDefinitions.Character
{
    [ChaosEffect("convert_random_enemy")]
    public sealed class ConvertRandomEnemy : BaseEffect
    {
        static IEnumerable<CharacterBody> getAllConvertableEnemies()
        {
            return CharacterBody.readOnlyInstancesList.Where(c =>
            {
                if (!c || !c.teamComponent)
                    return false;

                switch (c.teamComponent.teamIndex)
                {
                    case TeamIndex.Monster:
                    case TeamIndex.Lunar:
                    case TeamIndex.Void:
                        return true;
                    default:
                        return false;
                }
            });
        }

        [EffectCanActivate]
        static bool CanActivate(in EffectCanActivateContext context)
        {
            return !context.IsNow || getAllConvertableEnemies().Any();
        }

        public override void OnStart()
        {
            CharacterBody body = RNG.NextElementUniform(getAllConvertableEnemies().ToArray());

            // TODO: This is not networked, clients will still see the old indicator if one was present before the effect
#pragma warning disable Publicizer001 // Accessing a member that was not originally public
            GameObject positionIndicator = body.teamComponent.indicator;
#pragma warning restore Publicizer001 // Accessing a member that was not originally public
            if (positionIndicator)
            {
                GameObject.Destroy(positionIndicator);
            }

            body.teamComponent.teamIndex = TeamIndex.Player;

            CharacterMaster master = body.master;
            if (master)
            {
                master.teamIndex = TeamIndex.Player;

                if (master.TryGetComponent(out BaseAI ai))
                {
                    ai.enemyAttention = 0f;
                    ai.currentEnemy.Reset();
                    ai.ForceAcquireNearestEnemyIfNoCurrentEnemy();
                }

                BossGroup bossGroup = BossGroup.FindBossGroup(body);
                if (bossGroup)
                {
#pragma warning disable Publicizer001 // Accessing a member that was not originally public
                    bossGroup.combatSquad.RemoveMember(master);
#pragma warning restore Publicizer001 // Accessing a member that was not originally public
                }
            }
        }
    }
}
