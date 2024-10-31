using RiskOfChaos.Components;
using RiskOfChaos.ConfigHandling;
using RiskOfChaos.ConfigHandling.AcceptableValues;
using RiskOfChaos.EffectHandling;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Data;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.EffectHandling.Formatting;
using RiskOfChaos.Utilities.Extensions;
using RiskOfOptions.OptionConfigs;
using RoR2;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace RiskOfChaos.EffectDefinitions.World
{
    [ChaosEffect("mountain_shrine_add", ConfigName = "Add Mountain Shrine")]
    public sealed class MountainShrineAdd : MonoBehaviour
    {
        static GameObject _shrineUseEffectPrefab;

        [EffectConfig]
        static readonly ConfigHolder<int> _numShrinesPerActivation =
            ConfigFactory<int>.CreateConfig("Shrines per Activation", 2)
                              .Description("The amount of mountain shrines to activate each time this effect is activated")
                              .AcceptableValues(new AcceptableValueMin<int>(1))
                              .OptionConfig(new IntFieldConfig { Min = 1 })
                              .FormatsEffectName()
                              .Build();

        [SystemInitializer]
        static void Init()
        {
            AsyncOperationHandle<GameObject> shrineUseEffectLoad = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Common/VFX/ShrineUseEffect.prefab");
            shrineUseEffectLoad.OnSuccess(p => _shrineUseEffectPrefab = p);
        }

        [EffectCanActivate]
        static bool CanActivate(in EffectCanActivateContext context)
        {
            TeleporterInteraction instance = TeleporterInteraction.instance;
            return instance && (!context.IsNow || instance.activationState <= TeleporterInteraction.ActivationState.IdleToCharging);
        }

        [GetEffectNameFormatter]
        static EffectNameFormatter GetNameFormatter()
        {
            return new EffectNameFormatter_GenericInt32(_numShrinesPerActivation.Value);
        }

        void Start()
        {
            if (!NetworkServer.active)
                return;

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

            if (_shrineUseEffectPrefab)
            {
                foreach (PlayerCharacterMasterController playerMaster in PlayerCharacterMasterController.instances)
                {
                    if (!playerMaster.isConnected)
                        continue;

                    CharacterMaster master = playerMaster.master;
                    if (!master || master.IsDeadAndOutOfLivesServer())
                        continue;

                    CharacterBody body = master.GetBody();
                    if (!body)
                        continue;

                    EffectManager.SpawnEffect(_shrineUseEffectPrefab, new EffectData
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
}
