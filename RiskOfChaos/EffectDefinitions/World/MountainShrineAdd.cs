using BepInEx.Configuration;
using RiskOfChaos.EffectHandling;
using RiskOfChaos.Utilities;
using RiskOfOptions.OptionConfigs;
using RiskOfOptions.Options;
using RoR2;
using UnityEngine;

namespace RiskOfChaos.EffectDefinitions.World
{
    [ChaosEffect(EFFECT_ID, ConfigName = "Add Mountain Shrine", EffectWeightReductionPercentagePerActivation = 20f)]
    public class MountainShrineAdd : BaseEffect
    {
        const string EFFECT_ID = "MountainShrineAdd";

        static ConfigEntry<int> _numShrinesPerActivation;
        static int numShrinesPerActivation => _numShrinesPerActivation?.Value ?? 2;

        [SystemInitializer(typeof(ChaosEffectCatalog))]
        static void Init()
        {
            string configSectionName = getConfigSectionName(EFFECT_ID);
            if (string.IsNullOrEmpty(configSectionName))
            {
                Log.Error(ERROR_INVALID_CONFIG_SECTION_NAME);
                return;
            }

            _numShrinesPerActivation = Main.Instance.Config.Bind(new ConfigDefinition(configSectionName, "Shrines per Activation"), 2, new ConfigDescription("The amount of mountain shrines to activate each time this effect is activated"));
            addConfigOption(new IntSliderOption(_numShrinesPerActivation, new IntSliderConfig
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
