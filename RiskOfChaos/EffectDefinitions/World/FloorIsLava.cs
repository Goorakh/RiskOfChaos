using RiskOfChaos.Components;
using RiskOfChaos.ConfigHandling;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Data;
using RiskOfChaos.Utilities;
using RiskOfChaos.Utilities.Extensions;
using RiskOfOptions.OptionConfigs;
using RoR2;
using UnityEngine;

namespace RiskOfChaos.EffectDefinitions.World
{
    [ChaosTimedEffect("floor_is_lava", 30f, AllowDuplicates = false)]
    public sealed class FloorIsLava : TimedEffect
    {
        [EffectConfig]
        static readonly ConfigHolder<int> _percentDamagePerSecond =
            ConfigFactory<int>.CreateConfig("Percent Damage Per Second", 3)
                              .Description("How much damage should be inflicted for characters touching the ground. Value of 3 means 3% of max health in damage per second.")
                              .OptionConfig(new IntSliderConfig
                              {
                                  formatString = "{0}%/s",
                                  min = 1,
                                  max = 100
                              })
                              .ValueConstrictor(CommonValueConstrictors.Clamped(1, 100))
                              .Build();

        [RequireComponent(typeof(CharacterBody))]
        sealed class GroundedDamageController : MonoBehaviour
        {
            CharacterBody _body;
            CharacterMotor _motor;

            float _groundedTimer;

            void Awake()
            {
                _body = GetComponent<CharacterBody>();
                _motor = _body.characterMotor;

                InstanceTracker.Add(this);
            }

            void OnDestroy()
            {
                InstanceTracker.Remove(this);
            }

            void FixedUpdate()
            {
                if (!_body)
                    return;

                if (!_motor)
                {
                    _motor = _body.characterMotor;
                    if (!_motor)
                        return;
                }

                const DotController.DotIndex DOT_INDEX = DotController.DotIndex.PercentBurn;

                if (_motor.isGrounded)
                {
                    _groundedTimer += Time.fixedDeltaTime;
                    if (_groundedTimer >= 0.25f && !_body.gameObject.HasDOT(DOT_INDEX))
                    {
                        for (int i = 0; i < _percentDamagePerSecond.Value; i++)
                        {
                            // AttackerObject has to be non-null for DOT to be applied, and passing in _body.gameObject will consider the character's items and stats for the dot damage
                            DotController.InflictDot(_body.gameObject, DummyDamageInflictor.Instance.gameObject, DOT_INDEX);
                        }
                    }
                }
                else
                {
                    DotController dotController = DotController.FindDotController(_body.gameObject);
                    if (dotController)
                    {
                        dotController.RemoveDOT(DOT_INDEX);
                    }

                    _groundedTimer = 0f;
                }
            }

            public static void AddComponentToBody(CharacterBody body)
            {
                body.gameObject.AddComponent<GroundedDamageController>();
            }
        }

        public override void OnStart()
        {
            CharacterBody.readOnlyInstancesList.TryDo(GroundedDamageController.AddComponentToBody, FormatUtils.GetBestBodyName);
            CharacterBody.onBodyStartGlobal += GroundedDamageController.AddComponentToBody;
        }

        public override void OnEnd()
        {
            CharacterBody.onBodyStartGlobal -= GroundedDamageController.AddComponentToBody;

            InstanceUtils.DestroyAllTrackedInstances<GroundedDamageController>();
        }
    }
}
