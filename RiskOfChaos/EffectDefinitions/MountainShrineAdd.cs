using BepInEx.Configuration;
using RiskOfChaos.EffectHandling;
using RiskOfChaos.Utility;
using RiskOfOptions.OptionConfigs;
using RiskOfOptions.Options;
using RoR2;
using UnityEngine;

namespace RiskOfChaos.EffectDefinitions
{
    [ChaosEffect(EFFECT_ID, ConfigName = "Add Mountain Shrine")]
    public class MountainShrineAdd : BaseEffect
    {
        const string EFFECT_ID = "MountainShrineAdd";

        static string _configSectionName;

        static ConfigEntry<int> _numShrinesPerActivation;
        static int numShrinesPerActivation => _numShrinesPerActivation?.Value ?? 2;

        [SystemInitializer(typeof(ChaosEffectCatalog))]
        static void Init()
        {
            _configSectionName = ChaosEffectCatalog.GetConfigSectionName(EFFECT_ID);

            _numShrinesPerActivation = Main.Instance.Config.Bind(new ConfigDefinition(_configSectionName, "Shrines per Activation"), 2, new ConfigDescription("The amount of mountain shrines to activate each time this effect is activated"));
            ChaosEffectCatalog.AddEffectConfigOption(new IntSliderOption(_numShrinesPerActivation, new IntSliderConfig
            {
                min = 1,
                max = 10
            }));
        }

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
                subjectAsCharacterBody = PlayerUtils.GetLocalUserBody(),
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
