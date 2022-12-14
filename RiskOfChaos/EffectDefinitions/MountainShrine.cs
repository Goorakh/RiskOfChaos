using RiskOfChaos.EffectHandling;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace RiskOfChaos.EffectDefinitions
{
    [ChaosEffect("MountainShrineAdd2")]
    public class MountainShrineAdd2 : BaseEffect
    {
        const int NUM_SHRINE_ADD = 2;

        [EffectCanActivate]
        static bool CanActivate()
        {
            TeleporterInteraction instance = TeleporterInteraction.instance;
            return instance && instance.activationState <= TeleporterInteraction.ActivationState.IdleToCharging;
        }

        [EffectWeightMultiplierSelector]
        static float GetWeight()
        {
            return RoCMath.CalcReductionWeight(TeleporterInteraction.instance.shrineBonusStacks, 2f);
        }

        public override void OnStart()
        {
            TeleporterInteraction tpInteraction = TeleporterInteraction.instance;

            for (int i = 0; i < NUM_SHRINE_ADD; i++)
            {
                tpInteraction.AddShrineStack();
            }

            Chat.SendBroadcastChat(new Chat.SubjectFormatChatMessage
            {
                subjectAsCharacterBody = PlayerUtils.GetLocalUserBody(),
                baseToken = "SHRINE_BOSS_USE_MESSAGE"
            });

            foreach (CharacterBody body in PlayerUtils.GetAllPlayerBodies(false))
            {
                EffectManager.SpawnEffect(LegacyResourcesAPI.Load<GameObject>("Prefabs/Effects/ShrineUseEffect"), new EffectData
                {
                    origin = body.corePosition,
                    rotation = Quaternion.identity,
                    scale = 1f,
                    color = new Color(0.7372549f, 0.90588236f, 0.94509804f)
                }, true);
            }
        }
    }
}
