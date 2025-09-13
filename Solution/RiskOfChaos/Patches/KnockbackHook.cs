using RiskOfChaos.ModificationController.Knockback;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.Patches
{
    static class KnockbackHook
    {
        [SystemInitializer]
        static void Init()
        {
            On.RoR2.CharacterMotor.ApplyForceImpulse += CharacterMotor_ApplyForceImpulse;
            On.RoR2.CharacterMotor.AddDisplacement += CharacterMotor_AddDisplacement;

            On.RoR2.RigidbodyMotor.ApplyForceImpulse += RigidbodyMotor_ApplyForceImpulse;
            On.RoR2.RigidbodyMotor.AddDisplacement += RigidbodyMotor_AddDisplacement;
        }

        static void CharacterMotor_ApplyForceImpulse(On.RoR2.CharacterMotor.orig_ApplyForceImpulse orig, CharacterMotor self, ref PhysForceInfo forceInfo)
        {
            tryMultiplyForce(self.hasEffectiveAuthority, ref forceInfo);
            orig(self, ref forceInfo);
        }

        static void CharacterMotor_AddDisplacement(On.RoR2.CharacterMotor.orig_AddDisplacement orig, CharacterMotor self, Vector3 displacement)
        {
            tryMultiplyForce(self.hasEffectiveAuthority, ref displacement);
            orig(self, displacement);
        }

        static void RigidbodyMotor_ApplyForceImpulse(On.RoR2.RigidbodyMotor.orig_ApplyForceImpulse orig, RigidbodyMotor self, ref PhysForceInfo forceInfo)
        {
            tryMultiplyForce(self.hasEffectiveAuthority, ref forceInfo);
            orig(self, ref forceInfo);
        }

        static void RigidbodyMotor_AddDisplacement(On.RoR2.RigidbodyMotor.orig_AddDisplacement orig, RigidbodyMotor self, Vector3 displacement)
        {
            tryMultiplyForce(self.hasEffectiveAuthority, ref displacement);
            orig(self, displacement);
        }

        static void tryMultiplyForce(bool hasAuthority, ref PhysForceInfo forceInfo)
        {
            tryMultiplyForce(hasAuthority, ref forceInfo.force);
        }

        static void tryMultiplyForce(bool hasAuthority, ref Vector3 force)
        {
            if (!hasAuthority)
            {
                Log.Debug($"Not multiplying force, NetworkServer.active={NetworkServer.active}, {nameof(hasAuthority)}={hasAuthority}");
                return;
            }

            if (!KnockbackModificationManager.Instance || !KnockbackModificationManager.Instance.AnyModificationActive)
                return;

            force *= KnockbackModificationManager.Instance.TotalKnockbackMultiplier;
        }
    }
}
