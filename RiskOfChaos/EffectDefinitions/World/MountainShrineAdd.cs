using RiskOfChaos.Components;
using RiskOfChaos.ConfigHandling;
using RiskOfChaos.EffectHandling;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Data;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.Utilities;
using RiskOfOptions.OptionConfigs;
using RoR2;
using UnityEngine;

namespace RiskOfChaos.EffectDefinitions.World
{
    [ChaosEffect("mountain_shrine_add", ConfigName = "Add Mountain Shrine", EffectWeightReductionPercentagePerActivation = 20f)]
    public sealed class MountainShrineAdd : BaseEffect
    {
        [EffectConfig]
        static readonly ConfigHolder<int> _numShrinesPerActivation =
            ConfigFactory<int>.CreateConfig("Shrines per Activation", 2)
                              .Description("The amount of mountain shrines to activate each time this effect is activated")
                              .OptionConfig(new IntSliderConfig
                              {
                                  min = 1,
                                  max = 10
                              })
                              .ValueConstrictor(ValueConstrictors.GreaterThanOrEqualTo(1))
                              .Build();

        [EffectCanActivate]
        static bool CanActivate(EffectCanActivateContext context)
        {
            TeleporterInteraction instance = TeleporterInteraction.instance;
            return instance && (!context.IsNow || instance.activationState <= TeleporterInteraction.ActivationState.IdleToCharging);
        }

        [EffectNameFormatArgs]
        static object[] GetEffectNameFormatArgs()
        {
            return new object[] { _numShrinesPerActivation.Value };
        }

        public override void OnStart()
        {
            TeleporterInteraction tpInteraction = TeleporterInteraction.instance;

            for (int i = _numShrinesPerActivation.Value - 1; i >= 0; i--)
            {
                tpInteraction.AddShrineStack();
            }

            Chat.SendBroadcastChat(new Chat.SubjectFormatChatMessage
            {
                subjectAsCharacterBody = ChaosInteractor.GetBody(),
                baseToken = "SHRINE_BOSS_USE_MESSAGE"
            });

            foreach (CharacterBody body in PlayerUtils.GetAllPlayerBodies(true))
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
