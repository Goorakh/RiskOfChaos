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
            On.RoR2.CharacterMotor.ApplyForceImpulse += (On.RoR2.CharacterMotor.orig_ApplyForceImpulse orig, CharacterMotor self, ref PhysForceInfo forceInfo) =>
            {
                tryMultiplyForce(self.hasEffectiveAuthority, ref forceInfo);

                orig(self, ref forceInfo);
            };

            On.RoR2.RigidbodyMotor.ApplyForceImpulse += (On.RoR2.RigidbodyMotor.orig_ApplyForceImpulse orig, RigidbodyMotor self, ref PhysForceInfo forceInfo) =>
            {
                tryMultiplyForce(self.hasEffectiveAuthority, ref forceInfo);

                orig(self, ref forceInfo);
            };

            On.RoR2.CharacterMotor.AddDisplacement += (orig, self, displacement) =>
            {
                tryMultiplyForce(self.hasEffectiveAuthority, ref displacement);
                orig(self, displacement);
            };

            On.RoR2.RigidbodyMotor.AddDisplacement += (orig, self, displacement) =>
            {
                tryMultiplyForce(self.hasEffectiveAuthority, ref displacement);
                orig(self, displacement);
            };
        }

        static void tryMultiplyForce(bool hasAuthority, ref PhysForceInfo forceInfo)
        {
            tryMultiplyForce(hasAuthority, ref forceInfo.force);
        }

        static void tryMultiplyForce(bool hasAuthority, ref Vector3 force)
        {
            if (!hasAuthority)
            {
#if DEBUG
                Log.Debug($"Not multiplying force, NetworkServer.active={NetworkServer.active}, {nameof(hasAuthority)}={hasAuthority}");
#endif
                return;
            }

            if (!KnockbackModificationManager.Instance || !KnockbackModificationManager.Instance.AnyModificationActive)
                return;

            force *= KnockbackModificationManager.Instance.TotalKnockbackMultiplier;
        }
    }
}
