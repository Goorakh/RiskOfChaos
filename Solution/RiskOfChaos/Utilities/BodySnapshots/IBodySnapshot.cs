using RoR2;

namespace RiskOfChaos.Utilities.BodySnapshots
{
    public interface IBodySnapshot
    {
        void ApplyTo(CharacterBody body);
    }
}
