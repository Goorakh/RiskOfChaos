using RiskOfChaos.Content;
using RoR2;
using RoR2.Audio;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectHandling.Controllers
{
    [DisallowMultipleComponent]
    [RequiredComponents(typeof(ChaosEffectDispatcher))]
    public sealed class ChaosEffectActivationSoundHandler : NetworkBehaviour
    {
        static ChaosEffectActivationSoundHandler _instance;
        public static ChaosEffectActivationSoundHandler Instance => _instance;

        static AkEventIdArg _effectActivationSoundEvent;

        [SystemInitializer]
        static void Init()
        {
            bool canPlaySound = !Application.isBatchMode;

            AkEventIdArg effectActivationSoundEvent = 0;
            if (canPlaySound)
            {
                effectActivationSoundEvent = (AkEventIdArg)"Play_env_hiddenLab_laptop_sequence_fail";
                if (effectActivationSoundEvent == 0)
                {
                    Log.Error("Failed to find effect activation sound ID");
                }
            }

            _effectActivationSoundEvent = effectActivationSoundEvent;
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
            if (_effectActivationSoundEvent == 0)
                return;

            bool playedSound = false;
            foreach (CameraRigController cameraRigController in CameraRigController.readOnlyInstancesList)
            {
                if (cameraRigController && cameraRigController.localUserViewer != null)
                {
                    AkSoundEngine.PostEvent(_effectActivationSoundEvent, cameraRigController.gameObject);
                    playedSound = true;
                }
            }

            if (!playedSound)
            {
                foreach (AkAudioListener audioListener in AkAudioListener.DefaultListeners.ListenerList)
                {
                    if (audioListener.GetComponent<MusicController>())
                        continue;

                    AkSoundEngine.PostEvent(_effectActivationSoundEvent, audioListener.gameObject);
                }
            }
        }
        
        [Server]
        public void PlayEffectActivatedSoundServer()
        {
            RpcPlayEffectActivatedSound();
        }
    }
}
