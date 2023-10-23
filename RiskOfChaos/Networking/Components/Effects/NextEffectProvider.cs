using RiskOfChaos.EffectHandling;
using RiskOfChaos.EffectHandling.Controllers;
using RiskOfChaos.UI.NextEffectDisplay;
using RiskOfChaos.Utilities.Extensions;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.Networking.Components.Effects
{
    public class NextEffectProvider : NetworkBehaviour
    {
        static NextEffectProvider _instance;
        public static NextEffectProvider Instance => _instance;

        ChaosEffectIndex _nextEffectIndex = ChaosEffectIndex.Invalid;
        const uint NEXT_EFFECT_INDEX_DIRTY_BIT = 1 << 0;

        Run.FixedTimeStamp _nextEffectActivationTime = Run.FixedTimeStamp.negativeInfinity;
        const uint NEXT_EFFECT_ACTIVATION_TIME_DIRTY_BIT = 1 << 1;

        void OnEnable()
        {
            SingletonHelper.Assign(ref _instance, this);
        }

        void OnDisable()
        {
            SingletonHelper.Unassign(ref _instance, this);
        }

        static bool tryGetNextEffectState(out ChaosEffectIndex nextEffectIndex, out Run.FixedTimeStamp nextEffectActivationTime)
        {
            ChaosEffectDispatcher effectDispatcher = ChaosEffectDispatcher.Instance;
            if (effectDispatcher && effectDispatcher.HasAttemptedDispatchAnyEffectServer)
            {
                ChaosEffectActivationSignaler effectSignaler = effectDispatcher.GetCurrentEffectSignaler();
                if (effectSignaler)
                {
                    nextEffectIndex = effectSignaler.GetUpcomingEffect();
                    nextEffectActivationTime = Run.FixedTimeStamp.now + effectSignaler.GetTimeUntilNextEffect();
                    return true;
                }
            }

            nextEffectIndex = ChaosEffectIndex.Invalid;
            nextEffectActivationTime = Run.FixedTimeStamp.negativeInfinity;
            return false;
        }

        void FixedUpdate()
        {
            if (!hasAuthority)
                return;

            if (tryGetNextEffectState(out ChaosEffectIndex nextEffectIndex, out Run.FixedTimeStamp nextEffectActivationTime))
            {
                NetworkNextEffectIndex = nextEffectIndex;
                NetworkNextEffectActivationTime = nextEffectActivationTime;
            }
            else
            {
                NetworkNextEffectIndex = ChaosEffectIndex.Invalid;
                NetworkNextEffectActivationTime = Run.FixedTimeStamp.negativeInfinity;
            }
        }

        public ChaosEffectIndex NetworkNextEffectIndex
        {
            get
            {
                return _nextEffectIndex;
            }
            set
            {
                if (_nextEffectIndex == value)
                    return;

                _nextEffectIndex = value;
                SetDirtyBit(NEXT_EFFECT_INDEX_DIRTY_BIT);
            }
        }

        public Run.FixedTimeStamp NetworkNextEffectActivationTime
        {
            get
            {
                return _nextEffectActivationTime;
            }
            set
            {
                SetSyncVar(value, ref _nextEffectActivationTime, NEXT_EFFECT_ACTIVATION_TIME_DIRTY_BIT);
            }
        }

        public override bool OnSerialize(NetworkWriter writer, bool initialState)
        {
            if (initialState)
            {
                writer.WriteChaosEffectIndex(_nextEffectIndex);
                writer.Write(_nextEffectActivationTime);
                return true;
            }

            uint dirtyBits = syncVarDirtyBits;
            writer.WritePackedUInt32(dirtyBits);

            bool anythingWritten = false;
            if ((dirtyBits & NEXT_EFFECT_INDEX_DIRTY_BIT) != 0)
            {
                writer.WriteChaosEffectIndex(_nextEffectIndex);
                anythingWritten = true;
            }

            if ((dirtyBits & NEXT_EFFECT_ACTIVATION_TIME_DIRTY_BIT) != 0)
            {
                writer.Write(_nextEffectActivationTime);
                anythingWritten = true;
            }

            return anythingWritten;
        }

        public override void OnDeserialize(NetworkReader reader, bool initialState)
        {
            if (initialState)
            {
                _nextEffectIndex = reader.ReadChaosEffectIndex();
                _nextEffectActivationTime = reader.ReadFixedTimeStamp();
                return;
            }

            uint dirtyBits = reader.ReadPackedUInt32();
            if ((dirtyBits & NEXT_EFFECT_INDEX_DIRTY_BIT) != 0)
            {
                _nextEffectIndex = reader.ReadChaosEffectIndex();
            }

            if ((dirtyBits & NEXT_EFFECT_ACTIVATION_TIME_DIRTY_BIT) != 0)
            {
                _nextEffectActivationTime = reader.ReadFixedTimeStamp();
            }
        }
    }
}
