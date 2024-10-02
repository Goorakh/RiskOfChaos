using RoR2;

namespace RiskOfChaos.Utilities.BodySnapshots
{
    public readonly record struct BuffSnapshot(BuffIndex BuffIndex, int StackCount) : IBodySnapshot
    {
        public static BuffSnapshot FromBody(CharacterBody body, BuffIndex buffIndex)
        {
            return new BuffSnapshot(buffIndex, body.GetBuffCount(buffIndex));
        }

        public readonly void ApplyTo(CharacterBody body)
        {
            body.SetBuffCount(BuffIndex, StackCount);
        }
    }
}
