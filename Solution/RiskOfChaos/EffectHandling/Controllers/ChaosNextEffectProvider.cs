using RiskOfChaos.EffectHandling.Formatting;
using RiskOfChaos.Utilities;
using RoR2;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectHandling.Controllers
{
    [DisallowMultipleComponent]
    public class ChaosNextEffectProvider : NetworkBehaviour
    {
        static ChaosNextEffectProvider _instance;
        public static ChaosNextEffectProvider Instance => _instance;

        [SyncVar]
        public RunTimeStamp NextEffectActivationTime = Run.FixedTimeStamp.negativeInfinity;

        [SyncVar]
        int _nextEffectIndexInternal;

        public ChaosEffectIndex NextEffectIndex
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (ChaosEffectIndex)(_nextEffectIndexInternal - 1);
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => _nextEffectIndexInternal = (int)value + 1;
        }

        public EffectNameFormatter NextEffectNameFormatter
        {
            get
            {
                if (!ChaosEffectNameFormattersNetworker.Instance)
                    return null;

                return ChaosEffectNameFormattersNetworker.Instance.GetNameFormatter(NextEffectIndex);
            }
        }

        void OnEnable()
        {
            SingletonHelper.Assign(ref _instance, this);
        }

        void OnDisable()
        {
            SingletonHelper.Unassign(ref _instance, this);
        }

        void FixedUpdate()
        {
            if (NetworkServer.active)
            {
                fixedUpdateServer();
            }
        }

        [Server]
        void fixedUpdateServer()
        {
            RunTimeStamp mostRelevantNextEffectActivationTime = Run.FixedTimeStamp.negativeInfinity;
            ChaosEffectIndex mostRelevantNextEffectIndex = ChaosEffectIndex.Invalid;

            if (!ChaosEffectActivationSignaler.EffectDispatchingCompletelyDisabled &&
                ((Run.instance && Run.instance.stageClearCount > 0) ||
                 (Stage.instance && Stage.instance.entryTime.timeSinceClamped > ChaosEffectActivationSignaler.MIN_STAGE_TIME_REQUIRED_TO_DISPATCH)))
            {
                foreach (ChaosEffectActivationSignaler effectActivationSignaler in ChaosEffectActivationSignaler.InstancesList)
                {
                    RunTimeStamp effectActivationTime = effectActivationSignaler.GetNextEffectActivationTime();
                    if (!effectActivationTime.IsInfinity && (mostRelevantNextEffectActivationTime.IsNegativeInfinity || effectActivationTime < mostRelevantNextEffectActivationTime))
                    {
                        mostRelevantNextEffectActivationTime = effectActivationTime;
                        mostRelevantNextEffectIndex = effectActivationSignaler.GetUpcomingEffect();
                    }
                }
            }

            NextEffectActivationTime = mostRelevantNextEffectActivationTime;
            NextEffectIndex = mostRelevantNextEffectIndex;
        }
    }
}
