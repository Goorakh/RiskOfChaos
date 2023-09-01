using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.Utilities;
using RiskOfChaos.Utilities.Extensions;
using RoR2;
using UnityEngine;

namespace RiskOfChaos.EffectDefinitions.World
{
    [ChaosTimedEffect("floor_is_lava", 30f, AllowDuplicates = false)]
    public sealed class FloorIsLava : TimedEffect
    {
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

                if (_motor.isGrounded)
                {
                    _groundedTimer += Time.fixedDeltaTime;
                    if (_groundedTimer >= 0.25f && !_body.gameObject.HasDOT(DotController.DotIndex.Burn))
                    {
                        DotController.InflictDot(_body.gameObject, _body.gameObject, DotController.DotIndex.Burn, 8f, 0.15f);
                    }
                }
                else
                {
                    DotController dotController = DotController.FindDotController(_body.gameObject);
                    if (dotController)
                    {
                        dotController.RemoveDOT(DotController.DotIndex.Burn);
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
