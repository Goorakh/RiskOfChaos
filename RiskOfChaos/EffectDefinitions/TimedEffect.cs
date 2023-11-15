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
        }

        public bool IsNetDirty;

        public TimedEffectType TimedType { get; internal set; }

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

                return DurationSeconds - TimeElapsed;
            }
            set
            {
                if (!NetworkServer.active)
                {
                    Log.Warning("Called on client");
                    return;
                }

                if (DurationSeconds < 0f)
                {
                    Log.Warning($"Cannot set time remaining for effect {this}, no duration specified");
                    return;
                }

                DurationSeconds = TimeElapsed + value;
                IsNetDirty = true;
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

            TimedType = (TimedEffectType)reader.ReadByte();

            if (TimedType == TimedEffectType.FixedDuration)
            {
                DurationSeconds = reader.ReadSingle();
            }
        }

        public abstract void OnEnd();
    }
}
