using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.ModifierController.PhysicsModification
{
    public sealed class PhysicsModificationManager : NetworkedValueModificationManager<PhysicsModificationInfo>
    {
        static PhysicsModificationManager _instance;
        public static PhysicsModificationManager Instance => _instance;

        const uint SIMULATION_SPEED_MULTIPLIER_DIRTY_BIT = 1 << 1;

        float _totalSimulationSpeedMultiplier = 1f;
        public float NetworkedTotalSimulationSpeedMultiplier
        {
            get
            {
                return _totalSimulationSpeedMultiplier;
            }

            [param: In]
            set
            {
                SetSyncVar(value, ref _totalSimulationSpeedMultiplier, SIMULATION_SPEED_MULTIPLIER_DIRTY_BIT);
            }
        }

        const float AUTO_SIMULATE_EPSILON = 0.01f;
        public bool ShouldAutoSimulatePhysics => Mathf.Abs(_totalSimulationSpeedMultiplier - 1f) <= AUTO_SIMULATE_EPSILON;

        void OnEnable()
        {
            SingletonHelper.Assign(ref _instance, this);
        }

        void OnDisable()
        {
            SingletonHelper.Unassign(ref _instance, this);
        }

        protected override void updateValueModifications()
        {
            PhysicsModificationInfo physicsModificationInfo = getModifiedValue(new PhysicsModificationInfo());

            // Only values >0 are supported
            NetworkedTotalSimulationSpeedMultiplier = Mathf.Max(1f / 10000f, physicsModificationInfo.SpeedMultiplier);
        }

        protected override bool serialize(NetworkWriter writer, bool initialState, uint dirtyBits)
        {
            bool baseAnythingWritten = base.serialize(writer, initialState, dirtyBits);

            if (initialState)
            {
                writer.Write(_totalSimulationSpeedMultiplier);
                return true;
            }

            bool anythingWritten = false;

            if ((dirtyBits & SIMULATION_SPEED_MULTIPLIER_DIRTY_BIT) != 0)
            {
                writer.Write(_totalSimulationSpeedMultiplier);
                anythingWritten = true;
            }

            return baseAnythingWritten || anythingWritten;
        }

        protected override void deserialize(NetworkReader reader, bool initialState, uint dirtyBits)
        {
            base.deserialize(reader, initialState, dirtyBits);

            if (initialState)
            {
                _totalSimulationSpeedMultiplier = reader.ReadSingle();
                return;
            }

            if ((dirtyBits & SIMULATION_SPEED_MULTIPLIER_DIRTY_BIT) != 0)
            {
                _totalSimulationSpeedMultiplier = reader.ReadSingle();
            }
        }
    }
}
