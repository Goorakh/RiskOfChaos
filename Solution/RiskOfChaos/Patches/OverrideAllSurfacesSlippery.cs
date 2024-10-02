using KinematicCharacterController;
using RoR2;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace RiskOfChaos.Patches
{
    static class OverrideAllSurfacesSlippery
    {
        static bool _hasAppliedPatches;

        static bool _isActive;
        public static bool IsActive
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _isActive;
            set
            {
                if (_isActive == value)
                    return;

                _isActive = value;

                if (_isActive)
                {
                    tryApplyPatches();
                }

#if DEBUG
                Log.Debug($"Patch active: {_isActive}");
#endif
            }
        }

        static void tryApplyPatches()
        {
            if (_hasAppliedPatches)
                return;

            On.RoR2.CharacterMotor.OnGroundHit += CharacterMotor_OnGroundHit;

            _hasAppliedPatches = true;
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
