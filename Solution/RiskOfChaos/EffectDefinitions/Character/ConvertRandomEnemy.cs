using Grumpy.UnitTest;
using HG;
using RiskOfChaos.ChatMessages;
using RiskOfChaos.Content;
using RiskOfChaos.EffectHandling;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.EffectHandling.EffectComponents;
using RiskOfChaos.Utilities.Extensions;
using RoR2;
using RoR2.CharacterAI;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectDefinitions.Character
{
    [ChaosEffect("convert_random_enemy")]
    public sealed class ConvertRandomEnemy : NetworkBehaviour
    {
        static readonly Color32 _bossSubjectColor = new Color32(0xFF, 0x2C, 0x2C, 0xFF);
        static readonly Color32 _defaultSubjectColor = new Color32(0xFF, 0xFF, 0xFF, 0xFF);

        static bool canConvert(CharacterBody body)
        {
            if (!body || !body.teamComponent || body.isPlayerControlled)
                return false;

            switch (body.teamComponent.teamIndex)
            {
                case TeamIndex.Monster:
                case TeamIndex.Lunar:
                case TeamIndex.Void:
                    return true;
                default:
                    return false;
            }
        }

        static void getAllConvertableEnemies(List<CharacterBody> dest)
        {
            dest.EnsureCapacity(CharacterBody.readOnlyInstancesList.Count);

            foreach (CharacterBody body in CharacterBody.readOnlyInstancesList)
            {
                if (canConvert(body))
                {
                    dest.Add(body);
                }
            }
        }

        [EffectCanActivate]
        static bool CanActivate(in EffectCanActivateContext context)
        {
            return !context.IsNow || CharacterBody.readOnlyInstancesList.Any(canConvert);
        }

        ChaosEffectComponent _effectComponent;

        CharacterBody _enemyToConvert;

        void Awake()
        {
            _effectComponent = GetComponent<ChaosEffectComponent>();
        }

        public override void OnStartServer()
        {
            base.OnStartServer();

            Xoroshiro128Plus rng = new Xoroshiro128Plus(_effectComponent.Rng.nextUlong);

            Span<BodyIndex> convertOrder = stackalloc BodyIndex[BodyCatalog.bodyCount];
            for (int i = 0; i < BodyCatalog.bodyCount; i++)
            {
                convertOrder[i] = (BodyIndex)i;
            }

            Util.ShuffleSpan(convertOrder, rng);

            using (ListPool<CharacterBody>.RentCollection(out List<CharacterBody> allConvertableEnemies))
            {
                getAllConvertableEnemies(allConvertableEnemies);

                foreach (BodyIndex bodyIndex in convertOrder)
                {
                    IReadOnlyList<CharacterBody> availableBodies = [.. allConvertableEnemies.Where(b => b.bodyIndex == bodyIndex)];
                    if (availableBodies.Count > 0)
                    {
                        _enemyToConvert = rng.NextElementUniform(availableBodies);
                        break;
                    }
                }
            }

            if (!_enemyToConvert)
            {
                Log.Error("No available enemy to convert");
            }
        }

        void Start()
        {
            if (!NetworkServer.active)
                return;

            if (!_enemyToConvert)
            {
                Log.Warning("No enemy object reference, nothing to do");
                return;
            }

            _enemyToConvert.teamComponent.teamIndex = TeamIndex.Player;

            CharacterMaster master = _enemyToConvert.master;
            if (master)
            {
                master.teamIndex = TeamIndex.Player;

                foreach (BaseAI ai in master.AiComponents)
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

                if (master.inventory.GetItemCountPermanent(RoCContent.Items.MinAllyRegen) == 0)
                {
                    master.inventory.GiveItemPermanent(RoCContent.Items.MinAllyRegen);
                }
            }

            ChatMessageBase announcementMessage = getEnemyConvertedAnnouncementChatMessage(_enemyToConvert);
            if (announcementMessage != null)
            {
                Chat.SendBroadcastChat(announcementMessage);
            }
        }

        static ChatMessageBase getEnemyConvertedAnnouncementChatMessage(CharacterBody convertedBody)
        {
            if (convertedBody.inventory && convertedBody.inventory.GetItemCountPermanent(RoCContent.Items.InvincibleLemurianMarker) > 0)
            {
                return new Chat.SimpleChatMessage
                {
                    baseToken = "INVINCIBLE_LEMURIAN_RECRUIT_MESSAGE"
                };
            }

            Color32 subjectNameColor = _defaultSubjectColor;
            if (convertedBody.isBoss)
            {
                subjectNameColor = _bossSubjectColor;
            }

            return new BestNameSubjectChatMessage
            {
                BaseToken = "RECRUIT_ENEMY_MESSAGE",
                SubjectAsCharacterBody = convertedBody,
                SubjectNameOverrideColor = subjectNameColor
            };
        }
    }
}
