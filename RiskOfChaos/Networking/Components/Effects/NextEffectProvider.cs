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

        bool _hasValidEffectState = false;
        const uint HAS_VALID_EFFECT_STATE_DIRTY_BIT = 1 << 3;

        void OnEnable()
        {
            SingletonHelper.Assign(ref _instance, this);
        }

        void OnDisable()
        {
            SingletonHelper.Unassign(ref _instance, this);
        }

        readonly record struct NextEffectState(ChaosEffectIndex EffectIndex, Run.FixedTimeStamp ActivationTime, EffectNameFormatter DisplayNameFormatter);

        static bool tryGetNextEffectState(out NextEffectState nextEffectState)
        {
            nextEffectState = default;

            if (ChaosEffectActivationSignaler.EffectDispatchingCompletelyDisabled)
                return false;

            ChaosEffectDispatcher effectDispatcher = ChaosEffectDispatcher.Instance;
            if (!effectDispatcher)
                return false;

            ChaosEffectActivationSignaler effectSignaler = effectDispatcher.GetCurrentEffectSignaler();
            if (!effectSignaler)
                return false;

            Run run = Run.instance;
            Stage stage = Stage.instance;
            if (!run || !stage || (run.stageClearCount == 0 && stage.entryTime.timeSince < ChaosEffectActivationSignaler.MIN_STAGE_TIME_REQUIRED_TO_DISPATCH))
                return false;

            Run.FixedTimeStamp activationTime = Run.FixedTimeStamp.now + effectSignaler.GetTimeUntilNextEffect();

            ChaosEffectIndex nextEffectIndex = effectSignaler.GetUpcomingEffect();

            EffectNameFormatter displayNameFormatter;
            if (nextEffectIndex != ChaosEffectIndex.Invalid)
            {
                ChaosEffectInfo nextEffectInfo = ChaosEffectCatalog.GetEffectInfo(nextEffectIndex);

                displayNameFormatter = nextEffectInfo.LocalDisplayNameFormatter;
            }
            else
            {
                displayNameFormatter = EffectNameFormatter_None.Instance;
            }

            nextEffectState = new NextEffectState(nextEffectIndex, activationTime, displayNameFormatter);
            return true;
        }

        void refreshNextEffectState()
        {
            if (NetworkHasValidNextEffectState = tryGetNextEffectState(out NextEffectState nextEffectState))
            {
                NetworkNextEffectIndex = nextEffectState.EffectIndex;
                NetworkNextEffectActivationTime = nextEffectState.ActivationTime;
                NetworkNextEffectNameFormatter = nextEffectState.DisplayNameFormatter;
            }
            else
            {
                NetworkNextEffectIndex = ChaosEffectIndex.Invalid;
                NetworkNextEffectActivationTime = Run.FixedTimeStamp.negativeInfinity;
                NetworkNextEffectNameFormatter = EffectNameFormatter_None.Instance;
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

        public bool NetworkHasValidNextEffectState
        {
            get
            {
                return _hasValidEffectState;
            }
            private set
            {
                SetSyncVar(value, ref _hasValidEffectState, HAS_VALID_EFFECT_STATE_DIRTY_BIT);
            }
        }

        public override bool OnSerialize(NetworkWriter writer, bool initialState)
        {
            if (initialState)
            {
                writer.WriteChaosEffectIndex(_nextEffectIndex);
                writer.Write(_nextEffectActivationTime);

                writer.Write(_nextEffectNameFormatter);

                writer.Write(_hasValidEffectState);

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

            if ((dirtyBits & HAS_VALID_EFFECT_STATE_DIRTY_BIT) != 0)
            {
                writer.Write(_hasValidEffectState);
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

                _hasValidEffectState = reader.ReadBoolean();

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

            if ((dirtyBits & HAS_VALID_EFFECT_STATE_DIRTY_BIT) != 0)
            {
                _hasValidEffectState = reader.ReadBoolean();
            }
        }
    }
}
