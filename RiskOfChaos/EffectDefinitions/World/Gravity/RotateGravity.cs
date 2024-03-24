using BepInEx.Configuration;
using RiskOfChaos.ConfigHandling;
using RiskOfChaos.EffectHandling;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Data;
using RiskOfChaos.Utilities;
using RiskOfOptions.OptionConfigs;
using System;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectDefinitions.World.Gravity
{
    [ChaosTimedEffect("rotate_gravity", TimedEffectType.UntilStageEnd, DefaultSelectionWeight = 0.8f)]
    [EffectConfigBackwardsCompatibility("Effect: Random Gravity Direction (Lasts 1 stage)")]
    public sealed class RotateGravity : GenericGravityEffect
    {
        const float MAX_DEVITATION_MIN_VALUE = 0f;
        const float MAX_DEVITATION_MAX_VALUE = 90f;

        [EffectConfig]
        static readonly ConfigHolder<float> _maxDeviation =
            ConfigFactory<float>.CreateConfig("Max Rotation Angle", 30f)
                                .Description("The maximum amount of deviation (in degrees) that can be applied to the gravity direction")
                                .AcceptableValues(new AcceptableValueRange<float>(MAX_DEVITATION_MIN_VALUE, MAX_DEVITATION_MAX_VALUE))
                                .OptionConfig(new StepSliderConfig
                                {
                                    formatString = "{0:F1}",
                                    min = MAX_DEVITATION_MIN_VALUE,
                                    max = MAX_DEVITATION_MAX_VALUE,
                                    increment = 0.5f
                                })
                                .Build();

        public override event Action OnValueDirty;

        Quaternion _gravityRotation;

        public override void OnPreStartServer()
        {
            base.OnPreStartServer();

            float maxDeviation = _maxDeviation.Value;
            _gravityRotation = QuaternionUtils.RandomDeviation(Mathf.Clamp(maxDeviation - 10f, 0f, 10f), maxDeviation, RNG);
        }

        public override void Serialize(NetworkWriter writer)
        {
            base.Serialize(writer);
            writer.Write(_gravityRotation);
        }

        public override void Deserialize(NetworkReader reader)
        {
            base.Deserialize(reader);
            _gravityRotation = reader.ReadQuaternion();
        }

        public override void ModifyValue(ref Vector3 gravity)
        {
            gravity = _gravityRotation * gravity;
        }
    }
}
