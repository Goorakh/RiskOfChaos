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
#pragma warning disable Publicizer001 // Accessing a member that was not originally public
            body.SetBuffCount(BuffIndex, StackCount);
#pragma warning restore Publicizer001 // Accessing a member that was not originally public
        }
    }
}
