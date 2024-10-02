using RiskOfChaos.EffectHandling;
using RiskOfChaos.Utilities.Extensions;
using RoR2;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectDefinitions
{
    public abstract class TimedEffect : BaseEffect
    {
        public readonly new TimedEffectInfo EffectInfo;

        public TimedEffect() : base()
        {
            EffectInfo = base.EffectInfo as TimedEffectInfo;
            _maxStocks = EffectInfo.MaxStocks;
        }

        public bool IsNetDirty;

        public TimedEffectType TimedType { get; internal set; }

        public bool MatchesFlag(TimedEffectFlags flags)
        {
            return (flags & (TimedEffectFlags)(1 << (byte)TimedType)) != 0;
        }

        public virtual bool ShouldDisplayOnHUD
        {
            get
            {
                return EffectInfo.ShouldDisplayOnHUD && (TimedType != TimedEffectType.AlwaysActive || Configs.UI.DisplayAlwaysActiveEffects.Value);
            }
        }

        float _maxStocks = 1;
        public float MaxStocks
        {
            get
            {
                return _maxStocks;
            }
            set
            {
                _maxStocks = value;
                IsNetDirty = true;
            }
        }

        uint _spentStocks = 0;
        public uint SpentStocks
        {
            get
            {
                return _spentStocks;
            }
            set
            {
                _spentStocks = value;
                IsNetDirty = true;
            }
        }

        public float StocksRemaining => MaxStocks - SpentStocks;

        public float DurationSeconds { get; internal set; } = -1f;
        public float TimeStarted { get; private set; }

        public float TimeElapsed
        {
            get
            {
                Run run = Run.instance;
                if (!run)
                {
                    Log.Warning("No run instance");
                    return 0f;
                }

                return run.GetRunTime(RunTimerType.Realtime) - TimeStarted;
            }
        }

        public float TimeRemaining
        {
            get
            {
                if (DurationSeconds < 0f)
                {
                    Log.Warning($"Cannot get time remaining for effect {this}, no duration specified");
                    return 0f;
                }

                return (DurationSeconds * StocksRemaining) - TimeElapsed;
            }
        }

        public override void OnPreStartServer()
        {
            base.OnPreStartServer();

            TimeStarted = Run.instance.GetRunTime(RunTimerType.Realtime);
        }

        public override void Serialize(NetworkWriter writer)
        {
            base.Serialize(writer);

            writer.Write(TimeStarted);

            writer.Write(MaxStocks);
            writer.WritePackedUInt32(SpentStocks);

            writer.Write((byte)TimedType);
            if (TimedType == TimedEffectType.FixedDuration)
            {
                writer.Write(DurationSeconds);
            }
        }

        public override void Deserialize(NetworkReader reader)
        {
            base.Deserialize(reader);

            TimeStarted = reader.ReadSingle();

            _maxStocks = reader.ReadSingle();
            _spentStocks = reader.ReadPackedUInt32();

            TimedType = (TimedEffectType)reader.ReadByte();
            if (TimedType == TimedEffectType.FixedDuration)
            {
                DurationSeconds = reader.ReadSingle();
            }
        }

        public abstract void OnEnd();
    }
}
