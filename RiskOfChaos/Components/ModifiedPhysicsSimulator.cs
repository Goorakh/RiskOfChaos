using RiskOfChaos.ModifierController.PhysicsModification;
using UnityEngine;

namespace RiskOfChaos.Components
{
    public class ModifiedPhysicsSimulator : MonoBehaviour
    {
#if DEBUG
        bool _debugSimulateOverrideActive;
#endif

        void FixedUpdate()
        {
            PhysicsModificationManager physicsModificationManager = PhysicsModificationManager.Instance;

            bool autoSimulate;
            float speedMultiplier;
            if (physicsModificationManager)
            {
                autoSimulate = physicsModificationManager.ShouldAutoSimulatePhysics;
                speedMultiplier = physicsModificationManager.NetworkedTotalSimulationSpeedMultiplier;
            }
            else
            {
                autoSimulate = true;
                speedMultiplier = 1f;
            }

#if DEBUG
            autoSimulate &= !_debugSimulateOverrideActive;
#endif

            if (Physics.autoSimulation != autoSimulate)
            {
                Physics.autoSimulation = autoSimulate;

#if DEBUG
                Log.Debug($"autoSimulation={Physics.autoSimulation}");
#endif
            }

            if (!autoSimulate)
            {
                Physics.Simulate(Time.fixedDeltaTime * speedMultiplier);
            }
        }
    }
}
