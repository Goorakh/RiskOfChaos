using RiskOfChaos.ModifierController.Knockback;
using RoR2;
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
        }

        static void tryMultiplyForce(bool hasAuthority, ref PhysForceInfo forceInfo)
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

            forceInfo.force *= KnockbackModificationManager.Instance.NetworkedTotalKnockbackMultiplier;
        }
    }
}
