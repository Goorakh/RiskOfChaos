using RoR2;

namespace RiskOfChaos.EffectHandling
{
    public struct ChaosEffectActivationCounter
    {
        public static readonly ChaosEffectActivationCounter EmptyCounter = new ChaosEffectActivationCounter(-1);

        public readonly int EffectIndex;

        public int RunActivations;

        public int StageActivations;

        public ChaosEffectActivationCounter(int effectIndex)
        {
            EffectIndex = effectIndex;

            RunActivations = 0;
            StageActivations = 0;
        }

        public readonly override string ToString()
        {
            return $"{EffectIndex} ({ChaosEffectCatalog.GetEffectInfo((uint)EffectIndex).Identifier}): {nameof(StageActivations)}={StageActivations}, {nameof(RunActivations)}={RunActivations}";
        }
    }
}
