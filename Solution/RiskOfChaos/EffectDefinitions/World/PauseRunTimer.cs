using RiskOfChaos.EffectHandling;
using RiskOfChaos.EffectHandling.Controllers;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Data;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectDefinitions.World
{
    [ChaosTimedEffect("pause_run_timer", 60f, AllowDuplicates = false)]
    public sealed class PauseRunTimer : MonoBehaviour
    {
        [InitEffectInfo]
        public static readonly TimedEffectInfo EffectInfo;

        void Start()
        {
            if (!NetworkServer.active)
                return;

            ChaosEffectActivationSignaler.CanDispatchEffectsOverride += ChaosEffectActivationSignaler_CanDispatchEffectsOverride;
        }

        void OnDestroy()
        {
            ChaosEffectActivationSignaler.CanDispatchEffectsOverride -= ChaosEffectActivationSignaler_CanDispatchEffectsOverride;
        }

        static bool ChaosEffectActivationSignaler_CanDispatchEffectsOverride()
        {
            return false;
        }
    }
}
