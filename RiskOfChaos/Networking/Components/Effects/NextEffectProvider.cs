using Newtonsoft.Json;
using RiskOfChaos.EffectHandling;
using RiskOfChaos.EffectHandling.Controllers;
using RiskOfChaos.Utilities.Extensions;
using RoR2;
using System;
using System.Linq;
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

        string[] _nextEffectFormatArgs = Array.Empty<string>();
        const uint NEXT_EFFECT_FORMAT_ARGS_DIRTY_BIT = 1 << 2;

        void OnEnable()
        {
            SingletonHelper.Assign(ref _instance, this);
        }

        void OnDisable()
        {
            SingletonHelper.Unassign(ref _instance, this);
        }

        static bool tryGetNextEffectState(out ChaosEffectIndex nextEffectIndex, out Run.FixedTimeStamp nextEffectActivationTime, out string[] nextEffectNameFormatArgs)
        {
            ChaosEffectDispatcher effectDispatcher = ChaosEffectDispatcher.Instance;
            if (effectDispatcher && effectDispatcher.HasAttemptedDispatchAnyEffectServer)
            {
                ChaosEffectActivationSignaler effectSignaler = effectDispatcher.GetCurrentEffectSignaler();
                if (effectSignaler)
                {
                    nextEffectIndex = effectSignaler.GetUpcomingEffect();
                    nextEffectActivationTime = Run.FixedTimeStamp.now + effectSignaler.GetTimeUntilNextEffect();

                    ChaosEffectInfo nextEffectInfo = ChaosEffectCatalog.GetEffectInfo(nextEffectIndex);
                    if (nextEffectInfo.HasCustomDisplayNameFormatter)
                    {
                        nextEffectNameFormatArgs = nextEffectInfo.GetDisplayNameFormatArgs();
                    }
                    else
                    {
                        nextEffectNameFormatArgs = null;
                    }

                    return true;
                }
            }

            nextEffectIndex = ChaosEffectIndex.Invalid;
            nextEffectActivationTime = Run.FixedTimeStamp.negativeInfinity;
            nextEffectNameFormatArgs = null;
            return false;
        }

        void FixedUpdate()
        {
            if (!hasAuthority)
                return;

            if (tryGetNextEffectState(out ChaosEffectIndex nextEffectIndex, out Run.FixedTimeStamp nextEffectActivationTime, out string[] nextEffectNameFormatArgs))
            {
                NetworkNextEffectIndex = nextEffectIndex;
                NetworkNextEffectActivationTime = nextEffectActivationTime;
                NetworkNextEffectFormatArgs = nextEffectNameFormatArgs;
            }
            else
            {
                NetworkNextEffectIndex = ChaosEffectIndex.Invalid;
                NetworkNextEffectActivationTime = Run.FixedTimeStamp.negativeInfinity;
                NetworkNextEffectFormatArgs = null;
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

        public string[] NetworkNextEffectFormatArgs
        {
            get
            {
                return _nextEffectFormatArgs;
            }
            set
            {
                value ??= Array.Empty<string>();

                if (value.Length != _nextEffectFormatArgs.Length || !value.SequenceEqual(_nextEffectFormatArgs))
                {
                    _nextEffectFormatArgs = value;
                    SetDirtyBit(NEXT_EFFECT_FORMAT_ARGS_DIRTY_BIT);
                }
            }
        }

        public override bool OnSerialize(NetworkWriter writer, bool initialState)
        {
            if (initialState)
            {
                writer.WriteChaosEffectIndex(_nextEffectIndex);
                writer.Write(_nextEffectActivationTime);

                writer.WritePackedUInt32((uint)_nextEffectFormatArgs.Length);
                foreach (string arg in _nextEffectFormatArgs)
                {
                    writer.Write(arg);
                }

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
                writer.WritePackedUInt32((uint)_nextEffectFormatArgs.Length);
                foreach (string arg in _nextEffectFormatArgs)
                {
                    writer.Write(arg);
                }

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

                _nextEffectFormatArgs = new string[reader.ReadPackedUInt32()];
                for (int i = 0; i < _nextEffectFormatArgs.Length; i++)
                {
                    _nextEffectFormatArgs[i] = reader.ReadString();
                }

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
                _nextEffectFormatArgs = new string[reader.ReadPackedUInt32()];
                for (int i = 0; i < _nextEffectFormatArgs.Length; i++)
                {
                    _nextEffectFormatArgs[i] = reader.ReadString();
                }
            }
        }
    }
}
