using RoR2;

namespace RiskOfChaos.Utilities.BodySnapshots
{
    public readonly record struct HealthSnapshot(float HealthFraction, float ShieldFraction, bool OutOfDanger) : IBodySnapshot
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
            HealthSnapshot snapshot = this;
            void apply()
            {
                HealthComponent healthComponent = body.healthComponent;
                if (healthComponent)
                {
                    healthComponent.Networkhealth = snapshot.HealthFraction * healthComponent.fullHealth;
                    healthComponent.Networkshield = snapshot.HealthFraction * healthComponent.fullShield;
                }

                if (!snapshot.OutOfDanger)
                {
#pragma warning disable Publicizer001 // Accessing a member that was not originally public
                    body.outOfDangerStopwatch = 0f;
#pragma warning restore Publicizer001 // Accessing a member that was not originally public
                }
            }

            if (body.maxHealth == 0f)
            {
                void onBodyStart(CharacterBody b)
                {
                    if (b != body)
                        return;

                    apply();
                    CharacterBody.onBodyStartGlobal -= onBodyStart;
                }

                CharacterBody.onBodyStartGlobal += onBodyStart;
            }
            else
            {
                apply();
            }
        }
    }
}
