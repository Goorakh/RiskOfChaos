using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RoR2;
using System;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectHandling.Controllers
{
    [DisallowMultipleComponent]
    [RequiredComponents(typeof(ChaosEffectDispatcher))]
    public class ChaosAlwaysActiveEffectsHandler : MonoBehaviour
    {
        void Awake()
        {
            if (!NetworkServer.active)
            {
                enabled = false;
                return;
            }
        }

        void Start()
        {
            if (!NetworkServer.active)
                return;

            foreach (TimedEffectInfo timedEffectInfo in ChaosEffectCatalog.AllTimedEffects)
            {
                int alwaysActiveCount = timedEffectInfo.AlwaysActiveCount;
                if (alwaysActiveCount <= 0)
                    continue;

                Xoroshiro128Plus alwaysActiveEffectRNG = new Xoroshiro128Plus((ulong)HashCode.Combine(Run.instance.seed, timedEffectInfo.Identifier));

                for (int i = 0; i < alwaysActiveCount; i++)
                {
                    ChaosEffectDispatchArgs dispatchArgs = new ChaosEffectDispatchArgs
                    {
                        DispatchFlags = EffectDispatchFlags.DontPlaySound | EffectDispatchFlags.DontSendChatMessage,
                        RNGSeed = alwaysActiveEffectRNG.nextUlong,
                        OverrideDurationType = TimedEffectType.AlwaysActive,
                        OverrideDuration = 1f
                    };

                    ChaosEffectDispatcher.Instance.DispatchEffectServer(timedEffectInfo, dispatchArgs);
                }
            }
        }
    }
}
