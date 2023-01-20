using KinematicCharacterController;
using R2API.Networking;
using R2API.Networking.Interfaces;
using RiskOfChaos.Networking;
using RoR2;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.Patches
{
    static class OverrideAllSurfacesSlippery
    {
        static bool _hasAppliedPatches;

        static bool _isActive;
        static bool isActive
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _isActive;
            set
            {
                if (_isActive == value)
                    return;

                _isActive = value;

                if (_isActive && !_hasAppliedPatches)
                {
                    On.RoR2.CharacterMotor.OnGroundHit += CharacterMotor_OnGroundHit;
                    Run.onRunDestroyGlobal += Run_onRunDestroyGlobal;

                    _hasAppliedPatches = true;
                }
            }
        }

        public static bool NetworkIsActive
        {
            set
            {
                if (!NetworkServer.active)
                {
                    Log.Warning($"set_{nameof(NetworkIsActive)} called on client");
                    return;
                }

                if (isActive != value)
                {
                    new SyncOverrideEverythingSlippery(value).Send(NetworkDestination.Clients | NetworkDestination.Server);
                }
            }
        }

        // Not using cctor because this needs to be initialized on both server and all clients
        [SystemInitializer]
        static void Init()
        {
            SyncOverrideEverythingSlippery.OnReceive += SyncOverrideEverythingSlippery_OnReceive;
        }

        static void SyncOverrideEverythingSlippery_OnReceive(bool overrideIsSlippery)
        {
            isActive = overrideIsSlippery;
        }

        static void Run_onRunDestroyGlobal(Run _)
        {
            isActive = false;
        }

        static void CharacterMotor_OnGroundHit(On.RoR2.CharacterMotor.orig_OnGroundHit orig, CharacterMotor self, Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport)
        {
            orig(self, hitCollider, hitNormal, hitPoint, ref hitStabilityReport);

            if (isActive)
            {
#pragma warning disable Publicizer001 // Accessing a member that was not originally public
                // For some reason this is how ice gets slippery???
                self.isAirControlForced = true;
#pragma warning restore Publicizer001 // Accessing a member that was not originally public
            }
        }
    }
}
