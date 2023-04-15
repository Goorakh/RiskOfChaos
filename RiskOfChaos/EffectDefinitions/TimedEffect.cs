using BepInEx.Configuration;
using RiskOfChaos.EffectHandling;
using RiskOfChaos.Utilities.Extensions;
using RiskOfOptions.OptionConfigs;
using RiskOfOptions.Options;
using RoR2;
using System;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectDefinitions
{
    public abstract class TimedEffect : BaseEffect
    {
        protected static ConfigEntry<float> bindEffectDurationConfig(ChaosEffectInfo effectInfo, float defaultDuration, StepSliderConfig stepSliderConfig)
        {
            ConfigEntry<float> entry = Main.Instance.Config.Bind(new ConfigDefinition(effectInfo.ConfigSectionName, "Effect Duration"), defaultDuration, new ConfigDescription("How long the effect should last (in seconds)"));

            addConfigOption(new StepSliderOption(entry, stepSliderConfig));

            return entry;
        }

        public abstract TimedEffectType TimedType { get; }

        protected virtual TimeSpan duration => TimeSpan.Zero;

        float _effectStartTime;
        float _fixedDurationSeconds = -1f;

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
                if (_fixedDurationSeconds < 0f)
                {
                    Log.Warning($"Cannot get time remaining for effect {this}, no duration specified");
                    return 0f;
                }

                return _fixedDurationSeconds - TimeElapsed;
            }
        }

        public override void OnPreStartServer()
        {
            base.OnPreStartServer();

            _effectStartTime = Run.instance.GetRunTime(RunTimerType.Realtime);

            if (TimedType == TimedEffectType.FixedDuration)
            {
                _fixedDurationSeconds = (float)duration.TotalSeconds;
            }
        }

        public override void Serialize(NetworkWriter writer)
        {
            base.Serialize(writer);

            writer.Write(_effectStartTime);

            writer.Write((byte)TimedType);
            if (TimedType == TimedEffectType.FixedDuration)
            {
                writer.Write((float)duration.TotalSeconds);
            }
        }

        public override void Deserialize(NetworkReader reader)
        {
            base.Deserialize(reader);

            _effectStartTime = reader.ReadSingle();

            if (reader.ReadByte() == (byte)TimedEffectType.FixedDuration)
            {
                _fixedDurationSeconds = reader.ReadSingle();
            }
        }

        public abstract void OnEnd();
    }
}
