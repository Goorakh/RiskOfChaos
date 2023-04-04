using RiskOfChaos.EffectDefinitions;
using RiskOfChaos.Utilities;
using RoR2;
using RoR2.Audio;
using UnityEngine;

namespace RiskOfChaos.EffectHandling.Controllers
{
    [ChaosController(true)]
    public class ChaosEffectActivationSoundHandler : MonoBehaviour
    {
        static bool _isEffectActivationSoundInitialized;
        static AkEventIdArg _effectActivationSoundEventID;

        static ChaosEffectActivationSoundHandler()
        {
            static bool tryAssignEffectSoundID()
            {
                if (AkSoundEngine.IsInitialized())
                {
                    _effectActivationSoundEventID = AkSoundEngine.GetIDFromString("Play_env_hiddenLab_laptop_sequence_fail");
                    _isEffectActivationSoundInitialized = true;

#if DEBUG
                    Log.Debug($"Assigned effect activation event ID: {_effectActivationSoundEventID.id}");
#endif

                    return true;
                }
                else
                {
                    return false;
                }
            }

            if (!tryAssignEffectSoundID())
            {
                static void waitUntilSoundEngineInit()
                {
                    if (tryAssignEffectSoundID())
                    {
                        RoR2Application.onUpdate -= waitUntilSoundEngineInit;
                    }
                }

                RoR2Application.onUpdate += waitUntilSoundEngineInit;
            }
        }

        ChaosEffectDispatcher _effectDispatcher;

        void Awake()
        {
            _effectDispatcher = GetComponent<ChaosEffectDispatcher>();
        }

        void OnEnable()
        {
            _effectDispatcher.OnEffectDispatched += onEffectDispatched;
        }

        void OnDisable()
        {
            _effectDispatcher.OnEffectDispatched -= onEffectDispatched;
        }

        static void onEffectDispatched(in ChaosEffectInfo effectInfo, EffectDispatchFlags dispatchFlags, BaseEffect effectInstance)
        {
            if ((dispatchFlags & EffectDispatchFlags.DontPlaySound) == 0)
            {
                playEffectActivatedSoundOnAllPlayerBodies();
            }
        }

        static void playEffectActivatedSoundOnAllPlayerBodies()
        {
            if (!_isEffectActivationSoundInitialized)
            {
                Log.Warning("Unable to play effect activation sound, event ID not initialized");
                return;
            }

            foreach (CharacterBody playerBody in PlayerUtils.GetAllPlayerBodies(true))
            {
                EntitySoundManager.EmitSoundServer(_effectActivationSoundEventID, playerBody.gameObject);
            }
        }
    }
}
