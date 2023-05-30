using RiskOfChaos.Utilities;
using RoR2;
using RoR2.Audio;
using UnityEngine;

namespace RiskOfChaos.EffectHandling.Controllers
{
    [ChaosController(true)]
    public class ChaosEffectActivationSoundHandler : MonoBehaviour
    {
        static NetworkSoundEventIndex _effectActivationSoundEventIndex;

        [SystemInitializer(typeof(NetworkSoundEventCatalog))]
        static void Init()
        {
            _effectActivationSoundEventIndex = NetworkSoundEventCatalog.FindNetworkSoundEventIndex("Play_env_hiddenLab_laptop_sequence_fail");
        }

        ChaosEffectDispatcher _effectDispatcher;

        void Awake()
        {
            _effectDispatcher = GetComponent<ChaosEffectDispatcher>();
        }

        void OnEnable()
        {
            _effectDispatcher.OnEffectAboutToDispatchServer += onEffectAboutToDispatchServer;
        }

        void OnDisable()
        {
            _effectDispatcher.OnEffectAboutToDispatchServer -= onEffectAboutToDispatchServer;
        }

        static void onEffectAboutToDispatchServer(in ChaosEffectInfo effectInfo, EffectDispatchFlags dispatchFlags, bool willStart)
        {
            if ((dispatchFlags & EffectDispatchFlags.DontPlaySound) == 0)
            {
                playEffectActivatedSoundOnAllPlayerBodies();
            }
        }

        static void playEffectActivatedSoundOnAllPlayerBodies()
        {
            if (_effectActivationSoundEventIndex == NetworkSoundEventIndex.Invalid)
            {
                Log.Warning("Unable to play effect activation sound, event ID not initialized");
                return;
            }

            foreach (CharacterBody playerBody in PlayerUtils.GetAllPlayerBodies(true))
            {
                EntitySoundManager.EmitSoundServer(_effectActivationSoundEventIndex, playerBody.gameObject);
            }
        }
    }
}
