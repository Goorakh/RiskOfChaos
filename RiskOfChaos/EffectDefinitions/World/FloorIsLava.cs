using RiskOfChaos.Components;
using RiskOfChaos.ConfigHandling;
using RiskOfChaos.Content;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Data;
using RiskOfChaos.Utilities;
using RiskOfChaos.Utilities.CatalogIndexCollection;
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
            ConfigFactory<int>.CreateConfig("Percent Damage Per Second", 25)
                              .Description("How much damage should be inflicted for characters touching the ground. Value of 3 means 3% of max health in damage per second.")
                              .OptionConfig(new IntSliderConfig
                              {
                                  formatString = "{0}%/s",
                                  min = 1,
                                  max = 100
                              })
                              .ValueConstrictor(CommonValueConstrictors.Clamped(1, 100))
                              .Build();

        static int getGroundedDamagePerSecond(float groundedTime)
        {
            const float DAMAGE_START_TIME = 0.25f;
            const float DAMAGE_RAMP_END_TIME = 1.5f;

            if (groundedTime < DAMAGE_START_TIME)
                return 0;

            if (groundedTime <= DAMAGE_RAMP_END_TIME)
            {
                return Mathf.RoundToInt(Util.Remap(groundedTime, DAMAGE_START_TIME, DAMAGE_RAMP_END_TIME, 0f, _percentDamagePerSecond.Value));
            }
            else
            {
                return _percentDamagePerSecond.Value;
            }
        }

        [RequireComponent(typeof(CharacterBody))]
        sealed class GroundedDamageController : MonoBehaviour
        {
            static readonly MasterIndexCollection _overrideAlwaysGroundedMasters = new MasterIndexCollection(new string[]
            {
                "EngiTurretMaster",
                "SquidTurretMaster",
                "MinorConstructMaster",
                "Turret1Master",
                "VoidBarnacleNoCastMaster"
            });

            static DotController.DotIndex _dotIndex = DotController.DotIndex.None;
            static DotController.DotDef _dotDef;

            [SystemInitializer(typeof(CustomDOTs))]
            static void Init()
            {
                _dotIndex = CustomDOTs.PercentHealthDotIndex;
                _dotDef = DotController.GetDotDef(_dotIndex);
            }

            CharacterMaster _master;
            CharacterBody _body;
            CharacterMotor _motor;

            float _groundedTimer;

            void Awake()
            {
                _body = GetComponent<CharacterBody>();
                _master = _body.master;
                _motor = _body.characterMotor;

                InstanceTracker.Add(this);
            }

            void OnDestroy()
            {
                InstanceTracker.Remove(this);

                removeDOTStacks(int.MaxValue);
            }

            bool isGrounded()
            {
                if (_body.currentVehicle)
                    return false;

                if (_master && _overrideAlwaysGroundedMasters.Contains(_master.masterIndex))
                    return true;

                if (!_motor)
                    _motor = _body.characterMotor;

                return _motor && _motor.isGrounded;
            }

            void FixedUpdate()
            {
                if (!_body)
                    return;

                if (isGrounded())
                {
                    _groundedTimer += Time.fixedDeltaTime;
                }
                else
                {
                    _groundedTimer = 0f;
                }

                int missingDOTStacks = getGroundedDamagePerSecond(_groundedTimer) - _body.GetBuffCount(_dotDef.associatedBuff);
                if (missingDOTStacks > 0)
                {
                    for (int i = 0; i < missingDOTStacks; i++)
                    {
                        // AttackerObject has to be non-null for DOT to be applied, and passing in _body.gameObject will consider the character's items and stats for the dot damage
                        DotController.InflictDot(_body.gameObject, DummyDamageInflictor.Instance.gameObject, _dotIndex);
                    }
                }
                else if (missingDOTStacks < 0)
                {
                    removeDOTStacks(-missingDOTStacks);
                }
            }

            void removeDOTStacks(int stacksToRemove)
            {
                DotController dotController = DotController.FindDotController(_body.gameObject);
                if (dotController)
                {
                    dotController.RemoveDOTStacks(_dotIndex, stacksToRemove);
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
