using RiskOfChaos.EffectHandling;
using RiskOfChaos.Utilities.Extensions;
using RoR2;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectDefinitions
{
    public abstract class TimedEffect : BaseEffect
    {
        public bool IsNetDirty;

        public TimedEffectType TimedType { get; internal set; }
        public float DurationSeconds { get; internal set; } = -1f;

        float _effectStartTime;
        public float TimeStarted => _effectStartTime;

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

                return run.GetRunTime(RunTimerType.Realtime) - _effectStartTime;
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
        }

        public void SetTimeRemaining(float newTimeRemaining)
        {
            if (!NetworkServer.active)
            {
                Log.Warning("Called on client");
                return;
            }

            if (TimedType != TimedEffectType.FixedDuration)
            {
                Log.Error("Cannot set time remaining on non-fixed duration effect");
                return;
            }

            DurationSeconds = TimeElapsed + newTimeRemaining;
            IsNetDirty = true;
        }

        public override void OnPreStartServer()
        {
            base.OnPreStartServer();

            _effectStartTime = Run.instance.GetRunTime(RunTimerType.Realtime);
        }

        public override void Serialize(NetworkWriter writer)
        {
            base.Serialize(writer);

            writer.Write(_effectStartTime);

            writer.Write((byte)TimedType);
            if (TimedType == TimedEffectType.FixedDuration)
            {
                writer.Write(DurationSeconds);
            }
        }

        public override void Deserialize(NetworkReader reader)
        {
            base.Deserialize(reader);

            _effectStartTime = reader.ReadSingle();

            TimedType = (TimedEffectType)reader.ReadByte();

            if (TimedType == TimedEffectType.FixedDuration)
            {
                DurationSeconds = reader.ReadSingle();
            }
        }

        public abstract void OnEnd();
    }
}
