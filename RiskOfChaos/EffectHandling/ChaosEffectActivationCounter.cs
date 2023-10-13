using System;

namespace RiskOfChaos.EffectHandling
{
    public struct ChaosEffectActivationCounter
    {
        public static readonly ChaosEffectActivationCounter EmptyCounter = new ChaosEffectActivationCounter(ChaosEffectIndex.Invalid);

        public readonly ChaosEffectIndex EffectIndex;

        public int RunActivations;

        public int StageActivations;

        public ChaosEffectActivationCounter(ChaosEffectIndex effectIndex)
        {
            EffectIndex = effectIndex;

            RunActivations = 0;
            StageActivations = 0;
        }

        public override readonly string ToString()
        {
            return $"{ChaosEffectCatalog.GetEffectInfo(EffectIndex).Identifier} (Index {EffectIndex}): {nameof(StageActivations)}={StageActivations}, {nameof(RunActivations)}={RunActivations}";
        }
    }
}
