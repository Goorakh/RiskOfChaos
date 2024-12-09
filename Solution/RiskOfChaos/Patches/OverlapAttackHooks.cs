using RoR2;

namespace RiskOfChaos.Patches
{
    static class OverlapAttackHooks
    {
        public delegate void OnOverlapAttackResetIgnoredHealthComponentsDelegate(OverlapAttack overlapAttack);
        public static event OnOverlapAttackResetIgnoredHealthComponentsDelegate OnOverlapAttackResetIgnoredHealthComponents;

        [SystemInitializer]
        static void Init()
        {
            On.RoR2.OverlapAttack.ResetIgnoredHealthComponents += OverlapAttack_ResetIgnoredHealthComponents;
        }

        static void OverlapAttack_ResetIgnoredHealthComponents(On.RoR2.OverlapAttack.orig_ResetIgnoredHealthComponents orig, OverlapAttack self)
        {
            orig(self);
            OnOverlapAttackResetIgnoredHealthComponents?.Invoke(self);
        }
    }
}
