using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RoR2;
using UnityEngine;

namespace RiskOfChaos.EffectDefinitions.World.PhysicsModification
{
    [ChaosTimedEffect("laggy_physics", 60f, AllowDuplicates = false)]
    public sealed class LaggyPhysics : SimplePhysicsSpeedMultiplierEffect
    {
        protected override float multiplier => (_physicsActive ? 1f : 0f) * _physicsSpeedMultiplier;

        public override void OnStart()
        {
            base.OnStart();

            RoR2Application.onFixedUpdate += onFixedUpdate;
        }

        const float PHYSICS_ENABLED_ROLL_FREQUENCY = 0.1f;
        const float PHYSICS_ENABLED_PROBABILITY = 0.4f;

        float _lastPhysicsEnabledRollTime = float.NegativeInfinity;
        bool _physicsActive;

        const float CHANGE_PHYSICS_SPEED_PROBABILITY = 0.3f;
        float _physicsSpeedMultiplier = 1f;

        void onFixedUpdate()
        {
            if (TimeElapsed - _lastPhysicsEnabledRollTime >= PHYSICS_ENABLED_ROLL_FREQUENCY)
            {
                bool changedPhysicsSpeed = RoR2Application.rng.nextNormalizedFloat <= CHANGE_PHYSICS_SPEED_PROBABILITY;
                if (changedPhysicsSpeed)
                {
                    _physicsSpeedMultiplier = 1f + ((RoR2Application.rng.nextBool ? -1 : 1) * Mathf.Pow(RoR2Application.rng.nextNormalizedFloat, 2f) * 0.35f);
                }

                bool lastPhysicsActive = _physicsActive;

                _physicsActive = RoR2Application.rng.nextNormalizedFloat <= PHYSICS_ENABLED_PROBABILITY;
                _lastPhysicsEnabledRollTime = TimeElapsed;

                if (lastPhysicsActive != _physicsActive || changedPhysicsSpeed)
                {
                    invokeOnValueDirty();
                }
            }
        }

        public override void OnEnd()
        {
            base.OnEnd();

            RoR2Application.onFixedUpdate -= onFixedUpdate;
        }
    }
}
