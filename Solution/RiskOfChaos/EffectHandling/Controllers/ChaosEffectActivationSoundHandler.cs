using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectHandling.Controllers
{
    [DisallowMultipleComponent]
    [RequiredComponents(typeof(ChaosEffectDispatcher))]
    public class ChaosEffectActivationSoundHandler : NetworkBehaviour
    {
        static ChaosEffectActivationSoundHandler _instance;
        public static ChaosEffectActivationSoundHandler Instance => _instance;

        const string EFFECT_ACTIVATION_SOUND_EVENT_NAME = "Play_env_hiddenLab_laptop_sequence_fail";

        static bool _canPlaySound;
        static uint _effectActivationSoundEventID;

        [SystemInitializer]
        static void Init()
        {
            _canPlaySound = !Application.isBatchMode;

            if (_canPlaySound)
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
            SingletonHelper.Assign(ref _instance, this);

            _effectDispatcher.OnEffectAboutToDispatchServer += onEffectAboutToDispatchServer;
        }

        void OnDisable()
        {
            _effectDispatcher.OnEffectAboutToDispatchServer -= onEffectAboutToDispatchServer;

            SingletonHelper.Unassign(ref _instance, this);
        }

        void onEffectAboutToDispatchServer(ChaosEffectInfo effectInfo, in ChaosEffectDispatchArgs dispatchArgs, ref bool willStart)
        {
            if ((dispatchArgs.DispatchFlags & EffectDispatchFlags.DontPlaySound) == 0)
            {
                PlayEffectActivatedSoundServer();
            }
        }

        [ClientRpc]
        void RpcPlayEffectActivatedSound()
        {
            PlayEffectActivatedSoundClient();
        }

        [Client]
        public void PlayEffectActivatedSoundClient()
        {
            if (!_canPlaySound || _effectActivationSoundEventID == 0)
                return;

            foreach (AkAudioListener audioListener in AkAudioListener.DefaultListeners.ListenerList)
            {
                AkSoundEngine.PostEvent(_effectActivationSoundEventID, audioListener.gameObject);
            }
        }

        [Server]
        public void PlayEffectActivatedSoundServer()
        {
            RpcPlayEffectActivatedSound();

            if (NetworkClient.active)
            {
                PlayEffectActivatedSoundClient();
            }
        }
    }
}
