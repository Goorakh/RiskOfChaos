using RiskOfChaos.EffectHandling;
using RiskOfChaos.EffectHandling.Controllers;
using RiskOfChaos.EffectHandling.Formatting;
using RiskOfChaos.Utilities.Extensions;
using RoR2;
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

        EffectNameFormatter _nextEffectNameFormatter = null;
        const uint NEXT_EFFECT_FORMAT_ARGS_DIRTY_BIT = 1 << 2;

        void OnEnable()
        {
            SingletonHelper.Assign(ref _instance, this);
        }

        void OnDisable()
        {
            SingletonHelper.Unassign(ref _instance, this);
        }

        readonly record struct NextEffectState(ChaosEffectIndex EffectIndex, Run.FixedTimeStamp ActivationTime, EffectNameFormatter DisplayNameFormatter)
        {
            public static readonly NextEffectState None = new NextEffectState(ChaosEffectIndex.Invalid, Run.FixedTimeStamp.negativeInfinity, null);
        }

        static bool tryGetNextEffectState(out NextEffectState nextEffectState)
        {
            ChaosEffectDispatcher effectDispatcher = ChaosEffectDispatcher.Instance;
            if (effectDispatcher && effectDispatcher.HasAttemptedDispatchAnyEffectServer)
            {
                ChaosEffectActivationSignaler effectSignaler = effectDispatcher.GetCurrentEffectSignaler();
                if (effectSignaler)
                {
                    ChaosEffectIndex nextEffectIndex = effectSignaler.GetUpcomingEffect();
                    ChaosEffectInfo nextEffectInfo = ChaosEffectCatalog.GetEffectInfo(nextEffectIndex);

                    Run.FixedTimeStamp activationTime = Run.FixedTimeStamp.now + effectSignaler.GetTimeUntilNextEffect();

                    EffectNameFormatter displayNameFormatter = nextEffectInfo.GetDisplayNameFormatter();

                    nextEffectState = new NextEffectState(nextEffectIndex, activationTime, displayNameFormatter);
                    return true;
                }
            }

            nextEffectState = NextEffectState.None;
            return false;
        }

        void refreshNextEffectState()
        {
            if (tryGetNextEffectState(out NextEffectState nextEffectState))
            {
                NetworkNextEffectIndex = nextEffectState.EffectIndex;
                NetworkNextEffectActivationTime = nextEffectState.ActivationTime;
                NetworkNextEffectNameFormatter = nextEffectState.DisplayNameFormatter;
            }
            else
            {
                NetworkNextEffectIndex = ChaosEffectIndex.Invalid;
                NetworkNextEffectActivationTime = Run.FixedTimeStamp.negativeInfinity;
                NetworkNextEffectNameFormatter = null;
            }
        }

        void FixedUpdate()
        {
            if (!hasAuthority)
                return;

            refreshNextEffectState();
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

        public EffectNameFormatter NetworkNextEffectNameFormatter
        {
            get
            {
                return _nextEffectNameFormatter;
            }
            set
            {
                if (value is null && _nextEffectNameFormatter is null)
                    return;

                if (value is null ^ _nextEffectNameFormatter is null)
                {
                    _nextEffectNameFormatter = value;
                    SetDirtyBit(NEXT_EFFECT_FORMAT_ARGS_DIRTY_BIT);
                }
                else
                {
                    SetSyncVar(value, ref _nextEffectNameFormatter, NEXT_EFFECT_FORMAT_ARGS_DIRTY_BIT);
                }
            }
        }

        public override bool OnSerialize(NetworkWriter writer, bool initialState)
        {
            if (initialState)
            {
                writer.WriteChaosEffectIndex(_nextEffectIndex);
                writer.Write(_nextEffectActivationTime);

                writer.Write(_nextEffectNameFormatter);

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

            if ((dirtyBits & NEXT_EFFECT_FORMAT_ARGS_DIRTY_BIT) != 0)
            {
                writer.Write(_nextEffectNameFormatter);
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

                _nextEffectNameFormatter = reader.ReadEffectNameFormatter();

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

            if ((dirtyBits & NEXT_EFFECT_FORMAT_ARGS_DIRTY_BIT) != 0)
            {
                _nextEffectNameFormatter = reader.ReadEffectNameFormatter();
            }
        }
    }
}
