using R2API.Networking;
using R2API.Networking.Interfaces;
using RiskOfChaos.Networking;
using RoR2;
using UnityEngine;

namespace RiskOfChaos.EffectHandling.Controllers
{
    [ChaosController(true)]
    public class ChaosEffectActivationSoundHandler : MonoBehaviour
    {
        const string EFFECT_ACTIVATION_SOUND_EVENT_NAME = "Play_env_hiddenLab_laptop_sequence_fail";

        static uint _effectActivationSoundEventID;

        [SystemInitializer]
        static void Init()
        {
            if (!Application.isBatchMode)
            {
                _effectActivationSoundEventID = AkSoundEngine.GetIDFromString(EFFECT_ACTIVATION_SOUND_EVENT_NAME);
                if (_effectActivationSoundEventID == 0)
                {
                    Log.Error("Failed to find effect activation sound ID");
                }
            }
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

        static void onEffectAboutToDispatchServer(ChaosEffectInfo effectInfo, in ChaosEffectDispatchArgs dispatchArgs, ref bool willStart)
        {
            if (!dispatchArgs.HasFlag(EffectDispatchFlags.DontPlaySound))
            {
                PlayEffectActivatedSound();
            }
        }

        public static void PlayEffectActivatedSound()
        {
            // Make sure dedicated server builds can still tell clients about the sound since it can't look up the ID
            PostAkEventLocalMessage playSoundMessage;
            if (Application.isBatchMode)
            {
                playSoundMessage = new PostAkEventLocalMessage(EFFECT_ACTIVATION_SOUND_EVENT_NAME);
            }
            else
            {
                if (_effectActivationSoundEventID == 0)
                    return;

                playSoundMessage = new PostAkEventLocalMessage(_effectActivationSoundEventID);
            }

            playSoundMessage.Send(NetworkDestination.Clients | NetworkDestination.Server);
        }
    }
}
