using BepInEx.Configuration;
using RiskOfChaos.Components;
using RiskOfChaos.EffectHandling;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Data;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.Utilities;
using RiskOfOptions.OptionConfigs;
using RiskOfOptions.Options;
using RoR2;
using UnityEngine;

namespace RiskOfChaos.EffectDefinitions.World
{
    [ChaosEffect("mountain_shrine_add", ConfigName = "Add Mountain Shrine", EffectWeightReductionPercentagePerActivation = 20f)]
    public sealed class MountainShrineAdd : BaseEffect
    {
        [InitEffectInfo]
        static readonly ChaosEffectInfo _effectInfo;

        static ConfigEntry<int> _numShrinesPerActivation;
        static int numShrinesPerActivation => Mathf.Max(1, _numShrinesPerActivation?.Value ?? 2);

        [SystemInitializer(typeof(ChaosEffectCatalog))]
        static void Init()
        {
            _numShrinesPerActivation = _effectInfo.BindConfig("Shrines per Activation", 2, new ConfigDescription("The amount of mountain shrines to activate each time this effect is activated"));
            addConfigOption(new IntSliderOption(_numShrinesPerActivation, new IntSliderConfig
            {
                min = 1,
                max = 10
            }));
        }

        [EffectCanActivate]
        static bool CanActivate(EffectCanActivateContext context)
        {
            TeleporterInteraction instance = TeleporterInteraction.instance;
            return instance && (!context.IsNow || instance.activationState <= TeleporterInteraction.ActivationState.IdleToCharging);
        }

        [EffectNameFormatArgs]
        static object[] GetEffectNameFormatArgs()
        {
            return new object[] { numShrinesPerActivation };
        }

        public override void OnStart()
        {
            TeleporterInteraction tpInteraction = TeleporterInteraction.instance;

            for (int i = 0; i < numShrinesPerActivation; i++)
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
