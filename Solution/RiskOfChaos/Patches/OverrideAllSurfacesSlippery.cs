using KinematicCharacterController;
using RoR2;
using UnityEngine;

namespace RiskOfChaos.Patches
{
    static class OverrideAllSurfacesSlippery
    {
        public static bool IsActive;

        [SystemInitializer]
        static void Init()
        {
            On.RoR2.CharacterMotor.OnGroundHit += CharacterMotor_OnGroundHit;
        }

        static void CharacterMotor_OnGroundHit(On.RoR2.CharacterMotor.orig_OnGroundHit orig, CharacterMotor self, Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport)
        {
            orig(self, hitCollider, hitNormal, hitPoint, ref hitStabilityReport);

            if (IsActive)
            {
                // For some reason this is how ice gets slippery???
                self.isAirControlForced = true;
            }
        }
    }
}
