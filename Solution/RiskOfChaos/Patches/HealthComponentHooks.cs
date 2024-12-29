using RoR2;

namespace RiskOfChaos.Patches
{
    static class HealthComponentHooks
    {
        public delegate void TakeDamageDelegate(HealthComponent healthComponent, DamageInfo damageInfo);

        public static event TakeDamageDelegate PreTakeDamage;
        public static event TakeDamageDelegate PostTakeDamage;

        [SystemInitializer]
        static void Init()
        {
            On.RoR2.HealthComponent.TakeDamageProcess += HealthComponent_TakeDamageProcess;
        }

        static void HealthComponent_TakeDamageProcess(On.RoR2.HealthComponent.orig_TakeDamageProcess orig, HealthComponent self, DamageInfo damageInfo)
        {
            PreTakeDamage?.Invoke(self, damageInfo);
            orig(self, damageInfo);
            PostTakeDamage?.Invoke(self, damageInfo);
        }
    }
}
