using RoR2;

namespace RiskOfChaos.Utilities
{
    public readonly record struct HealthSnapshot(float HealthFraction, float ShieldFraction, bool OutOfDanger)
    {
        public static readonly HealthSnapshot Full = new HealthSnapshot(1f, 1f, true);

        public static HealthSnapshot FromBody(CharacterBody body)
        {
            if (!body)
                return Full;

            float healthFraction;
            float shieldFraction;
            if (body.healthComponent)
            {
                healthFraction = body.healthComponent.health / body.healthComponent.fullHealth;
                shieldFraction = body.healthComponent.shield / body.healthComponent.fullShield;
            }
            else
            {
                healthFraction = Full.HealthFraction;
                shieldFraction = Full.ShieldFraction;
            }

            return new HealthSnapshot(healthFraction, shieldFraction, body.outOfDanger);
        }

        public readonly void ApplyTo(CharacterBody body)
        {
            HealthComponent healthComponent = body.healthComponent;
            if (healthComponent)
            {
                healthComponent.Networkhealth = HealthFraction * healthComponent.fullHealth;
                healthComponent.Networkshield = HealthFraction * healthComponent.fullShield;
            }

            if (!OutOfDanger)
            {
#pragma warning disable Publicizer001 // Accessing a member that was not originally public
                body.outOfDangerStopwatch = 0f;
#pragma warning restore Publicizer001 // Accessing a member that was not originally public
            }
        }
    }
}
