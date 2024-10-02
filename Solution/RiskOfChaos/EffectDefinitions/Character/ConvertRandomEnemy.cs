using RiskOfChaos.ChatMessages;
using RiskOfChaos.Content;
using RiskOfChaos.EffectHandling;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.SaveHandling;
using RiskOfChaos.Utilities.Extensions;
using RoR2;
using RoR2.CharacterAI;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectDefinitions.Character
{
    [ChaosEffect("convert_random_enemy", IsNetworked = true)]
    public sealed class ConvertRandomEnemy : BaseEffect
    {
        static IEnumerable<CharacterBody> getAllConvertableEnemies()
        {
            return CharacterBody.readOnlyInstancesList.Where(c =>
            {
                if (!c || !c.teamComponent || c.isPlayerControlled)
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

            Util.ShuffleArray(convertOrder, RNG.Branch());

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

        public override void Serialize(NetworkWriter writer)
        {
            base.Serialize(writer);

            writer.Write(_enemyToConvert && !SaveManager.IsCollectingSaveData ? _enemyToConvert.networkIdentity : null);
        }

        public override void Deserialize(NetworkReader reader)
        {
            base.Deserialize(reader);

            NetworkIdentity enemyToConvertNetId = reader.ReadNetworkIdentity();
            _enemyToConvert = enemyToConvertNetId ? enemyToConvertNetId.GetComponent<CharacterBody>() : null;

#if DEBUG
            Log.Debug($"Deserialized target object: {_enemyToConvert}");
#endif
        }

        public override void OnStart()
        {
            if (!_enemyToConvert)
                return;

            GameObject positionIndicator = _enemyToConvert.teamComponent.indicator;

            if (positionIndicator)
            {
#if DEBUG
                Log.Debug($"Destroying old position indicator: {positionIndicator}");
#endif

                GameObject.Destroy(positionIndicator);
            }

            if (NetworkServer.active)
            {
                bool isBoss = _enemyToConvert.isBoss;

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

                    foreach (CombatSquad combatSquad in InstanceTracker.GetInstancesList<CombatSquad>())
                    {
                        if (combatSquad.ContainsMember(master))
                        {
                            combatSquad.RemoveMember(master);
                        }
                    }

                    master.gameObject.SetDontDestroyOnLoad(true);

                    master.inventory.GiveItem(Items.MinAllyRegen);
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
                    Chat.SendBroadcastChat(new BestNameSubjectChatMessage
                    {
                        BaseToken = isBoss ? "RECRUIT_BOSS_MESSAGE" : "RECRUIT_ENEMY_MESSAGE",
                        SubjectAsCharacterBody = _enemyToConvert
                    });
                }
            }
        }
    }
}
