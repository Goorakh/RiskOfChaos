﻿using BepInEx.Configuration;
using RiskOfChaos.ConfigHandling;
using RiskOfChaos.Content;
using RiskOfChaos.EffectHandling;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Data;
using RiskOfChaos.EffectHandling.EffectComponents;
using RiskOfChaos.ModificationController;
using RiskOfChaos.ModificationController.Gravity;
using RiskOfChaos.Utilities;
using RiskOfOptions.OptionConfigs;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectDefinitions.World.Gravity
{
    [ChaosTimedEffect("rotate_gravity", TimedEffectType.UntilStageEnd)]
    [EffectConfigBackwardsCompatibility("Effect: Random Gravity Direction (Lasts 1 stage)")]
    public sealed class RotateGravity : NetworkBehaviour
    {
        const float DEVITATION_MIN_VALUE = 0f;
        const float DEVITATION_MAX_VALUE = 90f;

        [EffectConfig]
        static readonly ConfigHolder<float> _minDeviation =
            ConfigFactory<float>.CreateConfig("Min Rotation Angle", 25f)
                                .Description("The minimum amount of deviation (in degrees) that can be applied to the gravity direction")
                                .AcceptableValues(new AcceptableValueRange<float>(DEVITATION_MIN_VALUE, DEVITATION_MAX_VALUE))
                                .OptionConfig(new StepSliderConfig
                                {
                                    FormatString = "{0:F1}",
                                    min = DEVITATION_MIN_VALUE,
                                    max = DEVITATION_MAX_VALUE,
                                    increment = 0.5f
                                })
                                .Build();

        [EffectConfig]
        static readonly ConfigHolder<float> _maxDeviation =
            ConfigFactory<float>.CreateConfig("Max Rotation Angle", 50f)
                                .Description("The maximum amount of deviation (in degrees) that can be applied to the gravity direction")
                                .AcceptableValues(new AcceptableValueRange<float>(DEVITATION_MIN_VALUE, DEVITATION_MAX_VALUE))
                                .OptionConfig(new StepSliderConfig
                                {
                                    FormatString = "{0:F1}",
                                    min = DEVITATION_MIN_VALUE,
                                    max = DEVITATION_MAX_VALUE,
                                    increment = 0.5f
                                })
                                .Build();

        ChaosEffectComponent _effectComponent;

        Quaternion _gravityRotation = Quaternion.identity;

        ValueModificationController _gravityModificationController;

        void Awake()
        {
            _effectComponent = GetComponent<ChaosEffectComponent>();
        }

        public override void OnStartServer()
        {
            base.OnStartServer();

            Xoroshiro128Plus rng = new Xoroshiro128Plus(_effectComponent.Rng.nextUlong);

            float maxDeviation = _maxDeviation.Value;
            float minDeviation = Mathf.Min(_minDeviation.Value, maxDeviation);

            _gravityRotation = QuaternionUtils.Spread(Vector3.down, minDeviation, maxDeviation, rng);

            Log.Debug($"Gravity angle: {Vector3.Angle(Vector3.down, _gravityRotation * Vector3.down)}");
        }

        void Start()
        {
            if (NetworkServer.active)
            {
                _gravityModificationController = Instantiate(RoCContent.NetworkedPrefabs.GravityModificationProvider).GetComponent<ValueModificationController>();

                GravityModificationProvider gravityModificationProvider = _gravityModificationController.GetComponent<GravityModificationProvider>();
                gravityModificationProvider.GravityRotation = _gravityRotation;

                NetworkServer.Spawn(_gravityModificationController.gameObject);
            }
        }

        void OnDestroy()
        {
            if (_gravityModificationController)
            {
                _gravityModificationController.Retire();
                _gravityModificationController = null;
            }
        }
    }
}
