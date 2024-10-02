using RiskOfChaos.Networking.Components;
using RiskOfChaos.Patches;
using RoR2;
using UnityEngine;

namespace RiskOfChaos.Utilities.Extensions
{
    public static class CharacterMotorExtensions
    {
        public static Vector3 GetGravity(this CharacterMotor motor)
        {
            return motor.GetGravity(Physics.gravity);
        }

        public static Vector3 GetGravity(this CharacterMotor motor, Vector3 worldGravity)
        {
            if (motor && motor.TryGetComponent(out IsJumpingOnJumpPadTracker jumpingTracker) && jumpingTracker.IsJumping)
            {
                return GravityTracker.BaseGravity;
            }
            else
            {
                return worldGravity;
            }
        }
    }
}
