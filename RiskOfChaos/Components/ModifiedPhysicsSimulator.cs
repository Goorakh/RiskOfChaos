using RiskOfChaos.ModifierController.PhysicsModification;
using UnityEngine;

namespace RiskOfChaos.Components
{
    [RequireComponent(typeof(PhysicsModificationManager))]
    public class ModifiedPhysicsSimulator : MonoBehaviour
    {
        PhysicsModificationManager _physicsModificationManager;

        float _physicsStepTimer = 0f;

        void Awake()
        {
            _physicsModificationManager = GetComponent<PhysicsModificationManager>();
        }

        void Update()
        {
            if (!_physicsModificationManager)
                return;

            if (Physics.autoSimulation != _physicsModificationManager.ShouldAutoSimulatePhysics)
            {
                Physics.autoSimulation = _physicsModificationManager.ShouldAutoSimulatePhysics;

#if DEBUG
                Log.Debug($"autoSimulation={Physics.autoSimulation}");
#endif
            }

            if (Physics.autoSimulation)
                return;

            _physicsStepTimer += Time.deltaTime;

            float fixedDeltaTime = Time.fixedDeltaTime;
            while (_physicsStepTimer > fixedDeltaTime)
            {
                _physicsStepTimer -= fixedDeltaTime;
                Physics.Simulate(fixedDeltaTime * _physicsModificationManager.NetworkedTotalSimulationSpeedMultiplier);
            }
        }
    }
}
