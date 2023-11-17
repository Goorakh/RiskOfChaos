using RiskOfChaos.Content;
using RiskOfChaos.EffectHandling;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.Utilities.Extensions;
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

        CharacterBody _enemyToConvert;

        public override void OnPreStartServer()
        {
            base.OnPreStartServer();

            BodyIndex[] convertOrder = Enumerable.Range(0, BodyCatalog.bodyCount)
                                                 .Cast<BodyIndex>()
                                                 .ToArray();

            Util.ShuffleArray(convertOrder, new Xoroshiro128Plus(RNG.nextUlong));

#if DEBUG
            Log.Debug($"Convert order: [{string.Join(", ", convertOrder.Select(BodyCatalog.GetBodyName))}]");
#endif

            CharacterBody[] allConvertableEnemies = getAllConvertableEnemies().ToArray();
            foreach (BodyIndex bodyIndex in convertOrder)
            {
                CharacterBody[] availableBodies = allConvertableEnemies.Where(b => b.bodyIndex == bodyIndex).ToArray();
                if (availableBodies.Length > 0)
                {
                    _enemyToConvert = RNG.NextElementUniform(availableBodies);
                    return;
                }
            }

            Log.Error("No available enemy to convert");
        }

        public override void OnStart()
        {
            if (!_enemyToConvert)
                return;

            // TODO: This is not networked, clients will still see the old indicator if one was present before the effect
#pragma warning disable Publicizer001 // Accessing a member that was not originally public
            GameObject positionIndicator = _enemyToConvert.teamComponent.indicator;
#pragma warning restore Publicizer001 // Accessing a member that was not originally public
            if (positionIndicator)
            {
                GameObject.Destroy(positionIndicator);
            }

            _enemyToConvert.teamComponent.teamIndex = TeamIndex.Player;

            CharacterMaster master = _enemyToConvert.master;
            if (master)
            {
                master.teamIndex = TeamIndex.Player;

                if (master.TryGetComponent(out BaseAI ai))
                {
                    ai.enemyAttention = 0f;
                    ai.currentEnemy.Reset();
                    ai.ForceAcquireNearestEnemyIfNoCurrentEnemy();
                }

                BossGroup bossGroup = BossGroup.FindBossGroup(_enemyToConvert);
                if (bossGroup)
                {
#pragma warning disable Publicizer001 // Accessing a member that was not originally public
                    bossGroup.combatSquad.RemoveMember(master);
#pragma warning restore Publicizer001 // Accessing a member that was not originally public
                }

                master.gameObject.SetDontDestroyOnLoad(true);
            }

            if (master && master.inventory.GetItemCount(Items.InvincibleLemurianMarker) > 0)
            {
                Chat.SendBroadcastChat(new Chat.SimpleChatMessage
                {
                    baseToken = "INVINCIBLE_LEMURIAN_RECRUIT_MESSAGE"
                });
            }
            else
            {
                Chat.SendBroadcastChat(new SubjectChatMessage
                {
                    baseToken = "RECRUIT_ENEMY_MESSAGE",
                    subjectAsCharacterBody = _enemyToConvert
                });
            }
        }
    }
}
