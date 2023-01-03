using KinematicCharacterController;
using R2API.Networking;
using R2API.Networking.Interfaces;
using RiskOfChaos.Networking;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.Patches
{
    static class OverrideAllSurfacesSlippery
    {
        static bool _isActive;
        public static bool NetworkIsActive
        {
            get
            {
                return _isActive;
            }
            set
            {
                if (!NetworkServer.active)
                {
                    Log.Warning($"set_{nameof(NetworkIsActive)} called on client");
                    return;
                }

                if (_isActive != value)
                {
                    _isActive = value;
                    new SyncOverrideEverythingSlippery(_isActive).Send(NetworkDestination.Clients);
                }
            }
        }

        // Not using cctor because this needs to be initialized on both server and all clients
        [SystemInitializer]
        static void Init()
        {
            SyncOverrideEverythingSlippery.OnReceive += SyncOverrideEverythingSlippery_OnReceive;
            On.RoR2.CharacterMotor.OnGroundHit += CharacterMotor_OnGroundHit;
        }

        static void SyncOverrideEverythingSlippery_OnReceive(bool overrideIsSlippery)
        {
            if (NetworkServer.active)
                return;

            _isActive = overrideIsSlippery;
        }

        static void CharacterMotor_OnGroundHit(On.RoR2.CharacterMotor.orig_OnGroundHit orig, CharacterMotor self, Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport)
        {
            orig(self, hitCollider, hitNormal, hitPoint, ref hitStabilityReport);

            if (_isActive)
            {
#pragma warning disable Publicizer001 // Accessing a member that was not originally public
                // For some reason this is how ice gets slippery???
                self.isAirControlForced = true;
#pragma warning restore Publicizer001 // Accessing a member that was not originally public
            }
        }
    }
}
