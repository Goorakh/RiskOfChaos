using System;

namespace RiskOfChaos.EffectHandling.EffectClassAttributes
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class ChaosEffectAttribute : HG.Reflection.SearchableAttribute
    {
        public readonly string Identifier;

        public string ConfigName { get; set; } = null;

        public float DefaultSelectionWeight { get; set; } = 1f;

        public float EffectWeightReductionPercentagePerActivation { get; set; } = 5f;

        public EffectActivationCountMode EffectRepetitionWeightCalculationMode { get; set; } = EffectActivationCountMode.PerStage;

        public int EffectStageActivationCountHardCap { get; set; } = -1;

        public bool IsNetworked { get; set; } = false;

        public ChaosEffectAttribute(string identifier)
        {
            Identifier = identifier;
        }

        internal ChaosEffectInfo BuildEffectInfo(ChaosEffectIndex index)
        {
            return new ChaosEffectInfo(index, this);
        }
    }
}
